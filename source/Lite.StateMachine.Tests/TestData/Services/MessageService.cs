// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Lite.StateMachine.Tests.TestData.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Allowed for testing.")]
public interface IMessageService
{
  List<string> Messages { get; }

  int Number { get; set; }

  void AddMessage(string message);
}

public class MessageService : IMessageService
{
  private int _number;

  public List<string> Messages { get; } = [];

  public int Number { get => _number; set => _number = value; }

  public void AddMessage(string message) =>
    Messages.Add(message);
}
