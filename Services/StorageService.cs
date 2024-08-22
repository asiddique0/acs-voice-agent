namespace LumenicBackend.Services
{
    public class StorageService : IStorageService
    {
        private readonly BlobContainerClient client;

        public StorageService(BlobContainerClient client)
        {
            this.client = client;
        }

        public async Task<Response<BlobContentInfo>> StoreFile(string fileName, Stream data)
        {
            return await this.client.UploadBlobAsync(fileName, data);
        }

        public async Task<Response<bool>> DeleteFile(string fileName)
        {
            return await this.client.DeleteBlobIfExistsAsync(fileName);
        }
    }
}
