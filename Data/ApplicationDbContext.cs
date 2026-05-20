using Microsoft.EntityFrameworkCore;
using Batarilan_Exercise1.Models;

namespace Batarilan_Exercise1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SitIn> SitIns { get; set; }

        public DbSet<UserAccount> Users { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<LabHistory> LabHistories { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Batarilan_Exercise1.Models.PcUnit> PcUnits { get; set; }

    }
}
