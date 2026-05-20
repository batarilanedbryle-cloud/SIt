using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Batarilan_Exercise1.Data;
using Batarilan_Exercise1.Models;
using System.Linq;
using System;

namespace Batarilan_Exercise1.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Inject announcements into every view rendered by this controller
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            ViewBag.SidebarAnnouncements = _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .Take(5)
                .ToList();
        }


        // GET: /User/Dashboard
        public IActionResult Dashboard()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return RedirectToAction("Login", "Account");

            var sitIns = _context.SitIns
                .Where(s => s.UserId == id.Value)
                .OrderByDescending(s => s.TimeIn)
                .ToList();

            var announcements = _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .ToList();

            var vm = new UserDashboardViewModel
            {
                User = user,
                SitIns = sitIns
            };

            var rewards = _context.Rewards
            .Where(r => r.UserId == id.Value)
            .ToList();

            var history = _context.LabHistories
                .Where(h => h.UserId == id.Value)
                .OrderByDescending(h => h.Date)
                .ToList() ?? new List<LabHistory>(); 

            int totalSessions = 10;

            int usedSessions = _context.SitIns
                .Count(s => s.UserId == id.Value);

            // Count bonus sessions earned by converting 3 points → 1 session
            int bonusSessions = _context.Rewards
                .Count(r => r.UserId == id.Value && r.Reason == "BONUS_SESSION");

            ViewBag.RemainingSessions = (totalSessions + bonusSessions) - usedSessions;


            ViewBag.Rewards = rewards;
            ViewBag.History = history;

            ViewBag.Announcements = announcements;

            // Student's own submitted feedbacks
            var myFeedbacks = _context.Feedbacks
                .Where(f => f.UserId == id.Value)
                .OrderByDescending(f => f.DateSubmitted)
                .ToList();
            ViewBag.MyFeedbacks = myFeedbacks;

            return View(vm);
        }


        // POST: /User/StartSitIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartSitIn(string purpose)
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return RedirectToAction("Login", "Account");

            var sitIn = new SitIn
            {
                UserId = user.Id,
                StudentName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Replace("  ", " ").Trim(),
                Purpose = string.IsNullOrWhiteSpace(purpose) ? "General" : purpose,
                TimeIn = DateTime.UtcNow,
                Status = "Active"
            };

            _context.SitIns.Add(sitIn);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        // POST: /User/EndSitIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EndSitIn(int sitInId)
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var sitIn = _context.SitIns.FirstOrDefault(s => s.Id == sitInId && s.UserId == id.Value);
            if (sitIn == null) return NotFound();

            sitIn.TimeOut = DateTime.UtcNow;
            sitIn.Status = "Done";

            // Check if student has 3 points → convert into 1 extra session
            var user = _context.Users.Find(id.Value);
            string successMsg = "Session ended successfully.";
            if (user != null && user.Points >= 3)
            {
                user.Points -= 3;
                // Give 1 extra session by increasing RemainingSessions via a negative SitIn credit record
                // We track this by logging a reward with negative "session cost" notation
                _context.Rewards.Add(new Reward
                {
                    UserId = user.Id,
                    StudentName = $"{user.FirstName} {user.LastName}".Trim(),
                    Points = -3,
                    Reason = "Converted 3 Points into 1 Extra Session",
                    DateEarned = DateTime.Now
                });
                // Grant 1 extra session by reducing used session count via a cancelled entry
                // We increase the session allowance stored on the user via a convention:
                // We add a "bonus session" SitIn-like entry — simplest: store bonus in a ViewBag offset
                // Cleanest approach: track bonus sessions in Rewards as a positive "BonusSession" entry
                _context.Rewards.Add(new Reward
                {
                    UserId = user.Id,
                    StudentName = $"{user.FirstName} {user.LastName}".Trim(),
                    Points = 0,
                    Reason = "BONUS_SESSION",
                    DateEarned = DateTime.Now
                });
                successMsg = "Session ended! 🎉 3 points converted into 1 extra session.";
            }

            _context.SaveChanges();

            TempData["Success"] = successMsg;
            return RedirectToAction("Dashboard");
        }

        public IActionResult Profile()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return RedirectToAction("Login", "Account");

            return View(user);
        }

        [HttpPost]
        public IActionResult Profile(UserAccount model)
        {
            var user = _context.Users.Find(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        // GET: /User/Edit/{id}
        public IActionResult Edit(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /User/Edit
        [HttpPost]
        public IActionResult Edit(UserAccount model)
        {
            // Find the existing user first — we update fields individually
            // so we never overwrite Role, Points, or other managed fields.
            var user = _context.Users.Find(model.Id);
            if (user == null) return NotFound();

            // Apply only the editable fields
            user.IdNumber    = model.IdNumber    ?? user.IdNumber;
            user.FirstName   = model.FirstName   ?? user.FirstName;
            user.MiddleName  = model.MiddleName  ?? user.MiddleName;
            user.LastName    = model.LastName    ?? user.LastName;
            user.Email       = model.Email       ?? user.Email;
            user.Course      = model.Course      ?? user.Course;
            user.CourseLevel = model.CourseLevel ?? user.CourseLevel;
            user.Address     = model.Address     ?? user.Address;

            // Only update password if a new one was typed
            if (!string.IsNullOrWhiteSpace(model.Password))
                user.Password = model.Password;

            _context.SaveChanges();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }
        public IActionResult Announcements(string? search)
        {
            var query = _context.Announcements.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(s) || a.Message.ToLower().Contains(s));
            }
            var list = query.OrderByDescending(a => a.DatePosted).ToList();
            ViewBag.Search = search;
            return View(list);
        }


        // POST: /User/SubmitFeedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitFeedback(string message)
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["FeedbackError"] = "Feedback message cannot be empty.";
                return RedirectToAction("Dashboard");
            }

            var feedback = new Feedback
            {
                UserId = user.Id,
                StudentName = $"{user.FirstName} {user.LastName}".Trim(),
                Message = message.Trim(),
                DateSubmitted = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();

            TempData["FeedbackSuccess"] = "Thank you! Your feedback has been submitted.";
            return RedirectToAction("Dashboard");
        }


        // GET: /User/SitInHistory
        public IActionResult SitInHistory()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var history = _context.SitIns
                .Where(s => s.UserId == id.Value)
                .OrderByDescending(s => s.TimeIn)
                .ToList();

            return View(history);
        }

        // GET: /User/MyRewards
        public IActionResult MyRewards()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            var rewards = _context.Rewards
                .Where(r => r.UserId == id.Value)
                .OrderByDescending(r => r.DateEarned)
                .ToList();

            ViewBag.User        = user;
            ViewBag.TotalPoints = rewards.Sum(r => r.Points);
            return View(rewards);
        }

        // GET: /User/MyFeedback
        public IActionResult MyFeedback()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var feedbacks = _context.Feedbacks
                .Where(f => f.UserId == id.Value)
                .OrderByDescending(f => f.DateSubmitted)
                .ToList();

            return View(feedbacks);
        }

        // GET: /User/MyReservations
        public IActionResult MyReservations()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var reservations = _context.Reservations
                .Where(r => r.UserId == id.Value)
                .OrderByDescending(r => r.DateCreated)
                .ToList();

            return View(reservations);
        }

        // POST: /User/SubmitReservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReservation(string purpose, DateTime reservationDate, string room, string pcNumber)
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(purpose))
            {
                TempData["ReservationError"] = "Purpose is required.";
                return RedirectToAction("MyReservations");
            }

            if (reservationDate < DateTime.Today)
            {
                TempData["ReservationError"] = "Reservation date cannot be in the past.";
                return RedirectToAction("MyReservations");
            }

            var reservation = new Reservation
            {
                UserId        = user.Id,
                StudentName   = $"{user.FirstName} {user.LastName}".Trim(),
                Purpose       = purpose.Trim(),
                ReservationDate = reservationDate,
                Room          = string.IsNullOrWhiteSpace(room)     ? "—" : room.Trim(),
                PcNumber      = string.IsNullOrWhiteSpace(pcNumber) ? "—" : pcNumber.Trim(),
                Status        = "Pending",
                DateCreated   = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            TempData["ReservationSuccess"] = "Reservation submitted! Waiting for admin approval.";
            return RedirectToAction("MyReservations");
        }

        // POST: /User/CancelReservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelReservation(int reservationId)
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login", "Account");

            var reservation = _context.Reservations
                .FirstOrDefault(r => r.Id == reservationId && r.UserId == id.Value);

            if (reservation == null)
            {
                TempData["ReservationError"] = "Reservation not found.";
                return RedirectToAction("MyReservations");
            }

            if (reservation.Status != "Pending")
            {
                TempData["ReservationError"] = "Only pending reservations can be cancelled.";
                return RedirectToAction("MyReservations");
            }

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();

            TempData["ReservationSuccess"] = "Reservation cancelled.";
            return RedirectToAction("MyReservations");
        }

    }
}
