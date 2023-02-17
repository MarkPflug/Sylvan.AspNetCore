using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Sylvan.AspNetCore.Mvc.Formatters;

namespace Benchmarks;

class Program
{
	static void Main(string[] args)
	{
		//new InputFormatterBenchmarks().Excel().Wait();

		BenchmarkSwitcher
		 .FromAssembly(typeof(Program).Assembly)
		 .Run(args, new MyConfig());

		//DebugPool<byte>.Instance.DumpStats();
	}
}

class MyConfig : ManualConfig
{
	public MyConfig() : base()
	{
		AddJob(Job.InProcess.WithMinIterationCount(1).WithWarmupCount(2).WithMaxIterationCount(6));
		AddLogger(ConsoleLogger.Default);
		AddColumnProvider(DefaultColumnProviders.Instance);
	}
}
