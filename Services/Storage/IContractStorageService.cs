public interface IContractStorageService
{
    Task UploadContractAsync(IFormFile contractFile);
    Task<Stream> DownloadContractAsync(string fileName);
    Task<List<string>> ListContractsAsync();
}
