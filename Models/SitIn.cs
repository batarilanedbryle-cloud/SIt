using System;

namespace Batarilan_Exercise1.Models
{
    public class SitIn
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string StudentName { get; set; }

        public string Purpose { get; set; }
        
        public DateTime TimeIn { get; set; } = DateTime.Now;

        public DateTime? TimeOut { get; set; }

        public string Status { get; set; } = "Active";
    }
}
