// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lite.StateMachine;

/// <summary>
///   UML generating partial class.
///   * State (Node) - Rounded Box.
///   * Composite states - Rounded Box.
///   * Command state - Rounded Box.
/// </summary>
/// <remarks>
///   vNext: Custom filled background for state types.
///     - Standard: Transparent
///     - Command: Yellow.
///     - Composite: Green.
/// </remarks>
/// <typeparam name="TStateId">State machine type.</typeparam>
public sealed partial class StateMachine<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Export the state machine's topology as DOT (Graphviz).</summary>
  /// <param name="initialStateIds">
  ///   Optional entry-point initial state(s) at the top-level. When NULL or empty, no global start marker is emitted.
  ///   Initial states denoted by a filled circle (start marker) → state.
  ///
  ///   NOTE: Multiple `initialStateId` can occur in an architecture:
  ///   <![CDATA[
  ///     if (appLicense == License.Pro)  await machine.RunAsync(StateId.EntryProState);
  ///     if (appLicense == License.Demo) await machine.RunAsync(StateId.EntryDemoState);
  ///   ]]>.
  /// </param>
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
  /// <param name="showParentInitialEdge">Whether to also draw the parent → initial-child edge (green, labeled "initial") for composite parents. Default true.</param>
  /// <returns>DOT text suitable for Graphviz.</returns>
  public string ExportUml(
    IEnumerable<TStateId>? initialStateIds = null,
    bool includeLegend = true,
    IDictionary<Result, string>? transitionColors = null,
    string parentToChildColor = "Green",
    string graphName = "StateMachine",
    bool rankLeftToRight = true,
    Func<TStateId, string>? nodeLabelSelector = null,
    string? legendText = null,
    bool showParentInitialEdge = true)
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

    // Composite parents + their children
    foreach (var parent in regs.Where(r => r.IsCompositeParent))
    {
      var parentId = parent.StateId;
      var clusterName = $"cluster_{Escape(parentId.ToString())}";
      sb.AppendLine($"  subgraph {clusterName} {{");
      sb.AppendLine($"    label=\"{Escape(nodeLabelSelector(parentId))}\";");
      sb.AppendLine($"    style=rounded;");
      sb.AppendLine($"    color=\"#888888\";");

      // Parent node inside the cluster (rounded rectangle)
      sb.AppendLine($"    \"{Escape(parentId.ToString())}\" [shape=box, style=rounded, label=\"{Escape(nodeLabelSelector(parentId))}\"];");

      // Child nodes under this parent (rounded rectangles)
      var children = regs.Where(r => Equals(r.ParentId, parentId)).ToList();
      foreach (var child in children)
      {
        var childId = child.StateId;
        sb.AppendLine($"    \"{Escape(childId.ToString())}\" [shape=box, style=rounded, label=\"{Escape(nodeLabelSelector(childId))}\"];");
      }

      // Internal START marker (filled black circle) if InitialChildId present
      if (parent.InitialChildId is not null)
      {
        var startClusterId = $"start_{Escape(parentId.ToString())}";
        sb.AppendLine($"    \"{startClusterId}\" [shape=circle, style=filled, fillcolor=\"black\", color=\"black\", label=\"\", width=0.25, height=0.25, fixedsize=true];");
        sb.AppendLine($"    \"{startClusterId}\" -> \"{Escape(parent.InitialChildId.Value.ToString())}\" [color=\"black\", label=\"start\"];");
      }

      // Internal final node for the composite cluster (double circle, inner filled black)
      var finalClusterId = $"final_{Escape(parentId.ToString())}";
      sb.AppendLine($"    \"{finalClusterId}\" [shape=doublecircle, style=filled, fillcolor=\"black\", color=\"black\", label=\"\", width=0.35, height=0.35, fixedsize=true];");

      sb.AppendLine("  }");
    }

    // NON-COMPOSITE root-level nodes (rounded rectangles)
    foreach (var reg in regs.Where(r => !r.IsCompositeParent && r.ParentId is null))
    {
      var id = reg.StateId;
      sb.AppendLine($"  \"{Escape(id.ToString())}\" [shape=box, style=rounded, label=\"{Escape(nodeLabelSelector(id))}\"];");
    }

    // Parent → initial child (green edge), optional
    if (showParentInitialEdge)
    {
      foreach (var parent in regs.Where(r => r.IsCompositeParent && r.InitialChildId is not null))
      {
        var parentId = parent.StateId;
        var childId = parent.InitialChildId!.Value;
        sb.AppendLine($"  \"{Escape(parentId.ToString())}\" -> \"{Escape(childId.ToString())}\" [color=\"{Escape(parentToChildColor)}\", label=\"initial\"];");
      }
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

    // LAST SUBSTATES: point to cluster's internal final node
    foreach (var parent in regs.Where(r => r.IsCompositeParent))
    {
      var parentId = parent.StateId;
      var finalClusterId = $"final_{Escape(parentId.ToString())}";

      var children = regs.Where(r => Equals(r.ParentId, parentId)).ToList();
      foreach (var child in children)
      {
        // "last" child = no outgoing transitions
        if (IsTerminal(child))
          sb.AppendLine($"  \"{Escape(child.StateId.ToString())}\" -> \"{finalClusterId}\" [color=\"black\", label=\"final\"];");
      }
    }

    // Setting of Initial Top-Level State(s). Because devs can `RunAsync(initialStateId)` anything
    // based on app logic, we provide the option for multiple starting points.
    // Consider this:
    //       if (appLicense == License.Pro)   await machine.RunAsync(StateId.EntryProState);
    //  else if (appLicense == License.Demo)  await machine.RunAsync(StateId.EntryDemoState);
    //
    // GLOBAL START marker (filled black circle) → initial top-level states
    var initialRoots = initialStateIds?.ToList() ?? new List<TStateId>();
    if (initialRoots.Count > 0)
    {
      var globalStartId = "start_global";
      sb.AppendLine($"  \"{globalStartId}\" [shape=circle, style=filled, fillcolor=\"black\", color=\"black\", label=\"\", width=0.25, height=0.25, fixedsize=true];");
      foreach (var init in initialRoots)
        sb.AppendLine($"  \"{globalStartId}\" -> \"{Escape(init.ToString())}\" [color=\"black\", label=\"start\"];");
    }

    // --- LAST TOP-LEVEL STATES: GLOBAL FINAL marker (double circle, inner filled black)
    var globalFinalId = "final_global";
    sb.AppendLine($"  \"{globalFinalId}\" [shape=doublecircle, style=filled, fillcolor=\"black\", color=\"black\", label=\"\", width=0.35, height=0.35, fixedsize=true];");

    foreach (var reg in regs.Where(r => r.ParentId is null))
    {
      if (IsTerminal(reg))
        sb.AppendLine($"  \"{Escape(reg.StateId.ToString())}\" -> \"{globalFinalId}\" [color=\"black\", label=\"final\"];");
    }

    // Optional legend
    if (includeLegend)
    {
      sb.AppendLine("  subgraph cluster_legend {");
      sb.AppendLine("    label=\"Legend\";");
      sb.AppendLine("    style=dashed;");
      sb.AppendLine("    color=\"#BBBBBB\";");

      sb.AppendLine("    legend_state [shape=box, style=rounded, label=\"State (rounded rectangle)\"];");
      sb.AppendLine("    legend_start [shape=circle, style=filled, fillcolor=\"black\", label=\"\"];");
      sb.AppendLine("    legend_final [shape=doublecircle, style=filled, fillcolor=\"black\", label=\"\"];");

      var legendOkColor = Escape(colors[Result.Ok]);
      var legendErrColor = Escape(colors[Result.Error]);
      var legendFailColor = Escape(colors[Result.Failure]);
      var legendParentColor = Escape(parentToChildColor);

      sb.AppendLine("    legend_start -> legend_state [color=\"black\", label=\"start\"];");
      sb.AppendLine($"    legend_state -> legend_state_ok   [color=\"{legendOkColor}\",   label=\"Ok\"];");
      sb.AppendLine($"    legend_state -> legend_state_err  [color=\"{legendErrColor}\", label=\"Error\"];");
      sb.AppendLine($"    legend_state -> legend_state_fail [color=\"{legendFailColor}\", label=\"Failure\"];");
      sb.AppendLine($"    legend_state -> legend_state_init [color=\"{legendParentColor}\", label=\"parent → initial child\"];");

      sb.AppendLine("    legend_state_ok   [shape=point, label=\"\"];");
      sb.AppendLine("    legend_state_err  [shape=point, label=\"\"];");
      sb.AppendLine("    legend_state_fail [shape=point, label=\"\"];");
      sb.AppendLine("    legend_state_init [shape=point, label=\"\"];");

      var legendBody = legendText ??
        $"Shapes:\n  • rounded rectangle = State\n  • filled circle = Start\n  • double circle (filled) = Final\n\n" +
        $"Edge colors:\n  • Ok = {colors[Result.Ok]}\n  • Error = {colors[Result.Error]}\n  • Failure = {colors[Result.Failure]}\n  • Parent→Child = {parentToChildColor}\n  • Start/Final edges = black";

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

  private static bool IsTerminal(StateRegistration<TStateId> reg) =>
    reg.OnSuccess is null && reg.OnError is null && reg.OnFailure is null;
}
