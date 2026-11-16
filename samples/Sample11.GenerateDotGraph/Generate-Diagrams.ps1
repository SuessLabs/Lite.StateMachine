<#
.SYNOPSIS
Builds the sample, loads its DLL, and renders the same diagrams as the console app.
.EXAMPLE
./Generate-Diagrams.ps1
#>
#Requires -Version 7.0

[CmdletBinding()]
param(
  [string] $OutputDirectory = (Join-Path $PSScriptRoot 'diagrams'),
  [string] $DotPath,
  [ValidateSet('Debug', 'Release')]
  [string] $Configuration = 'Release',
  [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([Environment]::Version.Major -lt 10) {
  throw 'Use PowerShell hosted on .NET 10 or later (for example PowerShell 7.6). Windows PowerShell 5.1 is not supported.'
}

$project = Join-Path $PSScriptRoot 'Sample11.GenerateDotGraph.csproj'
if (-not $SkipBuild) {
  dotnet build $project -c $Configuration -f net10.0 -p:TargetFrameworks=net10.0
  if ($LASTEXITCODE -ne 0) {
    throw 'Sample build failed.'
  }
}

# Matches this repository's Directory.Build.props output layout.
$assemblyDirectory = [IO.Path]::GetFullPath(
  (Join-Path $PSScriptRoot "../../output/Sample11.GenerateDotGraph/$Configuration/net10.0"))

# PowerShell does not use the sample's .deps.json automatically. These C# callbacks
# resolve its NuGet assemblies/native libraries, including on background render threads.
Add-Type -TypeDefinition @'
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
public static class Sample11DependencyLoader
{
  public static void Initialize(string applicationPath)
  {
    var resolver = new AssemblyDependencyResolver(applicationPath);
    AssemblyLoadContext.Default.Resolving += (context, name) =>
    {
      var path = resolver.ResolveAssemblyToPath(name);
      return path == null ? null : context.LoadFromAssemblyPath(path);
    };
    AssemblyLoadContext.Default.ResolvingUnmanagedDll += (assembly, name) =>
    {
      var path = resolver.ResolveUnmanagedDllToPath(name);
      return path == null ? IntPtr.Zero : NativeLibrary.Load(path);
    };
  }
}
'@
[Sample11DependencyLoader]::Initialize((Join-Path $assemblyDirectory 'Sample11.GenerateDotGraph.dll'))

foreach ($assemblyName in @('Lite.StateMachine.dll', 'Lite.StateMachine.DotGraph.dll', 'Sample11.GenerateDotGraph.dll'))
{
  $assemblyPath = Join-Path $assemblyDirectory $assemblyName
  if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "Assembly not found: $assemblyPath. Build the sample first or omit -SkipBuild."
  }
  [void][Reflection.Assembly]::LoadFrom($assemblyPath)
}

$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$renderer = if ($DotPath) {
  [Lite.StateMachine.DotGraph.GraphvizRenderer]::new($DotPath, [TimeSpan]::FromSeconds(30))
} else {
  [Lite.StateMachine.DotGraph.GraphvizRenderer]::new()
}
$token = [Threading.CancellationToken]::None

foreach ($includeLegend in @($false, $true)) {
  $name = if ($includeLegend) { 'machine-legend' } else { 'machine' }

  # This calls a public DLL method, not the console application's entry point.
  $dot = [Sample11.GenerateDotGraph.SampleStateMachine]::ExportDot($includeLegend)

  [IO.File]::WriteAllText((Join-Path $OutputDirectory "$name.dot"), $dot)

  [void]$renderer.RenderAsync($dot, (Join-Path $OutputDirectory "$name.svg"),
    [Lite.StateMachine.DotGraph.DotGraphFormat]::Svg, $token).GetAwaiter().GetResult()

  [void]$renderer.RenderAsync($dot, (Join-Path $OutputDirectory "$name.png"),
    [Lite.StateMachine.DotGraph.DotGraphFormat]::Png, $token).GetAwaiter().GetResult()

  Write-Output "Generated $name.dot, $name.svg, and $name.png in $OutputDirectory"
}
