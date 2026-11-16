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
