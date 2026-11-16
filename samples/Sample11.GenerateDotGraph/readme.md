# Sample 11: Generate DOT, SVG, and PNG diagrams

This sample uses `Lite.StateMachine.DotGraph` to render the same topology as
`Basic_GeneratesExportUml_SuccessTestAsync` in `DiMsTests.cs`:

* `Entry` transitions to the composite `Parent` state.
* `Parent` starts at `Parent_Fetch`, which transitions to `Parent_WaitMessage`.
* `Parent` has `Success`, `Error`, and `Failure` transitions to `Done`, `Error`, and `Failure`.
* `Error` and `Failure` transition back to `Parent`;
* `Done` is the terminal final state.

The DI factory, state identifiers, transition registrations, initial Entry marker,
and legend options match the test. State handlers are minimal diagram-only examples;
the test's message service, counters, and workflow execution are not needed to export
the topology. Neither generation path runs the workflow.

## Requirements

* Install .NET 10 or 11
* Install [Graphviz](https://graphviz.org/download/).
* Add Graphviz's `bin` directory to PATH, or pass its `dot`/`dot.exe` path explicitly.

The project references the local core and renderer projects; it does not reference
or reuse the unit-test assembly.

## Console application

From the repository root:

```powershell
dotnet run --project samples/Sample11.GenerateDotGraph -c Release -f net10.0 -- samples/Sample11.GenerateDotGraph/diagrams

# Optional second argument: the Graphviz executable (quote paths with spaces).
dotnet run --project samples/Sample11.GenerateDotGraph -c Release -f net10.0 -- samples/Sample11.GenerateDotGraph/diagrams 'C:/Program Files/Graphviz/bin/dot.exe'
```

Use `-f net11.0` to run on .NET 11. With no application arguments, diagrams are
written to a `diagrams` directory beside the built executable.

## External PowerShell generation

Use PowerShell hosted on .NET 10 or later, such as PowerShell 7.6.
Run a fresh process to avoid conflicts with previously loaded DLL versions:

```powershell
pwsh -NoProfile -File samples/Sample11.GenerateDotGraph/Generate-Diagrams.ps1

# Optional output folder and Graphviz path:
pwsh -NoProfile -File samples/Sample11.GenerateDotGraph/Generate-Diagrams.ps1 -OutputDirectory ./output/sample11-powershell -DotPath 'C:/Program Files/Graphviz/bin/dot.exe'
```

The script builds `net10.0`, loads the sample DLL and its libraries, and calls
`WorkflowDiagram.ExportDot(bool)` directly. It then renders the returned DOT through
`GraphvizRenderer`; it does not launch the sample executable. `-SkipBuild` reuses a
previous build, and `-Configuration Debug` selects Debug output instead of Release.
By default, the script writes to this sample's `diagrams` directory.

Both generation paths produce six files, replacing existing images on success:

| Version | DOT | SVG | PNG |
| --- | --- | --- | --- |
| Without legend | `machine.dot` | `machine.svg` | `machine.png` |
| With legend | `machine-legend.dot` | `machine-legend.svg` | `machine-legend.png` |

The checked-in `diagrams` directory contains generated examples. DOT text is the
same from both paths; image layout may vary with the installed Graphviz version
and fonts.
