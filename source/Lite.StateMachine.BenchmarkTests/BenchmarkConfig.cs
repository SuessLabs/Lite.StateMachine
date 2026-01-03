// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Lite.StateMachine.BenchmarkTests;

public class BenchmarkConfig
{
  /// <summary>Custom configuration.</summary>
  /// <returns>BenchmarkDotNet Configuration.</returns>
  public static IConfig Get()
  {
    return ManualConfig.CreateEmpty()
      //// Jobs
      .AddJob(Job.Default
        .WithRuntime(CoreRuntime.Core90)
        .WithPlatform(Platform.X64))
      //// Diagnoser and Output Configuration
      .AddDiagnoser(MemoryDiagnoser.Default)
      .AddColumnProvider(DefaultColumnProviders.Instance)
      .AddLogger(ConsoleLogger.Default)
      .AddExporter(CsvExporter.Default)
      .AddExporter(HtmlExporter.Default)
      .AddAnalyser([.. GetAnalysers()]);  //// "[.. GetAnalysers()]" == 'GetAnalysers().ToArray()"
  }

  /// <summary>Get analyzer for the custom configuration.</summary>
  /// <returns>Collection of default analyzers.</returns>
  private static IEnumerable<IAnalyser> GetAnalysers()
  {
    yield return EnvironmentAnalyser.Default;
    yield return OutliersAnalyser.Default;
    yield return MinIterationTimeAnalyser.Default;
    yield return MultimodalDistributionAnalyzer.Default;
    yield return RuntimeErrorAnalyser.Default;
    yield return ZeroMeasurementAnalyser.Default;
    yield return BaselineCustomAnalyzer.Default;
  }
}
