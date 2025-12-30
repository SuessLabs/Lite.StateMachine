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
      legend_composite [shape=doublecircle, label="Composite parent"];
      legend_leaf [shape=ellipse, label="Leaf state"];
      legend_composite -> legend_leaf [color="Green", label="parent → initial child"];
      legend_leaf -> legend_leaf_ok   [color="Blue",   label="Ok"];
      legend_leaf -> legend_leaf_err  [color="Yellow", label="Error"];
      legend_leaf -> legend_leaf_fail [color="Red", label="Failure"];
      legend_leaf_ok   [shape=point, label=""];
      legend_leaf_err  [shape=point, label=""];
      legend_leaf_fail [shape=point, label=""];
      legend_note [shape=note, label="Shapes:\n  • doublecircle = Composite parent\n  • ellipse = Leaf state\n\nEdge colors:\n  • Ok = Blue\n  • Error = Yellow\n  • Failure = Red\n  • Parent→Child = Green"];
    }
  """;

  public static string BasicStates123(bool hasLegend = false) => $$"""
    digraph "BasicStateMachine" {
      compound=true;
      rankdir=LR;
      fontsize=12;
      "State1" [shape=ellipse, label="State1"];
      "State2" [shape=ellipse, label="State2"];
      "State3" [shape=ellipse, label="State3"];
      "State1" -> "State2" [color="Blue", label="Ok"];
      "State2" -> "State3" [color="Blue", label="Ok"];{{(hasLegend == true
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
    digraph "StateMachine" {
      compound=true;
      rankdir=LR;
      fontsize=12;
      "State1" [shape=ellipse, label="State1"];
      "State2" [shape=ellipse, label="State2"];
      "State3" [shape=ellipse, label="State3"];
      "State1" -> "State2" [color="Blue", label="Ok"];
      "State2" -> "State3" [color="Blue", label="Ok"];
      "State2_Sub1" -> "State2_Sub2" [color="Blue", label="Ok"];{{(hasLegend == true
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
        "Parent" [shape=doublecircle, label="Parent"];
        "Parent_Fetch" [shape=ellipse, label="Parent_Fetch"];
        "Parent_WaitMessage" [shape=ellipse, label="Parent_WaitMessage"];
      }
      "Entry" [shape=ellipse, label="Entry"];
      "Done" [shape=ellipse, label="Done"];
      "Error" [shape=ellipse, label="Error"];
      "Failure" [shape=ellipse, label="Failure"];
      "Parent" -> "Parent_Fetch" [color="Green", label="initial"];
      "Entry" -> "Parent" [color="Blue", label="Ok"];
      "Parent" -> "Done" [color="Blue", label="Ok"];
      "Parent" -> "Error" [color="Yellow", label="Error"];
      "Parent" -> "Failure" [color="Red", label="Failure"];
      "Parent_Fetch" -> "Parent_WaitMessage" [color="Blue", label="Ok"];
      "Error" -> "Parent" [color="Blue", label="Ok"];
      "Failure" -> "Parent" [color="Blue", label="Ok"];{{(hasLegend == true
      ? System.Environment.NewLine + UmlLegend
      : string.Empty)}}
    }
    """;
}
