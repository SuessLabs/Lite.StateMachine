// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

public static class ExpectedUmlData
{
  public static string UmlLegend =>
    """
    subgraph cluster_legend {
      label="Legend";
      style=dashed;
      color="#BBBBBB";
      legend_state [shape=box, style=rounded, label="State (rounded rectangle)"];
      legend_start [shape=circle, style=filled, fillcolor="black", label=""];
      legend_final [shape=doublecircle, style=filled, fillcolor="black", label=""];
      legend_start -> legend_state [color="black", label="start"];
      legend_state -> legend_state_ok   [color="Blue",   label="Success"];
      legend_state -> legend_state_err  [color="Yellow", label="Error"];
      legend_state -> legend_state_fail [color="Red", label="Failure"];
      legend_state -> legend_state_init [color="Green", label="parent → initial child"];
      legend_state_ok   [shape=point, label=""];
      legend_state_err  [shape=point, label=""];
      legend_state_fail [shape=point, label=""];
      legend_state_init [shape=point, label=""];
      legend_note [shape=note, label="Shapes:\n  • rounded rectangle = State\n  • filled circle = Start\n  • double circle (filled) = Final\n\nEdge colors:\n  • Success = Blue\n  • Error = Yellow\n  • Failure = Red\n  • Parent→Child = Green\n  • Start/Final edges = black"];
    }
  """;

  public static string BasicStates123(bool hasLegend = false) => $$"""
    digraph "BasicStateMachine" {
      compound=true;
      rankdir=LR;
      fontsize=12;
      "State1" [shape=box, style=rounded, label="State1"];
      "State2" [shape=box, style=rounded, label="State2"];
      "State3" [shape=box, style=rounded, label="State3"];
      "State1" -> "State2" [color="Blue", label="Success"];
      "State2" -> "State3" [color="Blue", label="Success"];
      "start_global" [shape=circle, style=filled, fillcolor="black", color="black", label="", width=0.25, height=0.25, fixedsize=true];
      "start_global" -> "State1" [color="black", label="start"];
      "final_global" [shape=doublecircle, style=filled, fillcolor="black", color="black", label="", width=0.35, height=0.35, fixedsize=true];
      "State3" -> "final_global" [color="black", label="final"];{{(hasLegend == true
        ? System.Environment.NewLine + UmlLegend
        : string.Empty)}}
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
      "State1" -> "State2" [label="Success"];
      "State2" -> "State3" [label="Success"];{{(hasLegend == true
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
      "State1" -> "State2" [label="Success"];
      "State2" -> "State3" [label="Success"];
      "State2" -> "State2e" [label="Error"];
      "State2e" -> "State2" [label="Success"];{{(hasLegend == true
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
      "State1" -> "State2" [label="Success"];
      "State2" -> "State3" [label="Success"];
      "State2" -> "State2e" [label="Error"];
      "State2" -> "State2f" [label="Failure"];
      "State2e" -> "State2" [label="Success"];
      "State2f" -> "State1" [label="Success"];{{(hasLegend == true
        ? System.Environment.NewLine + UmlLegend
        : string.Empty)}}
    }
    """;

  public static string Composite(bool hasLegend = false) => $$"""
    digraph "StateMachine" {
      compound=true;
      rankdir=LR;
      fontsize=12;
      "State1" [shape=box, style=rounded, label="State1"];
      "State2" [shape=box, style=rounded, label="State2"];
      "State3" [shape=box, style=rounded, label="State3"];
      "State1" -> "State2" [color="Blue", label="Success"];
      "State2" -> "State3" [color="Blue", label="Success"];
      "State2_Sub1" -> "State2_Sub2" [color="Blue", label="Success"];
      "start_global" [shape=circle, style=filled, fillcolor="black", color="black", label="", width=0.25, height=0.25, fixedsize=true];
      "start_global" -> "State1" [color="black", label="start"];
      "final_global" [shape=doublecircle, style=filled, fillcolor="black", color="black", label="", width=0.35, height=0.35, fixedsize=true];
      "State3" -> "final_global" [color="black", label="final"];{{(hasLegend == true
          ? System.Environment.NewLine + UmlLegend
          : string.Empty)}}
    }
    """;

  public static string CompositeWithErrorFailure(bool hasLegend = false) => $$"""
    digraph "StateMachine" {
      compound=true;
      rankdir=LR;
      fontsize=12;
      subgraph cluster_Parent {
        label="Parent";
        style=rounded;
        color="#888888";
        "Parent" [shape=box, style=rounded, label="Parent"];
        "Parent_Fetch" [shape=box, style=rounded, label="Parent_Fetch"];
        "Parent_WaitMessage" [shape=box, style=rounded, label="Parent_WaitMessage"];
        "start_Parent" [shape=circle, style=filled, fillcolor="black", color="black", label="", width=0.25, height=0.25, fixedsize=true];
        "start_Parent" -> "Parent_Fetch" [color="black", label="start"];
        "final_Parent" [shape=doublecircle, style=filled, fillcolor="black", color="black", label="", width=0.35, height=0.35, fixedsize=true];
      }
      "Entry" [shape=box, style=rounded, label="Entry"];
      "Done" [shape=box, style=rounded, label="Done"];
      "Error" [shape=box, style=rounded, label="Error"];
      "Failure" [shape=box, style=rounded, label="Failure"];
      "Parent" -> "Parent_Fetch" [color="Green", label="initial"];
      "Entry" -> "Parent" [color="Blue", label="Success"];
      "Parent" -> "Done" [color="Blue", label="Success"];
      "Parent" -> "Error" [color="Yellow", label="Error"];
      "Parent" -> "Failure" [color="Red", label="Failure"];
      "Parent_Fetch" -> "Parent_WaitMessage" [color="Blue", label="Success"];
      "Error" -> "Parent" [color="Blue", label="Success"];
      "Failure" -> "Parent" [color="Blue", label="Success"];
      "Parent_WaitMessage" -> "final_Parent" [color="black", label="final"];
      "start_global" [shape=circle, style=filled, fillcolor="black", color="black", label="", width=0.25, height=0.25, fixedsize=true];
      "start_global" -> "Entry" [color="black", label="start"];
      "final_global" [shape=doublecircle, style=filled, fillcolor="black", color="black", label="", width=0.35, height=0.35, fixedsize=true];
      "Done" -> "final_global" [color="black", label="final"];{{(hasLegend == true
      ? System.Environment.NewLine + UmlLegend
      : string.Empty)}}
    }
    """;
}
