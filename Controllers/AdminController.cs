using Microsoft.AspNetCore.Mvc;
using Batarilan_Exercise1.Data;
using Batarilan_Exercise1.Models;
using System;
using System.Linq;
using System.Text;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Users  — view all registered students
    public IActionResult Users(string? search, string? course, string? sortBy)
    {
        var query = _context.Users
            .Where(u => (u.Role ?? "").ToLower() != "admin")
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(s) ||
                (u.IdNumber ?? "").ToLower().Contains(s) ||
                (u.Email    ?? "").ToLower().Contains(s) ||
                (u.Course   ?? "").ToLower().Contains(s));
        }

        // Course filter
        if (!string.IsNullOrWhiteSpace(course))
            query = query.Where(u => u.Course == course);

        // Sort
        query = sortBy switch
        {
            "name"    => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
            "points"  => query.OrderByDescending(u => u.Points),
            "course"  => query.OrderBy(u => u.Course),
            "id"      => query.OrderBy(u => u.IdNumber),
            _         => query.OrderBy(u => u.LastName)
        };

        var students = query.ToList();

        // Distinct courses for filter dropdown
        var courses = _context.Users
            .Where(u => (u.Role ?? "").ToLower() != "admin" && u.Course != null)
            .Select(u => u.Course!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        ViewBag.Search   = search;
        ViewBag.Course   = course;
        ViewBag.SortBy   = sortBy;
        ViewBag.Courses  = courses;
        ViewBag.Total    = students.Count;

        return View(students);
    }


    // Return the existing view file "AdminDashboard.cshtml"
    public IActionResult Dashboard()
    {
        ViewBag.TotalStudents   = _context.Users.Count(u => (u.Role ?? "").ToLower() != "admin");
        ViewBag.ActiveSitIns    = _context.SitIns.Count(s => s.Status == "Active");
        ViewBag.TotalSitIns     = _context.SitIns.Count();
        ViewBag.CompletedSitIns = _context.SitIns.Count(s => s.Status == "Done");
        ViewBag.Announcements   = _context.Announcements
                                    .OrderByDescending(a => a.DatePosted)
                                    .ToList();
        ViewBag.PendingReservations = _context.Reservations.Count(r => r.Status == "Pending");
        ViewBag.RecentReservations  = _context.Reservations
                                        .Where(r => r.Status == "Pending")
                                        .OrderByDescending(r => r.DateCreated)
                                        .Take(5)
                                        .ToList();

        // Purpose/language breakdown for analytics chart
        var purposeGroups = _context.SitIns
            .Where(s => s.Purpose != null)
            .GroupBy(s => s.Purpose)
            .Select(g => new { Purpose = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        ViewBag.PurposeLabels = purposeGroups.Select(g => g.Purpose).ToList();
        ViewBag.PurposeCounts = purposeGroups.Select(g => g.Count).ToList();

        return View("AdminDashboard");
    }

    // SEARCH STUDENT - improved: searches multiple fields (case-insensitive)
    // Excludes Admin accounts from results.
    public IActionResult Search(string keyword)
    {
        // Exclude Admin accounts using ToLower() so EF Core can translate the expression to SQL
        var q = _context.Users
            .Where(u => (u.Role ?? "").ToLower() != "admin")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();

            q = q.Where(u =>
                (u.IdNumber != null && u.IdNumber.ToLower().Contains(k)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(k)) ||
                (u.MiddleName != null && u.MiddleName.ToLower().Contains(k)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(k)) ||
                (u.Email != null && u.Email.ToLower().Contains(k)) ||
                (u.Course != null && u.Course.ToLower().Contains(k)) ||
                (u.CourseLevel != null && u.CourseLevel.ToLower().Contains(k)) ||
                (u.Role != null && u.Role.ToLower().Contains(k))
            );
        }

        var users = q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();
        return View(users);
    }

    // SIT-IN (kept as GET for quick admin action)
    public IActionResult SitIn(string idNumber, string? purpose)
    {
        if (string.IsNullOrEmpty(idNumber))
        {
            TempData["Error"] = "Please enter ID Number!";
            return RedirectToAction("Dashboard");
        }

        var user = _context.Users
            .FirstOrDefault(u => u.IdNumber.Trim() == idNumber.Trim());

        if (user == null)
        {
            TempData["Error"] = "Student not found!";
            return RedirectToAction("Dashboard");
        }

        // prevent duplicate active sit-in
        var existing = _context.SitIns
            .FirstOrDefault(s => s.UserId == user.Id && s.Status == "Active");

        if (existing != null)
        {
            TempData["Error"] = "Student already has active sit-in!";
            return RedirectToAction("CurrentSitIn");
        }

        var sitin = new SitIn
        {
            UserId = user.Id,
            StudentName = user.FirstName + " " + user.LastName,
            Purpose = string.IsNullOrWhiteSpace(purpose) ? "Laboratory" : purpose,
            Status = "Active",
            TimeIn = DateTime.Now
        };

        _context.SitIns.Add(sitin);
        _context.SaveChanges();

        TempData["Success"] = "Sit-in started!";
        return RedirectToAction("CurrentSitIn");
    }




    // CURRENT SIT-IN
    public IActionResult CurrentSitIn()
    {
        var active = _context.SitIns
            .Where(s => s.Status == "Active")
            .ToList();

        return View(active);
    }


    // TIME OUT (now POST with antiforgery)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TimeOut(int id)
    {
        var sitin = _context.SitIns.FirstOrDefault(s => s.Id == id);

        if (sitin == null)
            return NotFound();

        sitin.TimeOut = DateTime.Now;
        sitin.Status = "Done";

        // 🔥 COUNT COMPLETED SESSIONS
        var completedSessions = _context.SitIns
            .Count(s => s.UserId == sitin.UserId && s.Status == "Done");

        // 🔥 GIVE POINT ONLY IF 3 SESSIONS COMPLETED
        if (completedSessions % 3 == 0)
        {
            _context.Rewards.Add(new Reward
            {
                UserId = sitin.UserId,
                StudentName = sitin.StudentName,
                Points = 1,
                Reason = "Completed 3 Sit-In Sessions",
                DateEarned = DateTime.Now
            });
        }
        _context.LabHistories.Add(new LabHistory
        {
            UserId = sitin.UserId,
            Activity = sitin.Purpose,
            Feedback = "Completed session",
            Date = DateTime.Now
        });
        var user = _context.Users
        .FirstOrDefault(u => u.Id == sitin.UserId);

        if (user != null)
        {
            user.Points += 1; // add points
        }



        _context.SaveChanges();

        return RedirectToAction("CurrentSitIn");
    }




    // HISTORY & optional CSV export
    public IActionResult Records(bool export = false)
    {
        var records = _context.SitIns.ToList();

        if (export)
        {
            // Build CSV in memory
            var sb = new StringBuilder();
            sb.AppendLine("Student,Purpose,TimeIn,TimeOut,DurationMinutes,Status");

            foreach (var r in records.OrderByDescending(x => x.TimeIn))
            {
                var timeIn = r.TimeIn.ToString("yyyy-MM-dd HH:mm:ss");
                var timeOut = r.TimeOut.HasValue ? r.TimeOut.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                var duration = r.TimeOut.HasValue ? ((r.TimeOut.Value - r.TimeIn).TotalMinutes).ToString("F0") : "";
                // Escape commas if necessary
                var student = r.StudentName?.Replace("\"", "\"\"") ?? "";
                sb.AppendLine($"\"{student}\",\"{r.Purpose}\",\"{timeIn}\",\"{timeOut}\",\"{duration}\",\"{r.Status}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "sitin-records.csv");
        }

        return View(records);
    }

    // DELETE STUDENT (POST)
    // Removes the user and their sit-in entries. Protect admin accounts from deletion.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        // Prevent deleting Admin accounts
        if ((user.Role ?? "").ToLower() == "admin")
        {
            TempData["Error"] = "Admin accounts cannot be deleted.";
            return RedirectToAction("Search");
        }

        // Remove associated sit-ins to keep data consistent
        var sitIns = _context.SitIns.Where(s => s.UserId == user.Id).ToList();
        if (sitIns.Any())
        {
            _context.SitIns.RemoveRange(sitIns);
        }

        _context.Users.Remove(user);
        _context.SaveChanges();

        TempData["Success"] = "Student deleted successfully.";
        return RedirectToAction("Search");
    }
    public IActionResult Leaderboard()
    {
        var leaderboard = _context.Rewards
            .OrderByDescending(r => r.Points)
            .ToList();

        return View(leaderboard);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateAnnouncement(Announcement model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
        {
            TempData["Error"] = "Title and Message are required!";
            return RedirectToAction("Dashboard");
        }

        model.DatePosted = DateTime.Now;

        _context.Announcements.Add(model);
        _context.SaveChanges();

        TempData["Success"] = "Announcement posted successfully!";
        return RedirectToAction("Dashboard");
    }

    // GET: /Admin/EditAnnouncement/{id}
    public IActionResult EditAnnouncement(int id)
    {
        var announcement = _context.Announcements.Find(id);
        if (announcement == null) return NotFound();
        return View(announcement);
    }

    // POST: /Admin/EditAnnouncement
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditAnnouncement(Announcement model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
        {
            TempData["Error"] = "Title and Message are required!";
            return RedirectToAction("Dashboard");
        }

        var existing = _context.Announcements.Find(model.Id);
        if (existing == null) return NotFound();

        existing.Title   = model.Title.Trim();
        existing.Message = model.Message.Trim();
        _context.SaveChanges();

        TempData["Success"] = "Announcement updated successfully!";
        return RedirectToAction("Dashboard");
    }

    // POST: /Admin/DeleteAnnouncement/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAnnouncement(int id)
    {
        var announcement = _context.Announcements.Find(id);
        if (announcement == null) return NotFound();

        _context.Announcements.Remove(announcement);
        _context.SaveChanges();

        TempData["Success"] = "Announcement deleted.";
        return RedirectToAction("Dashboard");
    }

    public IActionResult Feedbacks()
    {
        var data = _context.Feedbacks
            .OrderByDescending(f => f.DateSubmitted)
            .ToList();

        return View(data);
    }
    public IActionResult TopStudents()
    {
        var top = _context.Users
            .OrderByDescending(u => u.Points)
            .Take(10)
            .ToList();

        return View(top);
    }
    [HttpPost]
    public IActionResult GiveReward(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            TempData["Error"] = "Student not found!";
            return RedirectToAction("Users");
        }

        user.Points += 5; // reward value (you can change this)
        _context.SaveChanges();

        TempData["Success"] = "Reward given successfully!";
        return RedirectToAction("Users");
    }

    // GET: /Admin/Rewards  — main rewards management page
    public IActionResult Rewards()
    {
        var students = _context.Users
            .Where(u => (u.Role ?? "").ToLower() != "admin")
            .OrderBy(u => u.LastName)
            .ToList();

        var rewardHistory = _context.Rewards
            .OrderByDescending(r => r.DateEarned)
            .ToList();

        ViewBag.Students = students;
        ViewBag.RewardHistory = rewardHistory;
        return View();
    }

    // POST: /Admin/GivePoints  — give custom points to a student
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GivePoints(int userId, int points, string reason, string? returnUrl)
    {
        string redirectAction = returnUrl == "/Admin/Users" ? "Users" : "Rewards";

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            TempData["Error"] = "Student not found.";
            return RedirectToAction(redirectAction);
        }

        if (points <= 0)
        {
            TempData["Error"] = "Points must be greater than zero.";
            return RedirectToAction(redirectAction);
        }

        if (string.IsNullOrWhiteSpace(reason))
            reason = "Manual reward by admin";

        // Update user total points
        user.Points += points;

        // Log the reward entry
        _context.Rewards.Add(new Reward
        {
            UserId = user.Id,
            StudentName = $"{user.FirstName} {user.LastName}".Trim(),
            Points = points,
            Reason = reason.Trim(),
            DateEarned = DateTime.Now
        });

        _context.SaveChanges();

        TempData["Success"] = $"✅ {points} point(s) awarded to {user.FirstName} {user.LastName}.";
        return RedirectToAction(redirectAction);
    }

    // POST: /Admin/RevokePoints  — remove a reward entry
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RevokePoints(int rewardId)
    {
        var reward = _context.Rewards.Find(rewardId);
        if (reward == null) return NotFound();

        // Also deduct from user total
        var user = _context.Users.FirstOrDefault(u => u.Id == reward.UserId);
        if (user != null)
            user.Points = Math.Max(0, user.Points - reward.Points);

        _context.Rewards.Remove(reward);
        _context.SaveChanges();

        TempData["Success"] = "Reward entry removed.";
        return RedirectToAction("Rewards");
    }



    // ── RESERVATIONS ─────────────────────────────────────────────────────────
    public IActionResult Reservations(string? status)
    {
        var query = _context.Reservations.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var list = query.OrderByDescending(r => r.ReservationDate).ToList();
        ViewBag.StatusFilter = status;
        ViewBag.PendingCount  = _context.Reservations.Count(r => r.Status == "Pending");
        ViewBag.ApprovedCount = _context.Reservations.Count(r => r.Status == "Approved");
        ViewBag.DeniedCount   = _context.Reservations.Count(r => r.Status == "Denied");
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveReservation(int id)
    {
        var r = _context.Reservations.Find(id);
        if (r != null)
        {
            r.Status = "Approved";

            // Mark the PC as unavailable
            if (!string.IsNullOrEmpty(r.Room) && !string.IsNullOrEmpty(r.PcNumber)
                && int.TryParse(r.PcNumber.Replace("PC-","").Replace("PC ","").Trim(), out int pcNum))
            {
                var pc = _context.PcUnits.FirstOrDefault(p => p.Room == r.Room && p.PcNumber == pcNum);
                if (pc != null)
                {
                    pc.Status        = "Unavailable";
                    pc.ReservationId = r.Id;
                }
            }

            _context.SaveChanges();
            TempData["Success"] = "Reservation approved.";
        }
        return RedirectToAction("Reservations");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DenyReservation(int id)
    {
        var r = _context.Reservations.Find(id);
        if (r != null)
        {
            r.Status = "Denied";

            // Free the PC if it was previously approved/locked
            var pc = _context.PcUnits.FirstOrDefault(p => p.ReservationId == r.Id);
            if (pc != null) { pc.Status = "Available"; pc.ReservationId = null; }

            _context.SaveChanges();
            TempData["Success"] = "Reservation denied.";
        }
        return RedirectToAction("Reservations");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteReservation(int id)
    {
        var r = _context.Reservations.Find(id);
        if (r != null)
        {
            // Free the PC if locked
            var pc = _context.PcUnits.FirstOrDefault(p => p.ReservationId == r.Id);
            if (pc != null) { pc.Status = "Available"; pc.ReservationId = null; }

            _context.Reservations.Remove(r);
            _context.SaveChanges();
            TempData["Success"] = "Reservation deleted.";
        }
        return RedirectToAction("Reservations");
    }

    // ── LEADERBOARD ───────────────────────────────────────────────────────────
    public IActionResult Leaderboard2()
    {
        var students = _context.Users
            .Where(u => (u.Role ?? "").ToLower() != "admin")
            .OrderByDescending(u => u.Points)
            .ToList();

        var rewardCounts = _context.Rewards
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var sitInCounts = _context.SitIns
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        ViewBag.RewardCounts = rewardCounts;
        ViewBag.SitInCounts  = sitInCounts;
        return View(students);
    }

    // ── GENERATE REPORT (CSV) ─────────────────────────────────────────────────
    public IActionResult GenerateReport(string type = "sitin")
    {
        var sb  = new System.Text.StringBuilder();
        var enc = System.Text.Encoding.UTF8;
        string Q(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmm");

        if (type == "sitin")
        {
            sb.AppendLine("ID,Student Name,Purpose,Time In,Time Out,Duration (min),Status");
            var records = _context.SitIns.OrderByDescending(s => s.TimeIn).ToList();
            foreach (var r in records)
            {
                int dur = r.TimeOut.HasValue ? (int)(r.TimeOut.Value - r.TimeIn).TotalMinutes : 0;
                string timeOut = r.TimeOut.HasValue ? r.TimeOut.Value.ToString("yyyy-MM-dd HH:mm") : "";
                sb.AppendLine(string.Join(",",
                    r.Id.ToString(),
                    Q(r.StudentName),
                    Q(r.Purpose),
                    r.TimeIn.ToString("yyyy-MM-dd HH:mm"),
                    timeOut,
                    dur.ToString(),
                    r.Status ?? ""));
            }
            return File(enc.GetBytes(sb.ToString()), "text/csv",
                "SitIn_Report_" + ts + ".csv");
        }
        else if (type == "students")
        {
            sb.AppendLine("ID Number,Last Name,First Name,Middle Name,Course,Year Level,Email,Address,Points");
            var students = _context.Users
                .Where(u => (u.Role ?? "").ToLower() != "admin")
                .OrderBy(u => u.LastName).ToList();
            foreach (var s in students)
                sb.AppendLine(string.Join(",",
                    Q(s.IdNumber), Q(s.LastName), Q(s.FirstName), Q(s.MiddleName),
                    Q(s.Course), Q(s.CourseLevel), Q(s.Email), Q(s.Address),
                    s.Points.ToString()));
            return File(enc.GetBytes(sb.ToString()), "text/csv",
                "Students_Report_" + ts + ".csv");
        }
        else if (type == "rewards")
        {
            sb.AppendLine("ID,Student Name,Points,Reason,Date Earned");
            var rewards = _context.Rewards.OrderByDescending(r => r.DateEarned).ToList();
            foreach (var r in rewards)
                sb.AppendLine(string.Join(",",
                    r.Id.ToString(),
                    Q(r.StudentName),
                    r.Points.ToString(),
                    Q(r.Reason),
                    r.DateEarned.ToString("yyyy-MM-dd HH:mm")));
            return File(enc.GetBytes(sb.ToString()), "text/csv",
                "Rewards_Report_" + ts + ".csv");
        }
        else if (type == "reservations")
        {
            sb.AppendLine("ID,Student Name,Purpose,Reservation Date,Room,PC Number,Status,Created");
            var res = _context.Reservations.OrderByDescending(r => r.ReservationDate).ToList();
            foreach (var r in res)
                sb.AppendLine(string.Join(",",
                    r.Id.ToString(),
                    Q(r.StudentName),
                    Q(r.Purpose),
                    r.ReservationDate.ToString("yyyy-MM-dd"),
                    Q(r.Room),
                    Q(r.PcNumber),
                    r.Status ?? "",
                    r.DateCreated.ToString("yyyy-MM-dd")));
            return File(enc.GetBytes(sb.ToString()), "text/csv",
                "Reservations_Report_" + ts + ".csv");
        }

        return RedirectToAction("Dashboard");
    }

    // GET: /Admin/Reports — report generation page
    public IActionResult Reports()
    {
        ViewBag.SitInCount       = _context.SitIns.Count();
        ViewBag.StudentCount     = _context.Users.Count(u => (u.Role ?? "").ToLower() != "admin");
        ViewBag.RewardCount      = _context.Rewards.Count();
        ViewBag.ReservationCount = _context.Reservations.Count();

        // Per-lab reservation breakdown
        var labGroups = _context.Reservations
            .Where(r => r.Room != null)
            .GroupBy(r => r.Room)
            .Select(g => new {
                Room    = g.Key,
                Total   = g.Count(),
                Pending  = g.Count(r => r.Status == "Pending"),
                Approved = g.Count(r => r.Status == "Approved"),
                Denied   = g.Count(r => r.Status == "Denied")
            })
            .OrderBy(g => g.Room)
            .ToList<dynamic>();
        ViewBag.LabGroups = labGroups;

        // All reservations list (most recent first)
        ViewBag.AllReservations = _context.Reservations
            .OrderByDescending(r => r.ReservationDate)
            .ToList();

        // All sit-in logs (most recent first)
        ViewBag.SitInLogs = _context.SitIns
            .OrderByDescending(s => s.TimeIn)
            .ToList();

        return View();
    }

    // ── PC STATUS PAGE ────────────────────────────────────────────────────────
    public IActionResult PcStatus(string? room)
    {
        var labs = new[] { "Lab 524", "Lab 526", "Lab 528", "Lab 530", "Lab 542", "Lab 544" };
        var selectedRoom = string.IsNullOrEmpty(room) ? labs[0] : room;

        var pcs = _context.PcUnits
            .Where(p => p.Room == selectedRoom)
            .OrderBy(p => p.PcNumber)
            .ToList();

        ViewBag.Labs         = labs;
        ViewBag.SelectedRoom = selectedRoom;
        return View(pcs);
    }

    // POST: toggle a PC's status manually
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TogglePcStatus(int pcId, string returnRoom)
    {
        var pc = _context.PcUnits.Find(pcId);
        if (pc != null)
        {
            pc.Status        = pc.Status == "Available" ? "Unavailable" : "Available";
            pc.ReservationId = pc.Status == "Available" ? null : pc.ReservationId;
            _context.SaveChanges();
        }
        return RedirectToAction("PcStatus", new { room = returnRoom });
    }

    // GET: /Admin/GetPcAvailability?room=Lab+524  — JSON for the reservation form
    public IActionResult GetPcAvailability(string room)
    {
        if (string.IsNullOrEmpty(room))
            return Json(new int[0]);

        var unavailable = _context.PcUnits
            .Where(p => p.Room == room && p.Status == "Unavailable")
            .Select(p => p.PcNumber)
            .ToList();

        return Json(unavailable);
    }

    // GET: /Admin/GetPendingCount — JSON for notification bell
    public IActionResult GetPendingCount()
    {
        var count = _context.Reservations.Count(r => r.Status == "Pending");
        return Json(new { count });
    }

    // POST: /Admin/MarkReservationsRead — clear the "new" flag (session-based)
    [HttpPost]
    public IActionResult MarkReservationsRead()
    {
        HttpContext.Session.SetString("ReservationsLastSeen", DateTime.Now.ToString("o"));
        return Ok();
    }

    // GET: /Admin/Announcements
    public IActionResult Announcements()
    {
        var list = _context.Announcements.OrderByDescending(a => a.DatePosted).ToList();
        return View(list);
    }

    // POST: /Admin/DeleteAnnouncement
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAnnouncement2(int id)
    {
        var ann = _context.Announcements.Find(id);
        if (ann != null) { _context.Announcements.Remove(ann); _context.SaveChanges(); TempData["Success"] = "Announcement deleted."; }
        return RedirectToAction("Announcements");
    }

}
