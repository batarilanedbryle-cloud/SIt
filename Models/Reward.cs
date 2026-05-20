using System;

namespace Batarilan_Exercise1.Models
{
    public class Reward
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string StudentName { get; set; }   // ✅ ADD THIS

        public int Points { get; set; }

        public string Reason { get; set; }        // ✅ ADD THIS

        public DateTime DateEarned { get; set; } = DateTime.Now;
    }
}
