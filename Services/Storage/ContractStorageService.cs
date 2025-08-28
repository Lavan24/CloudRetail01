using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Retail3.Services.Interface;

namespace Retail3.Services.Storage
{
    public class ContractStorageService : IContractStorageService
    {
        private readonly ShareClient _shareClient;

        public ContractStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage")
                ?? "UseDevelopmentStorage=true";
            var shareName = "contracts"; // Make sure this matches your Azure File Share name
            _shareClient = new ShareClient(connectionString, shareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadContractAsync(IFormFile contractFile)
        {
            if (contractFile == null || contractFile.Length == 0)
                throw new ArgumentException("Invalid file.");

            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(contractFile.FileName);

            using (var stream = contractFile.OpenReadStream())
            {
                await fileClient.CreateAsync(contractFile.Length);
                await fileClient.UploadRangeAsync(
                    new Azure.HttpRange(0, contractFile.Length),
                    stream
                );
            }
        }

        public async Task<Stream> DownloadContractAsync(string fileName)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            var exists = await fileClient.ExistsAsync();
            if (!exists)
                throw new FileNotFoundException("Contract not found.");

            var download = await fileClient.DownloadAsync();
            var memoryStream = new MemoryStream();
            await download.Value.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<List<string>> ListContractsAsync()
        {
            var files = new List<string>();
            var rootDir = _shareClient.GetRootDirectoryClient();

            await foreach (ShareFileItem item in rootDir.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                    files.Add(item.Name);
            }
            return files;
        }
    }
}
