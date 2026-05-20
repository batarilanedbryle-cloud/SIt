using System.ComponentModel.DataAnnotations;

namespace Batarilan_Exercise1.Models
{
    public class UserAccount
    {
        public int Id { get; set; }

        public string IdNumber { get; set; }

        public string LastName { get; set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        public string CourseLevel { get; set; }

        public string Email { get; set; }

        public string Course { get; set; }

        [Required]
        public string Address { get; set; }

        public string Password { get; set; }

        public string Role { get; set; } = "Student";
        public int Points { get; set; } = 0;

        //  public DateTime? TimeOut { get; set; }

        //  public string Status { get; set; }

    }
}
