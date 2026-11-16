// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Svg.Skia;
using Svg.Skia.TypefaceProviders;
using Wasmtime;

namespace Lite.StateMachine.DotGraph;

/// <summary>Offline Graphviz WASI execution and PNG encoding. No subprocesses or host filesystem imports.</summary>
internal static class BundledGraphviz
{
  internal static async Task RenderAsync(
    string? dot,
    string? inputPath,
    string outputPath,
    DotGraphFormat format,
    TimeSpan timeout,
    CancellationToken cancellationToken)
  {
    using var deadline = new CancellationTokenSource(timeout);
    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
    var token = cancellation.Token;
    var destination = Path.GetFullPath(outputPath);
    var directory = Path.GetDirectoryName(destination)!;

    Directory.CreateDirectory(directory);
    var temporary = Path.Combine(directory, Path.GetRandomFileName());

    try
    {
      dot ??= await File.ReadAllTextAsync(inputPath!, token).ConfigureAwait(false);
      var source = dot;

      await Task.Run(
        () =>
        {
          var svg = RenderSvg(source, token);
          token.ThrowIfCancellationRequested();

          if (format == DotGraphFormat.Svg)
            File.WriteAllBytes(temporary, svg);
          else
            WritePng(svg, temporary, token);
        },
        token).ConfigureAwait(false);

      token.ThrowIfCancellationRequested();
      File.Move(temporary, destination, overwrite: true);
    }
    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
    {
      throw new TimeoutException($"Graphviz rendering exceeded {timeout}.", ex);
    }
    finally
    {
      File.Delete(temporary);
    }
  }

  private static Stream OpenAsset(string name) =>
    typeof(BundledGraphviz).Assembly.GetManifestResourceStream("Lite.StateMachine.DotGraph." + name)
      ?? throw new InvalidOperationException("Bundled rendering asset is missing: " + name);

  private static byte[] RenderSvg(string dot, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    // Each invocation owns its engine/store. Cancellation cannot interrupt another render.
    using var configuration = new Config().WithEpochInterruption(true);
    using var engine = new Engine(configuration);
    using var binary = OpenAsset("graphviz.wasm");
    using var module = Module.FromStream(engine, "bundled-graphviz-16.1.1", binary);

    cancellationToken.ThrowIfCancellationRequested();

    using var store = new Store(engine);

    store.SetLimits(memorySize: 256 * 1024 * 1024);
    store.SetEpochDeadline(1);
    store.SetWasiConfiguration(new WasiConfiguration());

    using var linker = new Linker(engine);
    linker.DefineWasi();

    var instance = linker.Instantiate(store, module);
    var memory = instance.GetMemory("memory")!;
    var allocate = instance.GetFunction<int, int>("malloc")!;
    var render = instance.GetFunction<int, int, int, int, int>("graphviz_render")!;
    var lastError = instance.GetFunction<int>("graphviz_last_error")!;

    using var registration = cancellationToken.Register(engine.IncrementEpoch);

    try
    {
      var dotAddress = WriteString(memory, allocate, dot);
      var formatAddress = WriteString(memory, allocate, "svg");
      var engineAddress = WriteString(memory, allocate, "dot");
      var lengthAddress = allocate(sizeof(int));

      if (lengthAddress == 0)
        throw new InvalidOperationException("Bundled Graphviz could not allocate memory.");

      var result = render(dotAddress, formatAddress, engineAddress, lengthAddress);
      if (result == 0)
      {
        var errorAddress = lastError();
        var message = memory.GetSpan(errorAddress, 4096);
        var end = message.IndexOf((byte)0);

        throw new InvalidOperationException("Graphviz rendering failed: " + Encoding.UTF8.GetString(end < 0 ? message : message[..end]));
      }

      cancellationToken.ThrowIfCancellationRequested();
      var length = memory.ReadInt32(lengthAddress);

      if (length <= 0)
        throw new InvalidOperationException("Bundled Graphviz produced no image.");

      return memory.GetSpan(result, length).ToArray();
    }
    catch (WasmtimeException) when (cancellationToken.IsCancellationRequested)
    {
      cancellationToken.ThrowIfCancellationRequested();
      throw;
    }
  }

  private static void WritePng(byte[] data, string path, CancellationToken cancellationToken)
  {
    using var fontStream = OpenAsset("Tinos-Regular.ttf");
    using var typeface = SKTypeface.FromStream(fontStream);
    using var svg = new SKSvg();

    // A packaged font keeps PNG labels available even in fontless Linux containers.
    svg.Settings.TypefaceProviders = [new BundledTypefaceProvider(typeface)];

    using var stream = new MemoryStream(data, writable: false);

    var picture = svg.Load(stream) ?? throw new InvalidOperationException("Could not rasterize Graphviz SVG.");
    var bounds = picture.CullRect;
    var width = checked((int)Math.Ceiling(bounds.Width));
    var height = checked((int)Math.Ceiling(bounds.Height));

    if (width <= 0 || height <= 0 || (long)width * height > 32_000_000)
      throw new InvalidOperationException("PNG output must be between 1 and 32 million pixels. Use SVG for larger diagrams.");

    cancellationToken.ThrowIfCancellationRequested();

    using var surface = SKSurface.Create(new SKImageInfo(width, height))
      ?? throw new InvalidOperationException("Could not allocate the PNG canvas.");

    surface.Canvas.Clear(SKColors.Transparent);
    surface.Canvas.Translate(-bounds.Left, -bounds.Top);
    surface.Canvas.DrawPicture(picture);
    cancellationToken.ThrowIfCancellationRequested();

    using var image = surface.Snapshot();
    using var png = image.Encode(SKEncodedImageFormat.Png, 100)
      ?? throw new InvalidOperationException("Could not encode the PNG image.");

    using var output = File.Create(path);
    png.SaveTo(output);
  }

  private static int WriteString(Memory memory, Func<int, int> allocate, string text)
  {
    var data = Encoding.UTF8.GetBytes(text + '\0');
    var address = allocate(data.Length);

    if (address == 0)
      throw new InvalidOperationException("Bundled Graphviz could not allocate memory.");

    data.AsSpan().CopyTo(memory.GetSpan(address, data.Length));
    return address;
  }

  private sealed class BundledTypefaceProvider(SKTypeface typeface) : ITypefaceProvider
  {
    public SKTypeface FromFamilyName(string fontFamily, SKFontStyleWeight fontWeight, SKFontStyleWidth fontWidth, SKFontStyleSlant fontStyle) =>
      typeface;
  }
}
