
using System.ComponentModel.DataAnnotations;
using System.Web;


namespace Art_Logic.Models
{
    public class IntToHex
    {
        [Range(-8192, 8191,
        ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        [Display(Name = "Integer")]
        public int? IntToConvert { get; set; }

        [MaxLength(4, ErrorMessage = "Value for {0} must be in the range [0000..7F7F]")]
        [Display(Name = "Hexadecimal")]
        public string HexToConvert { get; set; }

        [Display(Name = "Upload a File to Convert")]
        public HttpPostedFileBase File { get; set; }
    }
}