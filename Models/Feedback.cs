using System;
namespace Batarilan_Exercise1.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string StudentName { get; set; }

        public string Message { get; set; }

        public DateTime DateSubmitted { get; set; } = DateTime.Now;
    }
}
