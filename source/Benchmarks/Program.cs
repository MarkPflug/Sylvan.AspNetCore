using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Benchmarks
{
    class Program
    {
        static void Main()
        {
            var cp = Encoding.UTF8;
            ;
            //var b = new InputFormatterBenchmarks();
            //b.Csv().Wait();
            //;
            //b.Json().Wait();

            //var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            //{
            //    "System.Private.CoreLib",
            //};

            //foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    if (skip.Contains(asm.GetName().Name))
            //        continue;
            //    Console.WriteLine(".................................");
            //    Console.WriteLine(asm.FullName);
            //    foreach (var m in asm.GetLoadedModules())
            //    {
            //        foreach (var t in m.GetTypes())
            //        {
            //            Console.WriteLine(t.FullName);
            //        }
            //    }
            //}

            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run();
        }
    }
}
