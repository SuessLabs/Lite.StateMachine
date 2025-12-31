// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests;

public static class AssertExtensions
{
  ////public static void AreEqualIgnoreLines(this Assert assert, string expected, string actual)
  ////{
  ////  assert.AreEqual(
  ////    expected.TrimEnd('\r', '\n'),
  ////    actual.TrimEnd('\r', '\n'));
  ////}

  public static void AreEqualIgnoreLines(string expected, string actual)
  {
    Assert.AreEqual(
      expected.TrimEnd('\r', '\n'),
      actual.TrimEnd('\r', '\n'),
      message: $"Incorrect string comparison.\n\nExpected:\n{expected}\n\nActual:\n{actual}");
  }
}
