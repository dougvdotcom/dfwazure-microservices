using System;
using Newtonsoft.Json;

namespace GalleryAPI.Models
{
    public class UploadedImage
    {
        [JsonProperty("photoname")]
        public string PhotoName { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("uploadtime")]
        public DateTime UploadTime { get; set; }
        [JsonProperty("imageurl")]
        public string ImageUrl { get; set; }
        [JsonProperty("thumburl")]
        public string ThumbUrl { get; set; }
        [JsonProperty("caption")]
        public string Caption { get; set; }
    }
}