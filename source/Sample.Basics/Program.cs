// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Sample.Basics;

internal class Program
{
  private static async Task Main(string[] args)
  {
    Console.WriteLine("Sample state machine with LiteState!");

    // await DemoMachine.RunAsync();

    // Basic state machine
    await States.DemoMachine.RunAsync();
  }
}
