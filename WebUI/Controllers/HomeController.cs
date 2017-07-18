using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Web.Mvc;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var nsm = NamespaceManager.CreateFromConnectionString(
                ConfigurationManager.AppSettings["ServiceBusConnection"]);
            if (!nsm.QueueExists("resize"))
            {
                nsm.CreateQueue("resize");
            }
            if (!nsm.TopicExists("process"))
            {
                nsm.CreateTopic("process");
            }
            if (nsm.SubscriptionExists("process", "save")) nsm.DeleteSubscription("process", "save");
            var saveFilter = new SqlFilter("Action = 0");
            nsm.CreateSubscription("process", "save", saveFilter);
            if (nsm.SubscriptionExists("process", "delete")) nsm.DeleteSubscription("process", "delete");
            var deleteFilter = new SqlFilter("Action = 1");
            nsm.CreateSubscription("process", "delete", deleteFilter);

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Upload()
        {
            var model = new UploadFileModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult Upload(UploadFileModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var extension = Path.GetExtension(model.File.FileName);
            if (extension != null && extension.ToLowerInvariant() != ".jpg")
            {
                ModelState.AddModelError("InvalidExtension", "Please only upload JPG files.");
                return View(model);
            }

            if (model.File.ContentLength <= 0) return View(model);

            var fileName = $"{Guid.NewGuid()}.jpg";
            var acct = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnection"]);
            var client = acct.CreateCloudBlobClient();
            var container = client.GetContainerReference("upload");
            var blob = container.GetBlockBlobReference(fileName);
            blob.Properties.ContentType = "image/jpeg";

            using (var stream = model.File.InputStream)
            {
                stream.Position = 0;
                blob.UploadFromStream(stream);
            }

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"]
                .ConnectionString))
            {
                using (var cmd = new SqlCommand("sp_uploaded_create", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("PhotoName", SqlDbType.NVarChar, 255)).Value = fileName;
                    cmd.Parameters.Add(new SqlParameter("Username", SqlDbType.NVarChar, 50)).Value = "Doug";
                    cmd.Parameters.Add(new SqlParameter("UploadTime", SqlDbType.DateTime)).Value =
                        DateTime.UtcNow;
                    cmd.Parameters.Add(new SqlParameter("Caption", SqlDbType.NVarChar, 1000)).Value =
                        WebUtility.HtmlEncode(model.Caption);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            var msg = new BrokeredMessage
            {
                Label = fileName
            };

            var qClient =
                QueueClient.CreateFromConnectionString(ConfigurationManager.AppSettings["ServiceBusConnection"],
                    "resize");
            qClient.Send(msg);

            return RedirectToAction("List");
        }

        public ActionResult List(string viewType)
        {
            var status = viewType?.ToLowerInvariant() == "approved";
            var imgloc = viewType?.ToLowerInvariant() == "approved" ? "saved" : "upload";
            var thumbloc = viewType?.ToLowerInvariant() == "approved" ? "savedthumb" : "uploadthumb";

            ViewBag.Status = status ? "Approved" : "Unapproved";

            var model = new List<UploadedPhotoViewModel>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"]
                .ConnectionString))
            {
                using (var cmd = new SqlCommand("sp_uploaded_get_bystatus", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("Status", SqlDbType.Bit)).Value = status;

                    conn.Open();
                    var reader = cmd.ExecuteReader();
                    if (!reader.HasRows) return View(model);
                    while (reader.Read())
                    {
                        var imgName = reader.GetString(reader.GetOrdinal("PhotoName"));
                        var tmp = new UploadedPhotoViewModel
                        {
                            PhotoName = imgName,
                            Caption = reader.GetString(reader.GetOrdinal("Caption")),
                            FullPhotoUrl = $"{ConfigurationManager.AppSettings["BaseStorageUrl"]}{imgloc}/{imgName}",
                            ThumbnailUrl = $"{ConfigurationManager.AppSettings["BaseStorageUrl"]}{thumbloc}/{imgName}",
                            UploadTime = reader.GetDateTime(reader.GetOrdinal("UploadTime")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            IsApproved = status
                        };

                        model.Add(tmp);
                    }
                }
            }

            return View(model);
        }

        public ActionResult Process(string id, int todo, string view)
        {
            var msg = new BrokeredMessage
            {
                Label = id
            };

            msg.Properties.Add("Action", todo);

            var topicClient =
                TopicClient.CreateFromConnectionString(ConfigurationManager.AppSettings["ServiceBusConnection"],
                    "process");
            topicClient.Send(msg);

            return RedirectToAction("List");
        }
    }
}