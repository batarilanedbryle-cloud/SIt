using System;

namespace Batarilan_Exercise1.Models
{
    public class LabHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Activity { get; set; }
        public string Feedback { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
