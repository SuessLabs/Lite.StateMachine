// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lite.StateMachine;

/// <summary>
///   UML generating partial class.
///   * Basic state (Node) - Box.
///   * Composite states - Circled.
///   * Command state - Hexagons.
/// </summary>
/// <typeparam name="TStateId">State machine type.</typeparam>
public sealed partial class StateMachine<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Export the state machine's topology as DOT (Graphviz).</summary>
  /// <param name="includeLegend">Include a legend subgraph that explains shapes and colors.</param>
  /// <param name="transitionColors">
  ///   Optional color overrides for Result-based transitions. Keys not present fall back to defaults:
  ///   Ok=Blue, Error=Yellow, Failure=Red.
  /// </param>
  /// <param name="parentToChildColor">Color used for parent → initial-child transition edges (default "Green").</param>
  /// <param name="graphName">Logical graph name (default "StateMachine").</param>
  /// <param name="rankLeftToRight">If true, left-to-right layout (DOT: rankdir=LR), else top-to-bottom.</param>
  /// <param name="nodeLabelSelector">Optional label selector for nodes. Defaults to <c>stateId.ToString()</c>.</param>
  /// <param name="legendText">Optional custom legend description text. If null, a sensible default is emitted.</param>
  /// <returns>DOT text suitable for Graphviz.</returns>
  public string ExportUml(
    bool includeLegend = true,
    IDictionary<Result, string>? transitionColors = null,
    string parentToChildColor = "Green",
    string graphName = "StateMachine",
    bool rankLeftToRight = true,
    Func<TStateId, string>? nodeLabelSelector = null,
    string? legendText = null)
  {
    // Defaults for Result edge colors
    var colors = new Dictionary<Result, string>
    {
      [Result.Ok] = "Blue",
      [Result.Error] = "Yellow",
      [Result.Failure] = "Red",
    };

    if (transitionColors != null)
    {
      foreach (var kvp in transitionColors)
        colors[kvp.Key] = kvp.Value ?? colors[kvp.Key];
    }

    nodeLabelSelector ??= id => id.ToString();

    var sb = new StringBuilder();
    sb.AppendLine($"digraph \"{Escape(graphName)}\" {{");

    // allow edges into subgraphs
    sb.AppendLine("  compound=true;");
    if (rankLeftToRight)
      sb.AppendLine("  rankdir=LR;");

    sb.AppendLine("  fontsize=12;");

    // First, define all nodes and cluster composite parents with their children.
    // Group registrations by ParentId (null = root-level nodes).
    var regs = _states.Values.ToList();

    // Emit clusters for composite parents
    foreach (var parent in regs.Where(r => r.IsCompositeParent))
    {
      var parentId = parent.StateId;
      var clusterName = $"cluster_{Escape(parentId.ToString())}";
      sb.AppendLine($"  subgraph {clusterName} {{");
      sb.AppendLine($"    label=\"{Escape(nodeLabelSelector(parentId))}\";");
      sb.AppendLine($"    style=rounded;");
      sb.AppendLine($"    color=\"#888888\";");

      // Emit parent node inside the cluster (doublecircle)
      sb.AppendLine($"    \"{Escape(parentId.ToString())}\" [shape=doublecircle, label=\"{Escape(nodeLabelSelector(parentId))}\"];");

      // Emit children under this parent (ellipse)
      var children = regs.Where(r => Equals(r.ParentId, parentId)).ToList();
      foreach (var child in children)
      {
        var childId = child.StateId;
        sb.AppendLine($"    \"{Escape(childId.ToString())}\" [shape=ellipse, label=\"{Escape(nodeLabelSelector(childId))}\"];");
      }

      // End of cluster
      sb.AppendLine("  }");
    }

    // Emit nodes that are NOT part of any composite parent cluster
    foreach (var reg in regs.Where(r => !r.IsCompositeParent && r.ParentId is null))
    {
      var id = reg.StateId;
      sb.AppendLine($"  \"{Escape(id.ToString())}\" [shape=ellipse, label=\"{Escape(nodeLabelSelector(id))}\"];");
    }

    // Parent → initial child edges (green)
    foreach (var parent in regs.Where(r => r.IsCompositeParent && r.InitialChildId is not null))
    {
      var parentId = parent.StateId;
      var childId = parent.InitialChildId!.Value;
      sb.AppendLine($"  \"{Escape(parentId.ToString())}\" -> \"{Escape(childId.ToString())}\" [color=\"{Escape(parentToChildColor)}\", label=\"initial\"];");
    }

    // Result-based transitions (Ok, Error, Failure) with labels and color overrides
    foreach (var reg in regs)
    {
      var fromId = reg.StateId;

      void Emit(Result res, TStateId? toId)
      {
        if (toId is null)
          return;

        var color = colors.TryGetValue(res, out var c) ? c : "Black";
        var label = res.ToString();
        sb.AppendLine($"  \"{Escape(fromId.ToString())}\" -> \"{Escape(toId.Value.ToString())}\" [color=\"{Escape(color)}\", label=\"{Escape(label)}\"];");
      }

      Emit(Result.Ok, reg.OnSuccess);
      Emit(Result.Error, reg.OnError);
      Emit(Result.Failure, reg.OnFailure);
    }

    // Optional legend
    if (includeLegend)
    {
      sb.AppendLine("  subgraph cluster_legend {");
      sb.AppendLine("    label=\"Legend\";");
      sb.AppendLine("    style=dashed;");
      sb.AppendLine("    color=\"#BBBBBB\";");

      sb.AppendLine("    legend_composite [shape=doublecircle, label=\"Composite parent\"];");
      sb.AppendLine("    legend_leaf [shape=ellipse, label=\"Leaf state\"];");

      var legendOkColor = Escape(colors[Result.Ok]);
      var legendErrColor = Escape(colors[Result.Error]);
      var legendFailColor = Escape(colors[Result.Failure]);
      var legendParentColor = Escape(parentToChildColor);

      sb.AppendLine($"    legend_composite -> legend_leaf [color=\"{legendParentColor}\", label=\"parent → initial child\"];");
      sb.AppendLine($"    legend_leaf -> legend_leaf_ok   [color=\"{legendOkColor}\",   label=\"Ok\"];");
      sb.AppendLine($"    legend_leaf -> legend_leaf_err  [color=\"{legendErrColor}\", label=\"Error\"];");
      sb.AppendLine($"    legend_leaf -> legend_leaf_fail [color=\"{legendFailColor}\", label=\"Failure\"];");

      sb.AppendLine("    legend_leaf_ok   [shape=point, label=\"\"];");
      sb.AppendLine("    legend_leaf_err  [shape=point, label=\"\"];");
      sb.AppendLine("    legend_leaf_fail [shape=point, label=\"\"];");

      var legendBody = legendText ??
        $"Shapes:\n  • doublecircle = Composite parent\n  • ellipse = Leaf state\n\n" +
        $"Edge colors:\n  • Ok = {colors[Result.Ok]}\n  • Error = {colors[Result.Error]}\n  • Failure = {colors[Result.Failure]}\n  • Parent→Child = {parentToChildColor}";

      sb.AppendLine($"    legend_note [shape=note, label=\"{Escape(legendBody)}\"];");
      sb.AppendLine("  }");
    }

    sb.AppendLine("}");
    return sb.ToString();
  }

  private static string Escape(string s)
  {
    if (s is null)
      return string.Empty;

    return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
  }
}
