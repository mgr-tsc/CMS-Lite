namespace CmsLite.Database.Repositories;

public interface IBlobRepo
{
    Task<(string ETag, long Size)> UploadAsync(string key, byte[] bytes);
    Task<(byte[] Bytes, string ETag)?> DownloadAsync(string key);
    Task<(long Size, string ETag)?> HeadAsync(string key);
    Task DeleteAsync(string key);
}

// Removed IBlobRepoXml - using generic approach instead
