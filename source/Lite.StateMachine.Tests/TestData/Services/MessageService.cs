// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Allowed for testing.")]
public interface IMessageService
{
  int GetNumber();

  void SetNumber(int number);
}

public class MessageService : IMessageService
{
  private int _cache;

  public int GetNumber() => _cache;

  public void SetNumber(int number) => _cache = number;
}
