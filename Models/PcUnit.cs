namespace Batarilan_Exercise1.Models
{
    public class PcUnit
    {
        public int Id { get; set; }

        // e.g. "Lab 524"
        public string Room { get; set; } = string.Empty;

        // 1 – 50
        public int PcNumber { get; set; }

        // "Available" | "Unavailable"
        public string Status { get; set; } = "Available";

        // FK to the active reservation that reserved this PC (null if free)
        public int? ReservationId { get; set; }
    }
}
