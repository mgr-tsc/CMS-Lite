using System.Collections.Concurrent;
using CmsLite.Database.Repositories;
using CmsLite.Helpers;

namespace CmsLiteTests.Fakes;

public class InMemoryBlobRepo : IBlobRepo
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ETag)> _store = new();

    public Task<(string ETag, long Size)> UploadAsync(string key, byte[] bytes)
    {
        var etag = $"etag-{Guid.NewGuid():N}";
        _store[key] = (bytes, etag);
        return Task.FromResult((etag, bytes.LongLength));
    }

    public Task<(byte[] Bytes, string ETag)?> DownloadAsync(string key)
    {
        if (_store.TryGetValue(key, out var value))
        {
            return Task.FromResult<(byte[] Bytes, string ETag)?>(value);
        }

        return Task.FromResult<(byte[] Bytes, string ETag)?>(null);
    }

    public Task<(long Size, string ETag)?> HeadAsync(string key)
    {
        if (_store.TryGetValue(key, out var value))
        {
            return Task.FromResult<(long Size, string ETag)?>(new(value.Bytes.LongLength, value.ETag));
        }

        return Task.FromResult<(long Size, string ETag)?>(null);
    }

    public Task DeleteAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
