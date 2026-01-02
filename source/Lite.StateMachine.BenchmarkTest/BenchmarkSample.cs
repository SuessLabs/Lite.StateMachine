// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
/*
using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace Lite.StateMachine.BenchmarkTest;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[CPUUsageDiagnoser]
public class BenchmarkSample
{
  private byte[] _data = [];
  private SHA256 _sha256 = SHA256.Create();

  [GlobalSetup]
  public void Setup()
  {
    _data = new byte[10000];
    new Random(42).NextBytes(_data);
  }

  [Benchmark]
  public byte[] Sha256()
  {
    return _sha256.ComputeHash(_data);
  }
}
*/
