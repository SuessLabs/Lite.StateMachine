// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

public static class ExpectedUmlData
{
  public static string UmlLegend =>
    """
    subgraph cluster_legend {
      label="Legend"; style=rounded; color=gray; fontcolor=gray;
      rankdir=LR;
      legend_start [label="Start (initial marker)", shape=plaintext];
      legend_start_sym [shape=point, label=""];
      legend_start_sym -> legend_start [style=invis];
      legend_regular [label="Regular state", shape=plaintext];
      legend_regular_sym [shape=box, label=""];
      legend_regular_sym -> legend_regular [style=invis];
      legend_composite [label="Composite (has submachine)", shape=plaintext];
      legend_composite_sym [shape=box3d, style=rounded, label=""];
      legend_composite_sym -> legend_composite [style=invis];
      legend_command [label="Command state (message-driven, timeout)", shape=plaintext];
      legend_command_sym [shape=hexagon, label=""];
      legend_command_sym -> legend_command [style=invis];
      legend_terminal [label="Terminal state (no outgoing transitions)", shape=plaintext];
      legend_terminal_sym [shape=doublecircle, label=""];
      legend_terminal_sym -> legend_terminal [style=invis];
      legend_edge [label="Edges labeled by outcome: Ok, Error, Failure", shape=plaintext];
      legend_edge_a [shape=box, label="State A"];
      legend_edge_b [shape=box, label="State B"];
      legend_edge_a -> legend_edge_b [label="Ok"];
    }
  """;

  public static string BasicStates(bool hasLegend = false) => $$"""
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State3" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];{{(hasLegend == true
        ? System.Environment.NewLine + UmlLegend
        : string.Empty)}}
    }
    """;

  public static string BasicStatesWithError(bool hasLegend = false) => $$"""
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State2e" [shape=box];
      "State3" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
      "State2" -> "State2e" [label="Error"];
      "State2e" -> "State2" [label="Ok"];{{(hasLegend == true
        ? System.Environment.NewLine + UmlLegend
        : string.Empty)}}
    }
    """;

  public static string BasicStatesWithErrorFailure(bool hasLegend = false) => $$"""
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State2e" [shape=box];
      "State2f" [shape=box];
      "State3" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
      "State2" -> "State2e" [label="Error"];
      "State2" -> "State2f" [label="Failure"];
      "State2e" -> "State2" [label="Ok"];
      "State2f" -> "State1" [label="Ok"];{{(hasLegend == true
        ? System.Environment.NewLine + UmlLegend
        : string.Empty)}}
    }
    """;

  public static string Composite(bool hasLegend = false) => $$"""
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State2e" [shape=box];
      "State2f" [shape=box];
      "State3" [shape=box];
      "State4" [shape=box3d, style=rounded];
      "State5" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
      "State2" -> "State2e" [label="Error"];
      "State2" -> "State2f" [label="Failure"];
      "State2e" -> "State2" [label="Ok"];
      "State2f" -> "State1" [label="Ok"];
      "State3" -> "State4" [label="Ok"];
      "State4" -> "State5" [label="Ok"];
      subgraph cluster_State4 {
        label="State4"; style=rounded; color=lightgray; fontcolor=gray;
        rankdir=LR;
        "start_State4" [shape=point];
        "start_State4" -> "State4_Sub1";
        "State4_Sub1" [shape=box];
        "State4_Sub2" [shape=doublecircle];
        "State4_Sub1" -> "State4_Sub2" [label="Ok"];
      }{{(hasLegend == true
        ? System.Environment.NewLine + UmlLegend
        : string.Empty)}}
    }
    """;
}
