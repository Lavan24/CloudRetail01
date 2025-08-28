using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Retail3.Services;
using Retail3.Services.Interface;
using Retail3.Services.Storage;

namespace Retail3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register Azure Storage clients
            var connectionString = builder.Configuration.GetConnectionString("AzureStorage")
                ?? "UseDevelopmentStorage=true";

            builder.Services.AddSingleton(new TableServiceClient(connectionString));
            builder.Services.AddSingleton(new BlobServiceClient(connectionString));
            builder.Services.AddSingleton(new QueueServiceClient(connectionString));
            builder.Services.AddSingleton(new ShareServiceClient(connectionString));

            //// Register main library service
            builder.Services.AddScoped<IRetail3Service, Retail3Service>();
            builder.Services.AddScoped<ITableStorageService, TableStorageService>();
            builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
            builder.Services.AddScoped<IQueueService, QueueService>();
            builder.Services.AddScoped<IContractStorageService, ContractStorageService>();
            builder.Services.AddSingleton<IFileStorageService, FileStorageService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
