// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sample.Basics;

internal class Program
{
  private static async Task Main(string[] args)
  {
    Console.WriteLine("Sample state machine with LiteState!");
    await States.DemoMachine.RunAsync();

    // Poor man's timestamp
    Console.WriteLine("\nRunning again, showing simple benchmarks...");
    for (int i = 1; i <= 10; i++)
    {
      var sw = Stopwatch.StartNew();

      await States.DemoMachine.RunAsync(logOutput: false);

      sw.Stop();
      Console.WriteLine($"Took {sw.ElapsedMilliseconds} ms ({sw.ElapsedTicks} ticks)");
    }
  }
}
