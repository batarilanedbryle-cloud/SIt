using System.Diagnostics;
using Batarilan_Exercise1.Data;
using Batarilan_Exercise1.Models;
using Microsoft.AspNetCore.Mvc;

namespace Batarilan_Exercise1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var leaderboard = _context.Users
                .Where(u => (u.Role ?? "").ToLower() != "admin")
                .OrderByDescending(u => u.Points)
                .ToList();

            var rewardCounts = _context.Rewards
                .GroupBy(r => r.UserId)
                .ToDictionary(g => g.Key, g => g.Count());

            var sitInCounts = _context.SitIns
                .GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.Leaderboard  = leaderboard;
            ViewBag.RewardCounts = rewardCounts;
            ViewBag.SitInCounts  = sitInCounts;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
