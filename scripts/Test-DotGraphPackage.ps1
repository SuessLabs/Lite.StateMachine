#Requires -Version 7.0
<#
.SYNOPSIS
Packs the renderer, restores a fresh NuGet consumer, and renders with an empty PATH.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dotnet = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
$version = '0.0.0-bundled.' + [DateTime]::UtcNow.Ticks
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('dotgraph-package-' + [Guid]::NewGuid().ToString('N'))
$feed = Join-Path $scratch 'feed'
$consumer = Join-Path $scratch 'consumer'
$publish = Join-Path $scratch 'publish'
[void][IO.Directory]::CreateDirectory($consumer)

& $dotnet pack (Join-Path $repository 'source/Lite.StateMachine.DotGraph') -c Release "-p:PackageVersion=$version" --output $feed -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Renderer packing failed.' }

$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Lite.StateMachine.DotGraph" Version="$version" />
  </ItemGroup>
</Project>
"@
[IO.File]::WriteAllText((Join-Path $consumer 'Consumer.csproj'), $project)
[IO.File]::WriteAllText((Join-Path $consumer 'Program.cs'), @'
using Lite.StateMachine.DotGraph;
var renderer = new GraphvizRenderer();
var directory = Path.Combine(AppContext.BaseDirectory, "images");
Directory.CreateDirectory(directory);
var dot = "digraph { subgraph cluster_child { label=\"Child\"; a [label=\"Prépare\"]; a -> b [label=\"Success\"]; } b -> c; }";
await renderer.RenderAsync(dot, Path.Combine(directory, "machine.svg"));
await renderer.RenderAsync(dot, Path.Combine(directory, "machine.png"), DotGraphFormat.Png);
var svg = File.ReadAllText(Path.Combine(directory, "machine.svg"));
var png = File.ReadAllBytes(Path.Combine(directory, "machine.png"));
if (!svg.Contains("<svg") || !svg.Contains("Prépare") || !svg.Contains("Child"))
  throw new Exception("SVG is missing graph content.");
if (Convert.ToHexString(png.AsSpan(0, 8)) != "89504E470D0A1A0A")
  throw new Exception("Invalid PNG signature.");
Console.WriteLine("PASS: Published NuGet consumer rendered SVG and PNG with an empty PATH.");
'@)
$escapedFeed = [Security.SecurityElement]::Escape($feed)
$configPath = Join-Path $scratch 'NuGet.Config'
[IO.File]::WriteAllText($configPath, @"
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$escapedFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@)
& $dotnet restore $consumer --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw 'NuGet consumer restore failed.' }
& $dotnet publish $consumer -c Release --no-restore --self-contained false --output $publish -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'NuGet consumer publish failed.' }

$savedPath = $env:PATH
try {
  # Nothing, including an existing Graphviz installation, can be resolved on PATH.
  $env:PATH = ''
  & $dotnet (Join-Path $publish 'Consumer.dll')
  if ($LASTEXITCODE -ne 0) { throw 'Published bundled rendering failed.' }
} finally {
  $env:PATH = $savedPath
}
Write-Output "Smoke-test files: $scratch"
