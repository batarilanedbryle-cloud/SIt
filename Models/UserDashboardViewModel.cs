using System.Collections.Generic;

namespace Batarilan_Exercise1.Models
{
    public class UserDashboardViewModel
    {
        public UserAccount User { get; set; }
        public List<SitIn> SitIns { get; set; }
    }
}
