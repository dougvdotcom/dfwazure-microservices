using System;

namespace WebUI.Models
{
    public class UploadedPhotoViewModel
    {
        public string PhotoName { get; set; }
        public string Username { get; set; }
        public DateTime UploadTime { get; set; }
        public string Caption { get; set; }
        public bool IsApproved { get; set; }
        public string FullPhotoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}