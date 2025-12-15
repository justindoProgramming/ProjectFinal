    using System.ComponentModel.DataAnnotations;

    namespace PetClinicSystem.Models
    {
        public class AccountProfileViewModel
        {
            [Required]
            [Display(Name = "Full name")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
