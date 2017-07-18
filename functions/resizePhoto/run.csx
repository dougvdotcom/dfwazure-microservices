#r "Microsoft.ServiceBus"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Drawing"

using System;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer;

public static void Run(BrokeredMessage myQueueItem, TraceWriter log)
{
    log.Info($"C# ServiceBus queue trigger function received message: {myQueueItem.Label}");

    log.Info("Logging in to Azure Storage, setting upload and thumbnail containers");
    var acct = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
    var client = acct.CreateCloudBlobClient();
    var container = client.GetContainerReference("upload");
    var thumbContainer = client.GetContainerReference("uploadthumb");
    container.CreateIfNotExists();
    container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
    thumbContainer.CreateIfNotExists();
    thumbContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

    log.Info("Retrieving uploaded image from blob storage and saving to memory stream.");
    var blob = container.GetBlockBlobReference(myQueueItem.Label);
    var inputStream = new MemoryStream();
    blob.DownloadToStream(inputStream);
    inputStream.Position = 0;

    log.Info("Creating thumbnail blob memory stream.");
    var outputStream = new MemoryStream();

    log.Info("Saving uploaded image to thumbnail.");
    var settings = new ImageResizer.ResizeSettings {
        MaxWidth = 250, 
        Format = "jpg"
    };
    ImageResizer.ImageBuilder.Current.Build(inputStream, outputStream, settings);
    outputStream.Position = 0;

    log.Info("Uploading thumbnail to Azure Storage.");
    var uploadBlob = thumbContainer.GetBlockBlobReference(myQueueItem.Label);
    uploadBlob.Properties.ContentType = "image/jpg";
    uploadBlob.UploadFromStream(outputStream);

    log.Info("Disposiong of memory streams.");
    inputStream.Dispose();
    outputStream.Dispose();

    log.Info("Function complete.");
}
