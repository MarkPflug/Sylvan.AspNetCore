using System;
using System.Buffers;
using System.Collections.Generic;

namespace Benchmarks;

public sealed class DebugPool<T> : ArrayPool<T>
{
	public static DebugPool<T> Instance = new DebugPool<T>(ArrayPool<T>.Shared);

	readonly ArrayPool<T> inner;

	int rentCount = 0;
	int returnCount = 0;
	int reuseCount = 0;

	public void DumpStats()
	{
		Console.WriteLine($"{rentCount} {returnCount} {reuseCount}");
	}

	public DebugPool(ArrayPool<T> inner)
	{
		this.inner = inner;
		seen = new HashSet<T[]>();
	}

	HashSet<T[]> seen;

	public override T[] Rent(int minimumLength)
	{
		rentCount++;
		var array = inner.Rent(minimumLength);
		if (seen.Add(array))
		{
			reuseCount++;
		}
		return array;
	}

	public override void Return(T[] array, bool clearArray = false)
	{
		returnCount++;
		inner.Return(array, clearArray);
	}
}
