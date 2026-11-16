# Lite.StateMachine.DotGraph

Optional DOT-to-SVG/PNG rendering for .NET. This package has no dependency on
Lite.StateMachine and accepts DOT from any source. The core state-machine package
is unchanged and does not depend on this renderer.

## Requirements

.NET 10 or 11. Restore the NuGet package and run: **no Graphviz installation,
PATH setup, executable path, or first-run download is required.**

The assembly embeds Graphviz 16.1.1 compiled to WebAssembly/WASI. Wasmtime executes
it in-process; Svg.Skia/SkiaSharp encode its SVG output as PNG, using an embedded
Tinos font. Runtime libraries are delivered as ordinary transitive NuGet assets.
Windows, macOS, and glibc-based Linux have x64 and ARM64 assets and CI coverage.
Linux musl/Alpine, browser/mobile hosts, and NativeAOT are not supported by this backend.
The core state-machine package retains its existing dependencies and AOT behavior.

## Usage

```csharp
using Lite.StateMachine.DotGraph;

IDotGraphRenderer renderer = new GraphvizRenderer(); // bundled, offline renderer

// With Lite.StateMachine, pass machine.ExportUml() as the DOT text.
await renderer.RenderAsync(machine.ExportUml(), "images/machine.svg");
await renderer.RenderAsync(machine.ExportUml(), "images/machine.png", DotGraphFormat.Png);

// Or render an existing DOT file from any source.
await renderer.RenderFileAsync("machine.dot", "images/machine.svg");

// Optional timeout, still using the bundled renderer.
renderer = new GraphvizRenderer(timeout: TimeSpan.FromMinutes(1));

// Explicit opt-in to an externally installed/application-supplied Graphviz instead.
renderer = new GraphvizRenderer("/opt/graphviz/bin/dot", TimeSpan.FromMinutes(1));
await renderer.RenderFileAsync("machine.dot", "machine.png", DotGraphFormat.Png, cancellationToken);
```

Output defaults to SVG; the format argument controls encoding rather than the file
extension. Parent directories are created automatically. Existing destinations are
replaced only after a successful render. Errors include Graphviz diagnostics.
Cancellation or the default 30-second timeout interrupts WASM layout and removes
temporary output. Initial module compilation and native PNG encoding finish before
cancellation is observed. Input DOT files are never modified. Concurrent renders
use isolated engines so cancellation cannot interrupt another request.

The bundled build includes core rendering and layout plugins. It does not load
host files, external images, or additional Graphviz plugins, and HTML-like labels
requiring Expat are unsupported. PNG output uses the bundled regular font as a
fallback for requested families/styles and is limited to 32 million pixels; WASM
memory is limited to 256 MiB per render. For additional Graphviz plugins, image
assets, or system-font fidelity, explicitly supply your own Graphviz executable.
That override uses the original process-based renderer, including its exit-code
diagnostics and relative-image resolution from the application's working directory.

The bundled binaries, source/build URLs, SHA-256 hashes, and licenses are recorded
in [Assets/SOURCES.txt](Assets/SOURCES.txt). License notices are also packed under
`licenses/` in the NuGet package. Graphviz and the font are checked in and embedded
at build time; the renderer contains no downloader or installer.

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
* Publish a separate tooling project referencing `Lite.StateMachine.DotGraph` into
  a `tooling` folder, keeping its `.deps.json`, managed dependencies, and `runtimes`
  folders together. Copying just the renderer DLL is insufficient for the embedded
  backend. No package reference is needed in the application project. For example,
  from a directory beside your script:

  ```powershell
  dotnet new console -f net10.0 -o diagram-tooling
  dotnet add diagram-tooling package Lite.StateMachine.DotGraph
  dotnet publish diagram-tooling -c Release --self-contained false -o tooling
  ```
* Use the application's build/publish output, including its matching
  `Lite.StateMachine.dll` and other dependencies. Load additional factory dependencies
  explicitly if needed; applications requiring a custom host or DI container may need
  that setup in the script too.

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

# PowerShell does not automatically use another application's .deps.json.
# C# callbacks also work when native libraries load on background render threads.
Add-Type -TypeDefinition @'
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
public static class DiagramToolDependencies
{
  public static void Load(string toolingApplication)
  {
    var resolver = new AssemblyDependencyResolver(toolingApplication);
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
[DiagramToolDependencies]::Load(
  (Resolve-Path "$PSScriptRoot/tooling/diagram-tooling.dll").Path)

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
$renderer = [Lite.StateMachine.DotGraph.GraphvizRenderer]::new()
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

The default suite tests the bundled renderer against both target frameworks:

```powershell
dotnet test source/Lite.StateMachine.DotGraph.Tests -c Release --filter 'TestCategory!=ExternalGraphviz'
pwsh -NoProfile -File scripts/Test-DotGraphPackage.ps1
```

The package smoke test restores a fresh NuGet consumer, publishes its runtime
dependencies, and generates both images with an empty PATH. CI runs the bundled
tests, package smoke test, and console/PowerShell samples on all six OS/CPU pairs
without installing Graphviz. These tests do not publish packages.

The optional `ExternalGraphviz` test retains coverage for the executable override.
Set `GRAPHVIZ_DOT` to a real executable and omit the filter to include that test.
