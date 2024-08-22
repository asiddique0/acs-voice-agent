namespace LumenicBackend.Interfaces
{
    public interface IStorageService
    {
        Task<Response<BlobContentInfo>> StoreFile(string fileName, Stream data);
        Task<Response<bool>> DeleteFile(string fileName);
    }
}
