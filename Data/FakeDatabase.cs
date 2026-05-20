using Batarilan_Exercise1.Models;
using System.Collections.Generic;

namespace Batarilan_Exercise1.Data
{
    public static class FakeDatabase
    {
        public static List<UserAccount> Users = new List<UserAccount>()
        {
            new UserAccount
            {
                Id = 1,
                IdNumber = "ADMIN001",
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@ccs.com",
                Password = "admin123",
                Role = "Admin"
            }
        };
    }
}
