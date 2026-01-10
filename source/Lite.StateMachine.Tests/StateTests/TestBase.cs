// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Lite.StateMachine.Tests.StateTests;

public class TestBase
{
  public required TestContext TestContext { get; set; }

  protected static void AssertMachineNotNull<T>(Lite.StateMachine.StateMachine<T> machine)
    where T : struct, Enum
  {
    Assert.IsNotNull(machine);
    Assert.IsNotNull(machine.Context);
  }

  /// <summary>ILogger Helper for generating clean in-line logs.</summary>
  /// <param name="logLevel">Log level (Default: Trace).</param>
  /// <returns><see cref="ILoggingBuilder"/>.</returns>
  protected static Action<ILoggingBuilder> InlineTraceLogger(LogLevel logLevel = LogLevel.Trace)
  {
    // Creates in-line log format
    return config =>
    {
      config.AddSimpleConsole(options =>
      {
        options.TimestampFormat = "HH:mm:ss.fff ";
        options.UseUtcTimestamp = false;
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.ColorBehavior = LoggerColorBehavior.Enabled;
      });
      config.SetMinimumLevel(logLevel);
    };
  }
}
