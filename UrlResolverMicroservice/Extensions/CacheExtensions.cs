using Microsoft.Extensions.Caching.Distributed;

namespace UrlResolverMicroservice.Extensions;

public static class CacheExtensions
{
	public static async Task<bool> GetBooleanAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadBoolean();
	}

	public static async Task<char> GetCharAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadChar();
	}

	public static async Task<decimal> GetDecimalAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadDecimal();
	}

	public static async Task<double> GetDoubleAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadDouble();
	}

	public static async Task<short> GetShortAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadInt16();
	}

	public static async Task<int> GetIntAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadInt32();
	}

	public static async Task<long> GetLongAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadInt64();
	}

	public static async Task<float> GetFloatAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadSingle();
	}

	public static async Task<string> GetStringAsync(this IDistributedCache cache, string key)
	{
		var bytes = await cache.GetAsync(key);
		if (bytes == null)
			return default;
		await using var memoryStream = new MemoryStream(bytes);
		var binaryReader = new BinaryReader(memoryStream);
		return binaryReader.ReadString();
	}

	public static Task SetAsync(this IDistributedCache cache, string key, bool value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, char value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, decimal value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, double value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, short value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, int value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, long value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, float value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

	public static Task SetAsync(this IDistributedCache cache, string key, string value,
		DistributedCacheEntryOptions options)
	{
		byte[] bytes;
		using (var memoryStream = new MemoryStream())
		{
			var binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(value);
			bytes = memoryStream.ToArray();
		}

		return cache.SetAsync(key, bytes, options);
	}

#if DNX451
    public static async Task<T> GetAsync<T>(this IDistributedCache cache, string key)
    {
        byte[] bytes = await cache.GetAsync(key);
        using (MemoryStream memoryStream = new MemoryStream(bytes))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            return (T)binaryFormatter.Deserialize(memoryStream);
        }
    }

    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value)
    {
        return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
    }

    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options)
    {
        byte[] bytes;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, value);
            bytes = memoryStream.ToArray();
        }

        return cache.SetAsync(key, bytes, options);
    }
#endif
}