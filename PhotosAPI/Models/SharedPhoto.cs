using System;
using System.Drawing;

namespace PhotosAPI.Models
{
    public class SharedPhoto
    {
        public string Title { get; set; }
        public string Username { get; set; }
        public string Caption { get; set; }
        public DateTime UploadedDateTime { get; set; }
        public Image Photo { get; set; }
    }
}