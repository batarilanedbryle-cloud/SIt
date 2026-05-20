namespace Batarilan_Exercise1.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime DatePosted { get; set; } = DateTime.Now;
    }

}
