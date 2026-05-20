namespace Batarilan_Exercise1.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string StudentName { get; set; }

        public string Purpose { get; set; }

        public DateTime ReservationDate { get; set; }

        public string Room { get; set; }     // ⭐ ADD THIS

        public string PcNumber { get; set; } // ⭐ ADD THIS

        public string Status { get; set; } = "Pending";

        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
