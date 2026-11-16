// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Lite.StateMachine.DotGraph;

namespace Sample11.GenerateDotGraph;

internal static class Program
{
  private static async Task<int> Main(string[] args)
  {
    if (args.Length > 2)
    {
      Console.Error.WriteLine("Usage: Sample11.GenerateDotGraph [output-directory] [dot-executable]");
      return 1;
    }

    var outputDirectory = Path.GetFullPath(args.Length > 0
      ? args[0]
      : Path.Combine(AppContext.BaseDirectory, "diagrams"));

    var renderer = new GraphvizRenderer(args.Length > 1
      ? args[1]
      : null);

    Directory.CreateDirectory(outputDirectory);

    foreach (var includeLegend in new[] { false, true })
    {
      var name = includeLegend ? "machine-legend" : "machine";

      // Create state machine and export DOT graph without running any states.
      var dot = SampleStateMachine.ExportDot(includeLegend);
      var dotPath = Path.Combine(outputDirectory, name + ".dot");

      await File.WriteAllTextAsync(dotPath, dot);
      await renderer.RenderAsync(dot, Path.Combine(outputDirectory, name + ".svg"));
      await renderer.RenderAsync(dot, Path.Combine(outputDirectory, name + ".png"), DotGraphFormat.Png);

      Console.WriteLine($"Generated {name}.dot, {name}.svg, and {name}.png in {outputDirectory}");
    }

    return 0;
  }
}
