// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Lite.StateMachine.DotGraph;

/// <summary>Renders DOT graphs independently of the layout engine's deployment.</summary>
public interface IDotGraphRenderer
{
  /// <summary>Renders DOT text to an image file.</summary>
  /// <param name="dot">DOT text, for example from StateMachine.ExportUml().</param>
  /// <param name="outputPath">Destination image path. Existing files are replaced on success.</param>
  /// <param name="format">Output format, independent of the destination extension.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A task representing image generation.</returns>
  Task RenderAsync(string dot, string outputPath, DotGraphFormat format = DotGraphFormat.Svg, CancellationToken cancellationToken = default);

  /// <summary>Renders an existing DOT file to an image file.</summary>
  /// <param name="inputPath">DOT file path.</param>
  /// <param name="outputPath">Destination image path. Existing files are replaced on success.</param>
  /// <param name="format">Output format, independent of the destination extension.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A task representing image generation.</returns>
  Task RenderFileAsync(string inputPath, string outputPath, DotGraphFormat format = DotGraphFormat.Svg, CancellationToken cancellationToken = default);
}
