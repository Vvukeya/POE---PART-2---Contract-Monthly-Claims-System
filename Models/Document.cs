using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POE___PART_2___Contract_Monthly_Claims_System.Models
{
    public class Document
    {
        [Key]
        public int DocumentID { get; set; }

        [ForeignKey("Claim")]
        public int ClaimID { get; set; }
        public virtual Claim? Claim { get; set; }

        [Required]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Type")]
        public string FileType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Path")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "File Size")]
        public long FileSize { get; set; }

        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}