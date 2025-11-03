using Disaster_App.Controllers;
using Disaster_App.Data;
using Disaster_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TestProjectDisasterApp.ControllerTests
{
    public class HomeControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly HomeController _controller;
        private readonly ILogger<HomeController> _logger;

        public HomeControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<HomeController>();
            _controller = new HomeController(_logger, _context);

            // Setup dummy session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new DummySession();
            _controller.ControllerContext.HttpContext = httpContext;
        }

        // ==============================
        // Basic pages
        // ==============================
        [Fact]
        public void Index_ReturnsView()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void UserHome_WithSession_ReturnsView()
        {
            _controller.HttpContext.Session.SetString("UserEmail", "test@example.com");
            var result = _controller.UserHome();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task VolunteerHome_WithVolunteer_ReturnsView()
        {
            // Arrange
            var user = new User { FullName = "VolUser", Email = "vol@example.com", PasswordHash = "pass", Role = "Volunteer" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var volunteer = new Volunteer { UserID = user.UserID };
            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetInt32("UserId", user.UserID);
            _controller.HttpContext.Session.SetString("UserEmail", user.Email);

            var result = await _controller.VolunteerHome();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void AdminHome_WithAdmin_ReturnsView()
        {
            _controller.HttpContext.Session.SetString("UserEmail", "admin@example.com");
            _controller.HttpContext.Session.SetString("UserRole", "Admin");

            var result = _controller.AdminHome();
            Assert.IsType<ViewResult>(result);
        }

        // ==============================
        // Register/Login
        // ==============================
        [Fact]
        public async Task Register_ValidUser_RedirectsToLogin()
        {
            var user = new User { FullName = "Test User", Email = "test@example.com", PasswordHash = "password123" };
            var result = await _controller.Register(user);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task Login_ValidUser_RedirectsUserHome()
        {
            var user = new User { FullName = "Login User", Email = "login@example.com", PasswordHash = "password" };
            user.PasswordHash = (string)_controller.GetType()
                .GetMethod("HashPassword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_controller, new object[] { user.PasswordHash })!;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginUser = new User { Email = "login@example.com", PasswordHash = "password" };
            var result = await _controller.Login(loginUser);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirect.ActionName);
        }

        // ==============================
        // LogIncident
        // ==============================
        [Fact]
        public async Task LogIncident_Valid_ReturnsRedirectToUserHome()
        {
            var user = new User { FullName = "Incident User", Email = "inc@example.com", PasswordHash = "pass" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetInt32("UserId", user.UserID);
            _controller.HttpContext.Session.SetString("UserEmail", user.Email);

            var incident = new Incident { Title = "Test Incident", Description = "Desc", Location = "Loc" };
            var result = await _controller.LogIncident(incident);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirect.ActionName);
        }

        // ==============================
        // LogDonation
        // ==============================
        [Fact]
        public async Task LogDonation_Valid_ReturnsRedirectToUserHome()
        {
            var user = new User { FullName = "Donor User", Email = "donor@example.com", PasswordHash = "pass" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetString("UserEmail", user.Email);
            _controller.HttpContext.Session.SetString("UserName", user.FullName);

            var donation = new Donation { DonorName = "", Email = "", ResourceType = "Food", Quantity = 5 };
            var result = await _controller.LogDonation(donation);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirect.ActionName);
        }

        // ==============================
        // Volunteerss
        // ==============================
        [Fact]
        public async Task Volunteerss_ValidRegistration_ReturnsRedirectToUserHome()
        {
            var user = new User { FullName = "VolUser", Email = "volreg@example.com", PasswordHash = "pass" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetInt32("UserId", user.UserID);
            _controller.HttpContext.Session.SetString("UserEmail", user.Email);

            var volunteer = new Volunteer { Skills = "Cooking" };
            var result = await _controller.Volunteerss(volunteer);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirect.ActionName);
        }

        // ==============================
        // VolunteerTask
        // ==============================
        [Fact]
        public async Task VolunteerTask_Valid_ReturnsRedirectToVolunteerHome()
        {
            var user = new User { FullName = "VolTask User", Email = "vtask@example.com", PasswordHash = "pass" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetString("UserEmail", user.Email);

            var task = new VolunteerTask { TaskName = "Clean", Status = "Open" };
            var result = await _controller.VolunteerTask(task);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VolunteerHome", redirect.ActionName);
        }

        // ==============================
        // View actions
        // ==============================
        [Fact]
        public async Task ViewVolunteerTasks_ReturnsView()
        {
            _controller.HttpContext.Session.SetString("UserEmail", "test@example.com");
            var result = await _controller.ViewVolunteerTasks();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task ViewVolunteers_ReturnsView()
        {
            _controller.HttpContext.Session.SetString("UserEmail", "test@example.com");
            var result = await _controller.ViewVolunteers();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task ViewDonations_ReturnsView()
        {
            _controller.HttpContext.Session.SetString("UserEmail", "test@example.com");
            var result = await _controller.ViewDonations();
            Assert.IsType<ViewResult>(result);
        }

       // [Fact]
       // public async Task ViewIncidents_ReturnsView()
        //{
         //   _controller.HttpContext.Session.SetString("UserEmail", "test@example.com");
         //   var result = await _controller.ViewIncidents();
          //  Assert.IsType<ViewResult>(result);
        }
    }

    // ==========================
    // Dummy session implementation
    // ==========================
    public class DummySession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();

        public IEnumerable<string> Keys => _sessionStorage.Keys;

        public string Id => "dummy_session";

        public bool IsAvailable => true;

        public void Clear() => _sessionStorage.Clear();

        public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _sessionStorage.Remove(key);

        public void Set(string key, byte[] value) => _sessionStorage[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }


// ==========================
// Session extension methods
// ==========================


namespace TestProjectDisasterApp.ControllerTests
{
    public static class SessionExtensions
    {
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        public static string? GetString(this ISession session, string key)
        {
            return session.TryGetValue(key, out var val) ? Encoding.UTF8.GetString(val) : null;
        }

        public static void SetInt32(this ISession session, string key, int value)
        {
            session.Set(key, BitConverter.GetBytes(value));
        }

        public static int? GetInt32(this ISession session, string key)
        {
            return session.TryGetValue(key, out var val) ? BitConverter.ToInt32(val) : (int?)null;
        }
    }
}
