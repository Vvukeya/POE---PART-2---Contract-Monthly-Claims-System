using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POE___PART_2___Contract_Monthly_Claims_System.Models
{
    public class Claim
    {
        public Claim()
        {
            Documents = new List<Document>();
        }

        [Key]
        public int ClaimID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public virtual User? User { get; set; }

        [Required(ErrorMessage = "Month is required")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public int Month { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2020, 2030, ErrorMessage = "Year must be between 2020 and 2030")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Module code is required")]
        [Display(Name = "Module Code")]
        public string ModuleCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of sessions is required")]
        [Range(1, 100, ErrorMessage = "Sessions must be between 1 and 100")]
        public int Sessions { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.1, 500, ErrorMessage = "Hours must be between 0.1 and 500")]
        public double Hours { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(1, 1000, ErrorMessage = "Hourly rate must be between 1 and 1000")]
        [Display(Name = "Hourly Rate")]
        public double HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        public double TotalAmount => Hours * HourlyRate;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        [Display(Name = "Correction Notes")]
        public string? CorrectionNotes { get; set; }

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public virtual ICollection<Document> Documents { get; set; }
    }
}