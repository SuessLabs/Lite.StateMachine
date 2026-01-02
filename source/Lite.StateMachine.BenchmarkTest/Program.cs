// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;

namespace Lite.StateMachine.BenchmarkTest;

internal class Program
{
  private static void Main(string[] args)
  {
    // Run via terminal:
    // dotnet run --configuration release
    _ = BenchmarkRunner.Run(typeof(Program).Assembly);

    ////BenchmarkSwitcher
    ////  .FromAssembly(Assembly.GetExecutingAssembly())
    ////  .Run(args);

    // Quicker running
    ////_ = BenchmarkRunner.Run<Benchmarks>(
    ////  DefaultConfig.Instance.AddJob(
    ////    Job.Default.WithToolchain(new InProcessEmitToolchain(timeout: TimeSpan.FromSeconds(5), logOutput: false))
    ////               .WithLaunchCount(1)
    ////               .WithWarmupCount(5)
    ////               .WithIterationCount(20)
    ////               .WithIterationTime(TimeInterval.FromMilliseconds(80)))
    ////  .AddLogger(new ConsoleLogger(unicodeSupport: true, ConsoleLogger.CreateGrayScheme())).WithOptions(ConfigOptions.DisableLogFile));
  }
}
