// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using ComparerBenchmark;

var summary = BenchmarkRunner.Run<ComparerBenchmarks>();

Console.ReadKey();