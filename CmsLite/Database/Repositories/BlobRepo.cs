using System;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace CmsLite.Database.Repositories;

public class BlobRepo : IBlobRepo
{
    private readonly BlobContainerClient _container;

    public BlobRepo(BlobServiceClient svc, IConfiguration cfg)
    {
        var name = cfg["AzureStorage:Container"] ?? "cms";
        _container = svc.GetBlobContainerClient(name);
        _container.CreateIfNotExists();
    }

    public async Task<(string ETag, long Size)> UploadJsonAsync(string key, byte[] bytes)
    {
        var blob = _container.GetBlobClient(key);
        using var ms = new MemoryStream(bytes);
        if (Helpers.Utilities.IsValidJson(bytes) == false)
            throw new ArgumentException("The provided bytes are not valid JSON.", nameof(bytes));
        var resp = await blob.UploadAsync(ms, overwrite: true);
        var props = await blob.GetPropertiesAsync();
        return (props.Value.ETag.ToString(), props.Value.ContentLength);
    }

    public async Task<(byte[] Bytes, string ETag)?> DownloadAsync(string key)
    {
        var blob = _container.GetBlobClient(key);
        if (!await blob.ExistsAsync()) return null;
        using var ms = new MemoryStream();
        await blob.DownloadToAsync(ms);
        var props = await blob.GetPropertiesAsync();
        return (ms.ToArray(), props.Value.ETag.ToString());
    }

    public async Task<(long Size, string ETag)?> HeadAsync(string key)
    {
        var blob = _container.GetBlobClient(key);
        if (!await blob.ExistsAsync()) return null;
        var props = await blob.GetPropertiesAsync();
        return (props.Value.ContentLength, props.Value.ETag.ToString());
    }

    public Task DeleteAsync(string key)
    {
        var blob = _container.GetBlobClient(key);
        return blob.DeleteIfExistsAsync();
    }
}
