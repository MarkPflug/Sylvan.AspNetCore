using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Sylvan.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;

namespace Benchmarks;

class Program
{
	static async Task Main(string[] args)
	{
		var b = new OutputFormatterBenchmarks();
		b.RecordCount = 1000;
		await b.ExcelXlsb();
		await b.Csv();

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
