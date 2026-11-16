# Lite.StateMachine.DotGraph

Optional DOT-to-SVG/PNG rendering for .NET. This package has no dependency on
Lite.StateMachine and accepts DOT from any source. The core state-machine package
is unchanged and does not depend on this renderer.

## Requirements

.NET 10 or 11 and a separately installed or application-supplied
[Graphviz](https://graphviz.org/download/) `dot` executable. The wrapper uses portable
.NET process APIs and is intended for Windows, Linux, and macOS desktop/server apps.
It does not bundle native binaries, install software, or download anything at runtime.
It is not a browser or mobile rendering backend.

## Usage

```csharp
using Lite.StateMachine.DotGraph;

IDotGraphRenderer renderer = new GraphvizRenderer(); // dot on PATH

// With Lite.StateMachine, pass machine.ExportUml() as the DOT text.
await renderer.RenderAsync(machine.ExportUml(), "images/machine.svg");
await renderer.RenderAsync(machine.ExportUml(), "images/machine.png", DotGraphFormat.Png);

// Or render an existing DOT file from any source.
await renderer.RenderFileAsync("machine.dot", "images/machine.svg");

// Optional application-supplied executable and timeout.
renderer = new GraphvizRenderer("/opt/graphviz/bin/dot", TimeSpan.FromMinutes(1));
await renderer.RenderFileAsync("machine.dot", "machine.png", DotGraphFormat.Png, cancellationToken);
```

Output defaults to SVG; the format argument controls encoding rather than the file
extension. Parent directories are created automatically. Existing destinations are
replaced only after a successful render. Rendering errors include Graphviz's exit
code and diagnostics. Cancellation or the default 30-second timeout terminates the
Graphviz process and removes temporary output. Input DOT files are never modified.
Relative image references in DOT resolve from the application's working directory.

`IDotGraphRenderer` keeps consumers independent of executable deployment, leaving
room for a future backend with bundled or embedded rendering. Cross-platform
deployment today still requires compatible Graphviz binaries and their dependencies
on each target OS/architecture.

Build the NuGet package from the repository root:

```sh
dotnet pack source/Lite.StateMachine.DotGraph -c Release
```

## Generate diagrams externally with PowerShell

You can load the application's compiled DLL and the renderer DLL from an external
script. The application does **not** need a reference to the DotGraph NuGet package
or a rebuild for rendering. This works when an existing public factory or constructor
exposes the configured state machine, or an existing method returns DOT text.
The factory should register the topology without running the workflow.

Prepare the script's environment:

* Use PowerShell 7 running a compatible .NET runtime. Check
  `[System.Environment]::Version`; Windows PowerShell 5.1 cannot load these assemblies.
  For example, load the `net10.0` renderer with a .NET 10 PowerShell host. Installing
  a newer .NET SDK does not change the runtime used by an existing PowerShell host.
* Build the renderer separately with
  `dotnet build source/Lite.StateMachine.DotGraph -c Release -f net10.0`, or extract
  `lib/net10.0/Lite.StateMachine.DotGraph.dll` from its `.nupkg` into a tooling folder.
  No package reference is needed in the application project.
* Use the application's build/publish output, including its matching
  `Lite.StateMachine.dll` and other dependencies. Load additional factory dependencies
  explicitly if needed; applications requiring a custom host or DI container may need
  that setup in the script too.
* Install Graphviz, or supply the path to an application-managed `dot` executable.

Save this example as `Export-StateDiagram.ps1`. Replace the example paths, type,
and method with an **existing** API in your application. This example assumes a
public static, parameterless `MyApp.WorkflowFactory.Create()` method that returns
a configured `StateMachine<TStateId>`:

```powershell
$ErrorActionPreference = 'Stop'

# Paths are relative to this script; adjust to your build output/tooling folder.
$applicationDll = (Resolve-Path "$PSScriptRoot/app/MyApp.dll").Path
$rendererDll = (Resolve-Path "$PSScriptRoot/tooling/Lite.StateMachine.DotGraph.dll").Path
$applicationDirectory = Split-Path $applicationDll
$outputDirectory = Join-Path $PSScriptRoot 'diagrams'

# Load the application's version of the core library, then the application.
[void][System.Reflection.Assembly]::LoadFrom(
  (Join-Path $applicationDirectory 'Lite.StateMachine.dll'))
$application = [System.Reflection.Assembly]::LoadFrom($applicationDll)
[void][System.Reflection.Assembly]::LoadFrom($rendererDll)

# Invoke an existing public static factory without starting the application.
$factoryType = $application.GetType('MyApp.WorkflowFactory', $true)
$factory = $factoryType.GetMethod('Create', [System.Type]::EmptyTypes)
if ($null -eq $factory -or -not $factory.IsStatic) {
  throw 'Expected a public static, parameterless Create() factory.'
}
$machine = $factory.Invoke($null, [object[]]@())
if ($null -eq $machine) {
  throw 'The factory did not return a configured state machine.'
}
$dot = $machine.ExportUml()

# Keep DOT as well as both image formats. No changes to the application project.
[void][System.IO.Directory]::CreateDirectory($outputDirectory)
[System.IO.File]::WriteAllText((Join-Path $outputDirectory 'machine.dot'), $dot)
$renderer = [Lite.StateMachine.DotGraph.GraphvizRenderer]::new(
  'dot', [TimeSpan]::FromSeconds(30)) # Or an absolute path to dot/dot.exe.
$token = [System.Threading.CancellationToken]::None
[void]$renderer.RenderAsync($dot, (Join-Path $outputDirectory 'machine.svg'),
  [Lite.StateMachine.DotGraph.DotGraphFormat]::Svg, $token).GetAwaiter().GetResult()
[void]$renderer.RenderAsync($dot, (Join-Path $outputDirectory 'machine.png'),
  [Lite.StateMachine.DotGraph.DotGraphFormat]::Png, $token).GetAwaiter().GetResult()
```

Run it in a fresh PowerShell process so previously loaded application assemblies
do not conflict with another build:

```powershell
pwsh -NoProfile -File ./Export-StateDiagram.ps1
```

For an existing public parameterless constructor, replace factory lookup/invocation
with `$machine = [System.Activator]::CreateInstance($application.GetType('MyApp.Workflow', $true))`.
If an existing method returns DOT directly, assign that result to `$dot` instead
of calling `ExportUml()`. If you already have a `.dot` file, skip loading the
application and core assemblies and call `RenderFileAsync()` on the renderer.

This is API invocation, not automatic extraction from arbitrary compiled code.
If the application only creates a machine inside a local variable in `Run()` or
`Main()` and never exposes it, the script cannot recover that object without an
application API change. Likewise, it cannot infer transitions that have not been
registered; topology that depends on configuration needs the corresponding setup.

## Tests

The test project runs against both target frameworks. Set `GRAPHVIZ_DOT` to `dot`
or an executable's full path to enable the real rendering tests:

```powershell
$env:GRAPHVIZ_DOT = 'dot'
dotnet test source/Lite.StateMachine.DotGraph.Tests -c Release
```

Without this variable, the integration tests are marked inconclusive; argument
validation and missing-executable tests still run. The DOT rendering CI workflow
installs Graphviz and runs the full suite on Windows, Linux, and macOS. This workflow
also builds the NuGet package without publishing it.
