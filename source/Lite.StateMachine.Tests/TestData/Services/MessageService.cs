// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Lite.StateMachine.Tests.TestData.States;

namespace Lite.StateMachine.Tests.TestData.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Allowed for testing.")]
public interface IMessageService
{
  /// <summary
  /// Gets or sets a counter.
  /// <see cref="DiStateBase{TStateClass, TStateId}"/> uses it as an automatic state transition counter.
  /// </summary>
  int Counter1 { get; set; }

  /// <summary>Gets or sets the user's custom counter.</summary>
  int Counter2 { get; set; }

  /// <summary>Gets or sets the user's custom counter.</summary>
  int Counter3 { get; set; }

  /// <summary>Gets or sets the user's custom counter.</summary>
  int Counter4 { get; set; }

  /// <summary>Gets a list of user's custom messages.</summary>
  List<string> Messages { get; }

  /// <summary>Add message to read-only list.</summary>
  /// <param name="message">Message.</param>
  void AddMessage(string message);
}

public class MessageService : IMessageService
{
  /// <inheritdoc/>
  public int Counter1 { get; set; }

  /// <inheritdoc/>
  public int Counter2 { get; set; }

  /// <inheritdoc/>
  public int Counter3 { get; set; }

  /// <inheritdoc/>
  public int Counter4 { get; set; }

  /// <inheritdoc/>
  public List<string> Messages { get; } = [];

  /// <inheritdoc/>
  public void AddMessage(string message) =>
    Messages.Add(message);
}
