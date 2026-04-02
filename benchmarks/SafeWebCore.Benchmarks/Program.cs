using BenchmarkDotNet.Running;
using SafeWebCore.Benchmarks;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args);
