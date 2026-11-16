// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using Lite.StateMachine.Tests.TestData;
using Sample11.GenerateDotGraph;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public sealed class DiagramSampleTests
{
  [TestMethod]
  [DataRow(false)]
  [DataRow(true)]
  public void Sample11MatchesCompositeExportTest(bool includeLegend)
  {
    var actual = WorkflowDiagram.ExportDot(includeLegend);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.CompositeWithErrorFailure(includeLegend), actual);
  }
}
