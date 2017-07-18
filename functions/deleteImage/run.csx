#r "Microsoft.ServiceBus"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Data"

using System;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Data;
using System.Data.SqlClient;

public static void Run(BrokeredMessage mySbMsg, TraceWriter log)
{
    log.Info($"C# ServiceBus topic trigger function processed message: {mySbMsg.Label}");

    var acct = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
    var client = acct.CreateCloudBlobClient();
    var sourceContainer = client.GetContainerReference("upload");
    var sourceThumbContainer = client.GetContainerReference("uploadthumb");
    sourceContainer.CreateIfNotExists();
    sourceContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
    sourceThumbContainer.CreateIfNotExists();
    sourceThumbContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

    log.Info("Deleting image data from database.");
    using(var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
    {
        using(var cmd = new SqlCommand("sp_uploaded_delete", conn)) 
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("PhotoName", SqlDbType.NVarChar, 255)).Value = mySbMsg.Label;
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }

    log.Info("Deleting image from upload container.");
    var sourceBlob = sourceContainer.GetBlockBlobReference(mySbMsg.Label);
    sourceBlob.Delete();
    
    log.Info("Deleting thumb from uploadthumb container.");
    var sourceThumb = sourceThumbContainer.GetBlockBlobReference(mySbMsg.Label);
    sourceThumb.Delete();
    
    log.Info("Function complete.");
}
