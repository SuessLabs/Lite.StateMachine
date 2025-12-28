// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
/*
using System;
using System.Collections.Generic;
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
  /// <summary>
  ///   Exports a DOT (Graphviz) diagram of the state machine.
  ///   - Top-level machine (`rankdir=LR`)
  ///   - Composite states are shown as clustered subgraphs
  ///   - Command states are hexagons;
  ///   - Terminal/final states (no transitions) are `doublecircle`
  ///   - Edge labels show Result (Ok, Error, Failure).
  /// </summary>
  /// <param name="includeSubmachines">Include composite submachines as subgraph clusters.</param>
  /// <param name="appendLegend">Include symbols legend.</param>
  /// <returns>State diagram.</returns>
  public string ExportUml(bool includeSubmachines = true, bool appendLegend = false)
  {
    var sb = new StringBuilder();
    sb.AppendLine("digraph StateMachine {");
    sb.AppendLine("  rankdir=LR;");
    sb.AppendLine("  compound=true;");
    sb.AppendLine("  node [fontname=\"Segoe UI\", fontsize=10];");
    sb.AppendLine("  edge [fontname=\"Segoe UI\", fontsize=10];");

    // Start marker
    sb.AppendLine("  start [shape=point];");
    if (_states.ContainsKey(_initialState))
      sb.AppendLine($"  start -> \"{Escape(_initialState.ToString())}\";");

    // Nodes
    foreach (var kv in _states)
    {
      var instance = GetEphemeralInstance(kv.Value);
      AppendNode(sb, instance, DefaultTimeoutMs);
    }

    // Edges
    foreach (var kv in _states)
    {
      var instance = GetEphemeralInstance(kv.Value);
      AppendEdges(sb, instance);
    }

    // Composite clusters
    if (includeSubmachines)
    {
      foreach (var kv in _states)
      {
        var instance = GetEphemeralInstance(kv.Value);
        if (instance is ICompositeState<TStateId> comp)
          AppendCompositeCluster(sb, kv.Key, kv.Value, includeSubmachines, DefaultTimeoutMs);
      }
    }

    if (appendLegend)
      AppendLegend(sb);

    sb.AppendLine("}");
    return sb.ToString();
  }

  /// <summary>Escape backslash.</summary>
  /// <param name="s">Input string.</param>
  /// <returns>Sanitized string.</returns>
  private static string Escape(string s) => s.Replace("\"", "\\\"");

  private void AppendCompositeCluster(
    StringBuilder sb,
    TStateId compositeId,
    StateRegistration<TStateId> reg,
    bool includeNested,
    int defaultTimeoutMs)
  {
    var label = Escape(compositeId.ToString());

    // Build an ephemeral instance with an ephemeral submachine
    var instance = GetEphemeralInstance(reg);
    var comp = (ICompositeState<TStateId>)instance;
    var sub = comp.Submachine;

    sb.AppendLine($"  subgraph cluster_{label} {{");
    sb.AppendLine($"    label=\"{label}\"; style=rounded; color=lightgray; fontcolor=gray;");
    sb.AppendLine($"    rankdir=LR;");
    sb.AppendLine($"    \"start_{label}\" [shape=point];");

    // Submachine initial (if set)
    var subInitialKnown = sub._states.ContainsKey(sub._initialState);
    if (subInitialKnown)
      sb.AppendLine($"    \"start_{label}\" -> \"{Escape(sub._initialState.ToString())}\";");

    // Nodes
    foreach (var kv in sub._states)
    {
      var subInstance = sub.GetEphemeralInstance(kv.Value);
      AppendSubNode(sb, subInstance, defaultTimeoutMs);
    }

    // Edges
    foreach (var kv in sub._states)
    {
      var subInstance = sub.GetEphemeralInstance(kv.Value);
      AppendSubEdges(sb, subInstance);
    }

    // Nested composites
    if (includeNested)
    {
      foreach (var kv in sub._states)
      {
        var nestedInstance = sub.GetEphemeralInstance(kv.Value);
        if (nestedInstance is ICompositeState<TStateId> nestedComp)
          sub.AppendCompositeCluster(sb, kv.Key, kv.Value, includeNested, defaultTimeoutMs);
      }
    }

    sb.AppendLine("  }");
  }

  private void AppendEdges(StringBuilder sb, IState<TStateId> state)
  {
    var from = Escape(state.StateId.ToString());
    foreach (var tr in state.Transitions)
    {
      var to = Escape(tr.Value.ToString());
      var label = tr.Key.ToString();
      sb.AppendLine($"  \"{from}\" -> \"{to}\" [label=\"{label}\"];");
    }
  }

  private void AppendLegend(StringBuilder sb)
  {
    sb.AppendLine("  subgraph cluster_legend {");
    sb.AppendLine("    label=\"Legend\"; style=rounded; color=gray; fontcolor=gray;");
    sb.AppendLine("    rankdir=LR;");

    sb.AppendLine("    legend_start [label=\"Start (initial marker)\", shape=plaintext];");
    sb.AppendLine("    legend_start_sym [shape=point, label=\"\"];");
    sb.AppendLine("    legend_start_sym -> legend_start [style=invis];");

    sb.AppendLine("    legend_regular [label=\"Regular state\", shape=plaintext];");
    sb.AppendLine("    legend_regular_sym [shape=box, label=\"\"];");
    sb.AppendLine("    legend_regular_sym -> legend_regular [style=invis];");

    sb.AppendLine("    legend_composite [label=\"Composite (has submachine)\", shape=plaintext];");
    sb.AppendLine("    legend_composite_sym [shape=box3d, style=rounded, label=\"\"];");
    sb.AppendLine("    legend_composite_sym -> legend_composite [style=invis];");

    sb.AppendLine("    legend_command [label=\"Command state (message-driven, timeout)\", shape=plaintext];");
    sb.AppendLine("    legend_command_sym [shape=hexagon, label=\"\"];");
    sb.AppendLine("    legend_command_sym -> legend_command [style=invis];");

    sb.AppendLine("    legend_terminal [label=\"Terminal state (no outgoing transitions)\", shape=plaintext];");
    sb.AppendLine("    legend_terminal_sym [shape=doublecircle, label=\"\"];");
    sb.AppendLine("    legend_terminal_sym -> legend_terminal [style=invis];");

    sb.AppendLine("    legend_edge [label=\"Edges labeled by outcome: Ok, Error, Failure\", shape=plaintext];");
    sb.AppendLine("    legend_edge_a [shape=box, label=\"State A\"];");
    sb.AppendLine("    legend_edge_b [shape=box, label=\"State B\"];");
    sb.AppendLine("    legend_edge_a -> legend_edge_b [label=\"Ok\"];");

    sb.AppendLine("  }");
  }

  private void AppendNode(StringBuilder sb, IState<TStateId> state, int defaultTimeoutMs)
  {
    var name = Escape(state.StateId.ToString());

    var shape = "box";
    if (state is ICommandState<TStateId>)
      shape = "hexagon";
    else if (state.IsComposite)
      shape = "box3d";

    var isTerminal = state.Transitions.Count == 0 && !state.IsComposite;
    if (isTerminal)
      shape = "doublecircle";

    var attrs = new List<string> { $"shape={shape}" };
    if (state is ICommandState<TStateId> cmd)
    {
      var timeout = cmd.TimeoutMs ?? defaultTimeoutMs;
      attrs.Add($"tooltip=\"Command state (timeout={timeout}ms)\"");
    }

    if (state.IsComposite)
      attrs.Add("style=rounded");

    sb.AppendLine($"  \"{name}\" [{string.Join(", ", attrs)}];");
  }

  private void AppendSubEdges(StringBuilder sb, IState<TStateId> state)
  {
    var from = Escape(state.StateId.ToString());
    foreach (var tr in state.Transitions)
    {
      var to = Escape(tr.Value.ToString());
      var label = tr.Key.ToString();
      sb.AppendLine($"    \"{from}\" -> \"{to}\" [label=\"{label}\"];");
    }
  }

  private void AppendSubNode(StringBuilder sb, IState<TStateId> state, int defaultTimeoutMs)
  {
    var name = Escape(state.StateId.ToString());

    var shape = "box";
    if (state is ICommandState<TStateId>)
      shape = "hexagon";
    else if (state.IsComposite)
      shape = "box3d";

    var isTerminal = state.Transitions.Count == 0 && !state.IsComposite;
    if (isTerminal)
      shape = "doublecircle";

    var attrs = new List<string> { $"shape={shape}" };
    if (state is ICommandState<TStateId> cmd)
    {
      var timeout = cmd.TimeoutMs ?? defaultTimeoutMs;
      attrs.Add($"tooltip=\"Command state (timeout={timeout}ms)\"");
    }

    if (state.IsComposite)
      attrs.Add("style=rounded");

    sb.AppendLine($"    \"{name}\" [{string.Join(", ", attrs)}];");
  }

  /// <summary>Create a very short instance of the state to extract the transitions.</summary>
  /// <param name="reg">State registration.</param>
  /// <returns>State instance.</returns>
  private IState<TStateId> GetEphemeralInstance(StateRegistration<TStateId> reg)
  {
    if (reg is null || reg.Factory is null)
      throw new NullReferenceException("Invalid or missing state factory.");

    var state = reg.Factory();

    var x = reg.Factory.GetType();

    // Because we're not starting the machine, we need to manually add set the StateId.
    state.SetStateId(reg.StateId);

    if (reg.OnSuccess is not null)
      (state as BaseState<TStateId>)?.AddTransition(Result.Ok, reg.OnSuccess.Value);

    if (reg.OnError is not null)
      (state as BaseState<TStateId>)?.AddTransition(Result.Error, reg.OnError.Value);

    if (reg.OnFailure is not null)
      (state as BaseState<TStateId>)?.AddTransition(Result.Failure, reg.OnFailure.Value);

    // For composites: build an ephemeral submachine for topology inspection (no Start).
    if (state is ICompositeState<TStateId> comp && reg.ConfigureSubmachine != null)
    {
      // TOOD (2025-12-25): May be able to just pass: (NULL, NULL)
      var sub = new StateMachine<TStateId>(_containerFactory, _eventAggregator);
      comp.Submachine = sub;
      reg.ConfigureSubmachine(sub);
    }

    return state;
  }
}
*/
