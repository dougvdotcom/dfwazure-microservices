using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using NLog;

namespace FileWatcherService
{
    internal class FtpFileWatcher
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string FtpPath = @"c:\inetpub\ftproot\";
        private const string SqlConn =
                "Server=tcp:dfwazure.database.windows.net,1433;Initial Catalog=sharedphoto;Persist Security Info=False;User ID=pinehead;Password=LinuxAcademy1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
            ;
        private const string StorageConn =
                "DefaultEndpointsProtocol=https;AccountName=dfwazure;AccountKey=L3Hm2DWIN8qyIlT2oqeUmwlivH+JimlNKFdLgQ+hiVYLRLhC+uYTnAFr2FENPESi6oyqZev6cEacG5FLDaEjhw==;EndpointSuffix=core.windows.net"
            ;
        private const string QueueConn =
                "Endpoint=sb://dfwazure.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=H2you+sGJDnDwO3Fk4H5IVLDJ9TJYNXVqlyzVz/T1Zw="
            ;

        public FtpFileWatcher()
        {
            var watcher = new FileSystemWatcher
            {
                Path = FtpPath,
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += OnFileChange;
            watcher.Changed += OnFileChange;
        }

        private static void OnFileChange(object sender, FileSystemEventArgs e)
        {
            Log.Info($"{e.FullPath} received.");

            if (!File.Exists(e.FullPath)) return;
            if (!FileReady(e.FullPath)) return;

            if (Path.GetExtension(e.FullPath)?.ToLowerInvariant() == ".xml")
            {
                Log.Info("This is an XML file. Creating database entry.");
                var doc = new XmlDocument();
                doc.Load(e.FullPath);

                var imgCaption = doc.SelectSingleNode("img/caption")?.InnerText;
                var imgName = doc.SelectSingleNode("img/name")?.InnerText;

                Log.Info($"This is image {imgName} with caption {imgCaption}");

                if (string.IsNullOrWhiteSpace(imgCaption) || string.IsNullOrWhiteSpace(imgName)) return;

                try
                {
                    using (var conn = new SqlConnection(SqlConn))
                    {
                        using (var cmd = new SqlCommand("sp_uploaded_create", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add(new SqlParameter("PhotoName", SqlDbType.NVarChar, 255)).Value = imgName;
                            //todo: get this value from the FTP user context
                            cmd.Parameters.Add(new SqlParameter("Username", SqlDbType.NVarChar, 50)).Value = "pinehead";
                            cmd.Parameters.Add(new SqlParameter("UploadTime", SqlDbType.DateTime)).Value =
                                DateTime.UtcNow;
                            cmd.Parameters.Add(new SqlParameter("Caption", SqlDbType.NVarChar, 1000)).Value =
                                imgCaption;

                            conn.Open();
                            cmd.ExecuteNonQuery();

                            Log.Info("DB record created.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal($"Couldn't create db record. Error: {ex.Message}");
                }
            }

            if (Path.GetExtension(e.FullPath)?.ToLowerInvariant() != ".jpg") return;

            Log.Info("JPG received. Uploading to storage.");

            var acct = CloudStorageAccount.Parse(StorageConn);
            var client = acct.CreateCloudBlobClient();
            var container = client.GetContainerReference("upload");
            var blob = container.GetBlockBlobReference(e.Name);
            blob.Properties.ContentType = "image/jpeg";
            using (var stream = File.OpenRead(e.FullPath))
            {
                blob.UploadFromStream(stream);
            }

            Log.Info("Uploaded. Putting message on queue.");
            var msg = new BrokeredMessage
            {
                Label = e.Name
            };
            var qClient = QueueClient.CreateFromConnectionString(QueueConn, "resize");
            qClient.Send(msg);

            Log.Info("Message put on queue.");

            Log.Info("Filewatcher complete.");
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
