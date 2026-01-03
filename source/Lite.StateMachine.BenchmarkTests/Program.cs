// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Lite.StateMachine.BenchmarkTests;
using Perfolizer.Horology;

namespace Lite.StateMachine.BenchmarkTests;

public class Program
{
  public static void Main(string[] args)
  {
    // Manual Execution:
    //  A. Visual Studio > Project (R-Click) > Debug > Start Without Debugging
    //  B. dotnet run --configuration release
    //
    // For more information on the VS BenchmarkDotNet Diagnosers see:
    //  https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
    Console.WriteLine("Hello, Lite.StateMachine Benchmark Tests!");

    // Run single benchmark class
    BenchmarkRunner.Run<BasicStateBenchmarks>();

    // Auto-Discover:
    ////BenchmarkRunner.Run(typeof(Program).Assembly);

    // Manual Selection:
    ////BenchmarkSwitcher.FromAssembly(System.Reflection.Assembly.GetExecutingAssembly()).Run(args);

    // Quick runner:
    ////BenchmarkRunner.Run<BasicStateBenchmarks>(
    ////  DefaultConfig.Instance.AddJob(
    ////    Job.Default.WithToolchain(new InProcessEmitToolchain(timeout: TimeSpan.FromSeconds(5), logOutput: false))
    ////               .WithLaunchCount(1)
    ////               .WithWarmupCount(5)
    ////               .WithIterationCount(20)
    ////               .WithIterationTime(TimeInterval.FromMilliseconds(80)))
    ////  .AddLogger(new ConsoleLogger(unicodeSupport: true, ConsoleLogger.CreateGrayScheme())).WithOptions(ConfigOptions.DisableLogFile));

    // Alternate manual config to "Quick Runner" above:
    ////BenchmarkRunner.Run<BasicStateBenchmarks>(BenchmarkConfig.Get());
  }
}
