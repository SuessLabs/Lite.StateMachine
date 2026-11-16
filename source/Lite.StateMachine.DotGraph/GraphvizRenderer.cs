// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.StateMachine.DotGraph;

/// <summary>Renders DOT using bundled Graphviz, or an explicitly supplied executable.</summary>
public sealed class GraphvizRenderer : IDotGraphRenderer
{
  private readonly string? _executablePath;
  private readonly TimeSpan _timeout;

  /// <summary>Initializes a new instance of the <see cref="GraphvizRenderer"/> class.</summary>
  /// <param name="executablePath">Null uses bundled Graphviz. Supply a path or "dot" to opt into an external executable.</param>
  /// <param name="timeout">Maximum rendering duration. Defaults to 30 seconds.</param>
  public GraphvizRenderer(string? executablePath = null, TimeSpan? timeout = null)
  {
    if (executablePath is not null)
      ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

    _executablePath = executablePath;
    _timeout = timeout ?? TimeSpan.FromSeconds(30);

    if (_timeout <= TimeSpan.Zero || _timeout.TotalMilliseconds > uint.MaxValue - 1)
      throw new ArgumentOutOfRangeException(nameof(timeout));
  }

  /// <inheritdoc/>
  public Task RenderAsync(string dot, string outputPath, DotGraphFormat format = DotGraphFormat.Svg, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(dot);
    return RenderCoreAsync(dot, null, outputPath, format, cancellationToken);
  }

  /// <inheritdoc/>
  public Task RenderFileAsync(string inputPath, string outputPath, DotGraphFormat format = DotGraphFormat.Svg, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
    var fullInputPath = Path.GetFullPath(inputPath);
    if (!File.Exists(fullInputPath))
      throw new FileNotFoundException("DOT input file was not found.", fullInputPath);

    ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    if (string.Equals(fullInputPath, Path.GetFullPath(outputPath), comparison))
      throw new ArgumentException("Input and output paths must differ.", nameof(outputPath));

    return RenderCoreAsync(null, fullInputPath, outputPath, format, cancellationToken);
  }

  private async Task RenderCoreAsync(string? dot, string? inputPath, string outputPath, DotGraphFormat format, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
    var formatName = format switch
    {
      DotGraphFormat.Svg => "svg",
      DotGraphFormat.Png => "png",
      _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    cancellationToken.ThrowIfCancellationRequested();

    if (_executablePath is null)
    {
      await BundledGraphviz.RenderAsync(dot, inputPath, outputPath, format, _timeout, cancellationToken).ConfigureAwait(false);
      return;
    }

    var fullOutputPath = Path.GetFullPath(outputPath);
    var outputDirectory = Path.GetDirectoryName(fullOutputPath)!;

    Directory.CreateDirectory(outputDirectory);
    var temporaryPath = Path.Combine(outputDirectory, Path.GetRandomFileName());

    using var timeout = new CancellationTokenSource(_timeout);
    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
    using var process = new Process();

    process.StartInfo = new ProcessStartInfo(_executablePath)
    {
      UseShellExecute = false,
      CreateNoWindow = true,
      RedirectStandardInput = dot is not null,
      RedirectStandardError = true,
      RedirectStandardOutput = true,
    };

    process.StartInfo.ArgumentList.Add("-T" + formatName);
    process.StartInfo.ArgumentList.Add("-o");
    process.StartInfo.ArgumentList.Add(temporaryPath);

    if (inputPath is not null)
      process.StartInfo.ArgumentList.Add(inputPath);

    try
    {
      try
      {
        process.Start();
      }
      catch (Win32Exception ex)
      {
        throw new InvalidOperationException($"Could not start Graphviz '{_executablePath}'. Install Graphviz and add dot to PATH, or supply its executable path.", ex);
      }

      // Drain both streams while writing input so large graphs/diagnostics cannot deadlock.
      var diagnosticsTask = process.StandardError.ReadToEndAsync();
      var outputTask = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);

      try
      {
        IOException? inputError = null;
        if (dot is not null)
        {
          try
          {
            // Close the pipe directly: closing the unused text writer can flush
            // to an already-closed pipe and mask Graphviz's error on Unix.
            await using var input = process.StandardInput.BaseStream;
            await input.WriteAsync(Encoding.UTF8.GetBytes(dot), cancellation.Token).ConfigureAwait(false);
          }
          catch (IOException ex)
          {
            // Graphviz may close stdin early on invalid DOT. Prefer its diagnostics.
            inputError = ex;
          }
        }

        await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        var diagnostics = await diagnosticsTask.ConfigureAwait(false);
        await outputTask.ConfigureAwait(false);
        cancellation.Token.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
          throw new InvalidOperationException($"Graphviz exited with code {process.ExitCode}: {diagnostics.Trim()}");

        if (inputError is not null)
          throw new IOException("Could not send DOT text to Graphviz.", inputError);

        if (!File.Exists(temporaryPath) || new FileInfo(temporaryPath).Length == 0)
          throw new InvalidOperationException("Graphviz produced no image. " + diagnostics.Trim());

        File.Move(temporaryPath, fullOutputPath, overwrite: true);
      }
      finally
      {
        if (!process.HasExited)
          process.Kill(entireProcessTree: true);

        await process.WaitForExitAsync().ConfigureAwait(false);
        await Task.WhenAll(diagnosticsTask, outputTask).ConfigureAwait(false);
      }
    }
    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
    {
      throw new TimeoutException($"Graphviz rendering exceeded {_timeout}.", ex);
    }
    finally
    {
      File.Delete(temporaryPath);
    }
  }
}
