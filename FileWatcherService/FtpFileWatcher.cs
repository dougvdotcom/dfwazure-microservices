using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;

namespace FileWatcherService
{
    internal class FtpFileWatcher
    {
        public FtpFileWatcher()
        {
            var watcher = new FileSystemWatcher
            {
                Path = @"c:\inetpub\ftproot\",
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += OnFileChange;
            watcher.Changed += OnFileChange;
        }

        private static void OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) return;
            if (!FileReady(e.FullPath)) return;

            if (Path.GetExtension(e.FullPath)?.ToLowerInvariant() == ".xml")
            {
                var doc = new XmlDocument();
                doc.Load(e.FullPath);

                var userName = doc.SelectSingleNode("img/username")?.InnerText;
                var imgCaption = doc.SelectSingleNode("img/caption")?.InnerText;
                var imgName = doc.SelectSingleNode("img/name")?.InnerText;

                using (var conn = new SqlConnection("Server=tcp:dfwazure.database.windows.net,1433;Initial Catalog=sharedphoto;Persist Security Info=False;User ID=pinehead;Password=LinuxAcademy1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
                {
                    using (var cmd = new SqlCommand("sp_upload_create", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("PhotoName", SqlDbType.NVarChar, 255)).Value = imgName;
                        cmd.Parameters.Add(new SqlParameter("Username", SqlDbType.NVarChar, 50)).Value = userName;
                        cmd.Parameters.Add(new SqlParameter("UploadTime", SqlDbType.DateTime)).Value =
                            DateTime.UtcNow;
                        cmd.Parameters.Add(new SqlParameter("Caption", SqlDbType.NVarChar, 1000)).Value =
                            imgCaption;

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (Path.GetExtension(e.FullPath)?.ToLowerInvariant() != ".jpg") return;

            var acct = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=dfwazure;AccountKey=L3Hm2DWIN8qyIlT2oqeUmwlivH+JimlNKFdLgQ+hiVYLRLhC+uYTnAFr2FENPESi6oyqZev6cEacG5FLDaEjhw==;EndpointSuffix=core.windows.net");
            var client = acct.CreateCloudBlobClient();
            var container = client.GetContainerReference("upload");
            var blob = container.GetBlockBlobReference(e.Name);
            blob.Properties.ContentType = "image/jpeg";
            using (var stream = File.OpenRead(e.FullPath))
            {
                blob.UploadFromStream(stream);
            }

            var msg = new BrokeredMessage
            {
                Label = e.Name
            };
            var qClient = QueueClient.CreateFromConnectionString("Endpoint=sb://dfwazure.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=H2you+sGJDnDwO3Fk4H5IVLDJ9TJYNXVqlyzVz/T1Zw=", "resize");
            qClient.Send(msg);
        }

        private static bool FileReady(string filepath)
        {
            const int maxtries = 10;
            var count = 0;
            var result = false;

            while (count < maxtries)
            {
                FileStream stream = null;
                try
                {
                    stream = File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    result = true;
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
                finally
                {
                    stream?.Close();
                }
                ++count;
            }

            return result;
        }
    }
}
