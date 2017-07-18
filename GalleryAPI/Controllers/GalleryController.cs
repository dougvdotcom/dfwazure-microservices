using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using GalleryAPI.Models;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;

namespace GalleryAPI.Controllers
{
    public class GalleryController : ApiController
    {
        // GET api/values
        [SwaggerOperation("GetAll")]
        public HttpResponseMessage Get()
        {
            var output = new List<UploadedImage>();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"]
                .ConnectionString))
            {
                using (var cmd = new SqlCommand("sp_uploaded_get_bystatus", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("Status", SqlDbType.Bit)).Value = 1;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var imgName = reader.GetString(reader.GetOrdinal("PhotoName"));
                                var tmp = new UploadedImage
                                {
                                    PhotoName = imgName,
                                    Username = reader.GetString(reader.GetOrdinal("Username")),
                                    UploadTime = reader.GetDateTime(reader.GetOrdinal("UploadTime")),
                                    Caption = reader.GetString(reader.GetOrdinal("Caption")),
                                    ImageUrl = $"{ConfigurationManager.AppSettings["CdnRootUrl"]}saved/{imgName}",
                                    ThumbUrl = $"{ConfigurationManager.AppSettings["CdnRootUrl"]}savedthumb/{imgName}"
                                };

                                output.Add(tmp);
                            }
                        }
                    }
                }
            }

            return new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json")
            };
        }

    }
}
