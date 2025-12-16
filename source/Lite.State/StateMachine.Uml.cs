// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Lite.State;

/// <summary>
///   UML generating partial class.
///   * Basic state (Node) - Box
///   * Composite states - Circled
///   * Command state - Hexagons
/// </summary>
/// <typeparam name="TState">State machine type</typeparam>
public sealed partial class StateMachine<TState> where TState : struct, Enum
{
  /// <summary>
  ///   Exports a DOT (Graphviz) diagram of the state machine.
  ///   - Top-level machine (`rankdir=LR`)
  ///   - Composite states are shown as clustered subgraphs
  ///   - Command states are hexagons; terminal states (no transitions) are `doublecircle`
  ///   - Edge labels show Result (Ok, Error, Failure)
  /// </summary>
  /// <param name="includeSubmachines">Include composite submachines as subgraph clusters.</param>
  public string ExportUml(bool includeSubmachines = true)
  {
    var sb = new StringBuilder();
    sb.AppendLine("digraph StateMachine {");
    sb.AppendLine("  rankdir=LR;");
    sb.AppendLine("  compound=true;");
    sb.AppendLine("  node [fontname=\"Segoe UI\", fontsize=10];");
    sb.AppendLine("  edge [fontname=\"Segoe UI\", fontsize=10];");

    // Top-level start marker
    sb.AppendLine("  start [shape=point];");
    if (_states.TryGetValue(_initialState, out _))
    {
      sb.AppendLine($"  start -> \"{Escape(_initialState.ToString())}\";");
    }

    // Nodes
    foreach (var kv in _states)
      AppendNode(sb, kv.Value, DefaultTimeoutMs);

    // Edges
    foreach (var kv in _states)
      AppendEdges(sb, kv.Value);

    // Composite clusters
    if (includeSubmachines)
    {
      foreach (var kv in _states)
      {
        if (kv.Value is ICompositeState<TState> comp)
          AppendCompositeCluster(sb, comp, includeSubmachines, DefaultTimeoutMs);
      }
    }

    sb.AppendLine("}");
    return sb.ToString();
  }

  /// <summary>Escape backslash.</summary>
  /// <param name="s">Input string.</param>
  /// <returns>Sanitized string.</returns>
  private static string Escape(string s) => s.Replace("\"", "\\\"");

  private void AppendCompositeCluster(
    StringBuilder sb,
    ICompositeState<TState> comp,
    bool includeNested,
    int defaultTimeoutMs)
  {
    var label = Escape(comp.Id.ToString());
    var sub = comp.Submachine;

    sb.AppendLine($"  subgraph cluster_{label} {{");
    sb.AppendLine($"    label=\"{label}\"; style=rounded; color=lightgray; fontcolor=gray;");
    sb.AppendLine($"    rankdir=LR;");

    // Submachine start marker (only if initial is valid)
    sb.AppendLine($"    \"start_{label}\" [shape=point];");
    if (sub._states.TryGetValue(sub._initialState, out _))
      sb.AppendLine($"    \"start_{label}\" -> \"{Escape(sub._initialState.ToString())}\";");

    // Nodes in submachine
    foreach (var kv in sub._states)
      AppendSubNode(sb, kv.Value, defaultTimeoutMs);

    // Edges in submachine
    foreach (var kv in sub._states)
      AppendSubEdges(sb, kv.Value);

    // Nested composites (if any)
    if (includeNested)
      foreach (var kv in sub._states)
        if (kv.Value is ICompositeState<TState> nested)
          AppendCompositeCluster(sb, nested, includeNested, defaultTimeoutMs);

    sb.AppendLine("  }");
  }

  private void AppendEdges(StringBuilder sb, IState<TState> state)
  {
    var from = Escape(state.Id.ToString());
    foreach (var tr in state.Transitions)
    {
      var to = Escape(tr.Value.ToString());
      var label = tr.Key.ToString();
      sb.AppendLine($"  \"{from}\" -> \"{to}\" [label=\"{label}\"];");
    }
  }

  /// <summary>Add basic state node.</summary>
  /// <param name="sb"><see cref="StringBuilder"/>.</param>
  /// <param name="state"><see cref="IState{TState}"/>.</param>
  /// <param name="defaultTimeoutMs">Default Timeout to display.</param>
  private void AppendNode(StringBuilder sb, IState<TState> state, int defaultTimeoutMs)
  {
    var name = Escape(state.Id.ToString());

    var shape = "box";
    if (state is ICommandState<TState>)
      shape = "hexagon";
    else if (state.IsComposite)
      shape = "box3d";

    var isTerminal = state.Transitions.Count == 0 && !state.IsComposite;
    if (isTerminal)
      shape = "doublecircle";

    // Optional helpful tooltip for command states
    var attrs = new List<string> { $"shape={shape}" };
    if (state is ICommandState<TState> cmd)
    {
      var timeout = cmd.TimeoutMs ?? defaultTimeoutMs;
      attrs.Add($"tooltip=\"Command state (timeout={timeout}ms)\"");
    }

    // visually indicate composite
    if (state.IsComposite)
      attrs.Add("style=rounded");

    sb.AppendLine($"  \"{name}\" [{string.Join(", ", attrs)}];");
  }

  private void AppendSubEdges(StringBuilder sb, IState<TState> state)
  {
    var from = Escape(state.Id.ToString());
    foreach (var tr in state.Transitions)
    {
      var to = Escape(tr.Value.ToString());
      var label = tr.Key.ToString();
      sb.AppendLine($"    \"{from}\" -> \"{to}\" [label=\"{label}\"];");
    }
  }

  private void AppendSubNode(StringBuilder sb, IState<TState> state, int defaultTimeoutMs)
  {
    var name = Escape(state.Id.ToString());

    var shape = "box";
    if (state is ICommandState<TState>)
      shape = "hexagon";
    else if (state.IsComposite)
      shape = "box3d";

    var isTerminal = state.Transitions.Count == 0 && !state.IsComposite;

    if (isTerminal)
      shape = "doublecircle";

    var attrs = new List<string> { $"shape={shape}" };
    if (state is ICommandState<TState> cmd)
    {
      var timeout = cmd.TimeoutMs ?? defaultTimeoutMs;
      attrs.Add($"tooltip=\"Command state (timeout={timeout}ms)\"");
    }

    if (state.IsComposite)
      attrs.Add("style=rounded");

    sb.AppendLine($"    \"{name}\" [{string.Join(", ", attrs)}];");
  }
}
