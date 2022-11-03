using Sylvan.Data;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Sylvan.AspNetCore.Mvc.Formatters;

sealed class GenericDisposable : IDisposable
{
	readonly Action a;

	public GenericDisposable(Action a)
	{
		this.a = a;
	}
	public void Dispose()
	{
		a();
	}
}

static class FormatterUtils
{
	static ConcurrentDictionary<Type, Func<object, DbDataReader>> objectReaderFactories;

	static MethodInfo syncObjectDataReaderCreateMethod;
	static MethodInfo asyncObjectDataReaderCreateMethod;

	static ConcurrentDictionary<Type, IDataBinder> binders =
	new ConcurrentDictionary<Type, IDataBinder>();

	static ConcurrentDictionary<Type, Func<IDataBinder, DbDataReader, object>> readerFactories =
		new ConcurrentDictionary<Type, Func<IDataBinder, DbDataReader, object>>();

	static MethodInfo BinderCreateMethod =
				typeof(DataBinder)
				.GetMethod("Create", 1, BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(ReadOnlyCollection<DbColumn>), typeof(DataBinderOptions) }, null)!;


	static FormatterUtils()
	{
		syncObjectDataReaderCreateMethod = GetObjectDataReaderCreateMethod(typeof(IEnumerable<>));
		asyncObjectDataReaderCreateMethod = GetObjectDataReaderCreateMethod(typeof(IAsyncEnumerable<>));
		objectReaderFactories = new ConcurrentDictionary<Type, Func<object, DbDataReader>>();
	}

	internal static IDataBinder GetObjectBinder(Type modelType, ReadOnlyCollection<DbColumn> schema, DataBinderOptions opts)
	{
		IDataBinder? binder;
		if (!binders.TryGetValue(modelType, out binder))
		{
			var method = BinderCreateMethod.MakeGenericMethod(modelType);
			binder = (IDataBinder)method.Invoke(null, new object[] { schema, opts })!;
			binders.TryAdd(modelType, binder);
		}
		return binder;
	}



	internal static Func<IDataBinder, DbDataReader, object> GetReaderFactory(Type modelType)
	{
		Func<IDataBinder, DbDataReader, object>? readerFactory;
		if (!readerFactories.TryGetValue(modelType, out readerFactory))
		{
			var targetType = modelType.GetGenericArguments()[0];

			var ctor = typeof(DataReader<>).MakeGenericType(targetType).GetConstructors()[0];

			var a = Expression.Parameter(typeof(DbDataReader));
			var b = Expression.Parameter(typeof(IDataBinder));

			var lambda =
				Expression.Lambda<Func<IDataBinder, DbDataReader, object>>(
					Expression.New(ctor, a, b),
					b,
					a
				);
			readerFactory = lambda.Compile();
			readerFactories.TryAdd(modelType, readerFactory);

		}
		return readerFactory;
	}


	static MethodInfo GetObjectDataReaderCreateMethod(Type seqType)
	{
		return
			typeof(ObjectDataReader)
			.GetMethods()
			.Single(m =>
			{
				if (m.Name != "Create")
					return false;
				var prms = m.GetParameters();
				var pt = prms[0].ParameterType;
				return pt.IsGenericType && pt.GetGenericTypeDefinition() == seqType;
			});
	}

	internal static bool CanWriteType(Type type)
	{
		if (type.IsPrimitive || type == typeof(string))
		{
			return false;
		}
		// can support I/DbDataReader and IEnumerable<T> where T is a complex type
		if (type == typeof(DbDataReader) || type == typeof(IDataReader))
		{
			return true;
		}

		return IsComplexIEnumerableT(type);
	}

	internal static DbDataReader GetReader(object? data)
	{
		if (data is DbDataReader r)
		{
			return r;
		}
		else if (data is IDataReader rr)
		{
			return rr.AsDbDataReader();
		}
		else if (data != null)
		{
			var type = data.GetType();
			if (objectReaderFactories.TryGetValue(type, out var factory))
			{
				return factory(data);
			}
			else
			{
				if (IsComplexIEnumerableT(type))
				{
					factory = GetObjectReaderFactory(type);
					return factory(data);
				}
			}
		}
		throw new NotSupportedException();
	}

	static Func<object, DbDataReader> GetObjectReaderFactory(Type type)
	{
		return objectReaderFactories.GetOrAdd(type, FormatterUtils.BuildObjectReaderFactory);
	}

	internal static bool IsComplexIEnumerableT(Type t)
	{
		if (t.IsGenericType)
		{
			var gtd = t.GetGenericTypeDefinition();
			if (gtd == typeof(IEnumerable<>) || gtd == typeof(IAsyncEnumerable<>))
			{
				var elementArg = t.GetGenericArguments()[0];
				if (Type.GetTypeCode(elementArg) == TypeCode.Object && elementArg != typeof(Guid))
				{
					return true;
				}
			}
		}
		return t.GetInterfaces().Any(IsComplexIEnumerableT);
	}

	internal static Func<object, DbDataReader> BuildObjectReaderFactory(Type type)
	{
		var param = Expression.Parameter(typeof(object));

		foreach (var iface in type.GetInterfaces())
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
			{
				var seqType = typeof(IAsyncEnumerable<>);
				var elementType = iface.GetGenericArguments()[0];

				var createMethod = asyncObjectDataReaderCreateMethod.MakeGenericMethod(new Type[] { elementType });
				var lambda = Expression.Lambda<Func<object, DbDataReader>>(
					Expression.Call(
						createMethod,
						Expression.Convert(
							param,
							seqType.MakeGenericType(elementType)
						),
						Expression.Default(typeof(CancellationToken))
					),
					param
				);

				return lambda.Compile();
			}
		}
		foreach (var iface in type.GetInterfaces())
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				var seqType = typeof(IEnumerable<>);
				var elementType = iface.GetGenericArguments()[0];
				var createMethod = syncObjectDataReaderCreateMethod.MakeGenericMethod(new Type[] { elementType });
				var lambda = Expression.Lambda<Func<object, DbDataReader>>(
					Expression.Call(
						createMethod,
						Expression.Convert(
							param,
							seqType.MakeGenericType(elementType)
						)
					),
					param
				);

				return lambda.Compile();
			}
		}

		// TODO: might be nice to explain why it isn't supported.
		throw new NotSupportedException();
	}
}


class DataReader<T> :
		IEnumerable<T>,
		IAsyncEnumerable<T>,
		IAsyncDisposable
		where T : new()
{

	DbDataReader data;
	IDataBinder<T> binder;

	public DataReader(DbDataReader data, object binder)
	{
		this.data = data;
		this.binder = (IDataBinder<T>)binder;
	}

	public ValueTask DisposeAsync()
	{
		return data.DisposeAsync();
	}

	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		while (await data.ReadAsync())
		{
			var record = new T();
			binder.Bind(data, record);
			yield return record;
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		while (data.Read())
		{
			var record = new T();
			binder.Bind(data, record);
			yield return record;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
