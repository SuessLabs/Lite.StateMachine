// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lite.StateMachine.DotGraph;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Lite.StateMachine.DotGraph.Tests;

[TestClass]
public sealed class GraphvizRendererTests
{
  private const string Graph = "digraph { subgraph cluster_child { label=\"Child\"; a [label=\"Prépare\"]; a -> b [label=\"Success\"]; } b -> c [label=\"Failure\"]; }";
  private readonly string _directory = Path.Combine(Path.GetTempPath(), "DotGraph tests " + Guid.NewGuid());

  [TestCleanup]
  public void Cleanup()
  {
    if (Directory.Exists(_directory))
      Directory.Delete(_directory, recursive: true);
  }

  [TestMethod]
  public async Task MissingExecutableExplainsInstallationAsync()
  {
    var renderer = new GraphvizRenderer(Path.Combine(_directory, "missing-dot"));
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync(Graph, Output("machine.svg")));
    StringAssert.Contains(exception.Message, "Install Graphviz");
    Assert.IsEmpty(Directory.GetFiles(_directory));
  }

  [TestMethod]
  public async Task ValidationRejectsInvalidArgumentsBeforeStartingGraphvizAsync()
  {
    Assert.Throws<ArgumentException>(() => new GraphvizRenderer(" "));
    Assert.Throws<ArgumentOutOfRangeException>(() => new GraphvizRenderer(timeout: TimeSpan.Zero));
    var renderer = new GraphvizRenderer("missing-dot");
    Assert.Throws<ArgumentException>(() => renderer.RenderAsync(" ", Output("machine.svg")));
    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => renderer.RenderAsync(Graph, Output("machine.svg"), (DotGraphFormat)99));
    Assert.Throws<FileNotFoundException>(() => renderer.RenderFileAsync(Output("missing.dot"), Output("machine.svg")));
    Directory.CreateDirectory(_directory);
    var input = Output("machine.dot");
    await File.WriteAllTextAsync(input, Graph);
    Assert.Throws<ArgumentException>(() => renderer.RenderFileAsync(input, input));
    Assert.AreEqual(Graph, await File.ReadAllTextAsync(input));
  }

  [TestMethod]
  public async Task PreCanceledRequestDoesNotCreateOutputAsync()
  {
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await Assert.ThrowsAsync<OperationCanceledException>(() => new GraphvizRenderer().RenderAsync(Graph, Output("machine.svg"), cancellationToken: cancellation.Token));
    Assert.IsFalse(Directory.Exists(_directory));
  }

  [TestMethod]
  [TestCategory("Graphviz")]
  public async Task RendersTextAsUtf8SvgAndReplacesExistingImageAsync()
  {
    var renderer = CreateRenderer();
    var output = Output("nested [states]/machine ü.svg");
    await renderer.RenderAsync(Graph, output);
    var svg = await File.ReadAllTextAsync(output);
    StringAssert.Contains(svg, "<svg");
    StringAssert.Contains(svg, "graphviz version 16.1.1");
    StringAssert.Contains(svg, "Prépare");
    StringAssert.Contains(svg, "Child");
    await renderer.RenderAsync("digraph { replacement }", output);
    StringAssert.Contains(await File.ReadAllTextAsync(output), "replacement");
    Assert.HasCount(1, Directory.GetFiles(Path.GetDirectoryName(output)!));
  }

  [TestMethod]
  [TestCategory("Graphviz")]
  public async Task RendersDotFileAsPngWithoutChangingSourceAsync()
  {
    var renderer = CreateRenderer();
    Directory.CreateDirectory(_directory);
    var input = Output("input [states] ü.dot");
    await File.WriteAllTextAsync(input, Graph);
    var output = Output("machine.png");
    await renderer.RenderFileAsync(input, output, DotGraphFormat.Png);
    var png = await File.ReadAllBytesAsync(output);
    Assert.IsGreaterThan(8, png.Length);
    Assert.AreEqual("89504E470D0A1A0A", Convert.ToHexString(png.AsSpan(0, 8)));
    Assert.AreEqual(Graph, await File.ReadAllTextAsync(input));
  }

  [TestMethod]
  [TestCategory("Graphviz")]
  public async Task InvalidDotReportsDiagnosticsAndPreservesImageAsync()
  {
    var renderer = CreateRenderer();
    Directory.CreateDirectory(_directory);
    var output = Output("existing.svg");
    await File.WriteAllTextAsync(output, "previous image");
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync("invalid dot", output));
    StringAssert.Contains(exception.Message, "Graphviz rendering failed");
    StringAssert.Contains(exception.Message, "parse DOT");
    Assert.AreEqual("previous image", await File.ReadAllTextAsync(output));
    Assert.HasCount(1, Directory.GetFiles(_directory));
  }

  [TestMethod]
  [TestCategory("Graphviz")]
  public async Task LargeInvalidInputReportsBundledDiagnosticsAsync()
  {
    var renderer = CreateRenderer();
    var dot = "invalid dot " + new string('x', 1024 * 1024);
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync(dot, Output("machine.svg")));
    StringAssert.Contains(exception.Message, "Graphviz rendering failed");
    Assert.IsEmpty(Directory.GetFiles(_directory));
  }

  [TestMethod]
  [TestCategory("Graphviz")]
  public async Task TimeoutPreservesImageAndCleansTemporaryOutputAsync()
  {
    var renderer = CreateRenderer(TimeSpan.FromMilliseconds(1));
    Directory.CreateDirectory(_directory);
    var output = Output("existing.svg");
    await File.WriteAllTextAsync(output, "previous image");
    await Assert.ThrowsAsync<TimeoutException>(() => renderer.RenderAsync(LargeGraph(), output));
    Assert.AreEqual("previous image", await File.ReadAllTextAsync(output));
    Assert.HasCount(1, Directory.GetFiles(_directory));
  }

  [TestMethod]
  [TestCategory("Graphviz")]
  public async Task CancellationStopsRenderingAndCleansTemporaryOutputAsync()
  {
    var renderer = CreateRenderer();
    var graph = LargeGraph();
    using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
    await Assert.ThrowsAsync<OperationCanceledException>(() => renderer.RenderAsync(graph, Output("machine.svg"), cancellationToken: cancellation.Token));
    Assert.IsEmpty(Directory.GetFiles(_directory));
  }

  [TestMethod]
  public void BundledAssetsMatchPinnedHashes()
  {
    using var engine = typeof(GraphvizRenderer).Assembly.GetManifestResourceStream("Lite.StateMachine.DotGraph.graphviz.wasm");
    using var font = typeof(GraphvizRenderer).Assembly.GetManifestResourceStream("Lite.StateMachine.DotGraph.Tinos-Regular.ttf");
    Assert.IsNotNull(engine);
    Assert.IsNotNull(font);
    Assert.AreEqual("37FAFDA7F34855E7DEFFCB7317C1970AEEA7B82F7CF191B8919501FCB48E9E3A", Convert.ToHexString(SHA256.HashData(engine)));
    Assert.AreEqual("60A0E8EF0C04DD5DD69FFE91025FA2AE5836CBD35600A82BA031977557E2CB61", Convert.ToHexString(SHA256.HashData(font)));
  }

  [TestMethod]
  [TestCategory("ExternalGraphviz")]
  public async Task ExternalExecutablePreservesBrokenPipeDiagnosticsAsync()
  {
    var executable = Environment.GetEnvironmentVariable("GRAPHVIZ_DOT");
    if (string.IsNullOrWhiteSpace(executable))
      Assert.Inconclusive("Optional: set GRAPHVIZ_DOT to test the external executable override.");

    var renderer = new GraphvizRenderer(executable);
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync("invalid dot " + new string('x', 1024 * 1024), Output("machine.svg")));
    StringAssert.Contains(exception.Message, "Graphviz exited with code");
    StringAssert.Contains(exception.Message, "syntax error");
    Assert.IsEmpty(Directory.GetFiles(_directory));
  }

  private static GraphvizRenderer CreateRenderer(TimeSpan? timeout = null) => new(timeout: timeout);

  private static string LargeGraph()
  {
    var graph = new StringBuilder("digraph {");
    for (var i = 0; i < 20000; ++i)
      graph.Append("n").Append(i).Append(" -> n").Append(i + 1).Append(';');

    return graph.Append('}').ToString();
  }

  private string Output(string name) => Path.Combine(_directory, name);
}
