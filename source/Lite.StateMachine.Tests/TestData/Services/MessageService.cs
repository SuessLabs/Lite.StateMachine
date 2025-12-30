// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Lite.StateMachine.Tests.TestData.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Allowed for testing.")]
public interface IMessageService
{
  int Counter1 { get; set; }

  int Counter2 { get; set; }

  List<string> Messages { get; }

  void AddMessage(string message);
}

public class MessageService : IMessageService
{
  public int Counter1 { get; set; }

  public int Counter2 { get; set; }

  public List<string> Messages { get; } = [];

  public void AddMessage(string message) =>
    Messages.Add(message);
}
