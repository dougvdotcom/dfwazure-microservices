using System.ComponentModel.DataAnnotations;
using System.Web;

namespace WebUI.Models
{
    public class UploadFileModel
    {
        [Required(ErrorMessage = "You must provide a caption.")]
        public string Caption { get; set; }
        [Required(ErrorMessage = "Please provide a JPG file to upload.")]
        public HttpPostedFileBase File { get; set; }
    }
}