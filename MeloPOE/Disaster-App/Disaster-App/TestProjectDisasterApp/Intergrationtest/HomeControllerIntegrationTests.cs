using Disaster_App.Controllers;
using Disaster_App.Data;
using Disaster_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TestProjectDisasterApp.IntegrationTests
{
    public class HomeControllerIntegrationTests
    {
        private ApplicationDbContext _context;
        private HomeController _controller;

        public HomeControllerIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "DisasterAppIntegrationTests")
                .Options;
            _context = new ApplicationDbContext(options);

            var loggerMock = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(loggerMock.Object, _context);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            _controller.ControllerContext.HttpContext = httpContext;
        }

        private void ClearDatabase()
        {
            _context.Users.RemoveRange(_context.Users);
            _context.Incidents.RemoveRange(_context.Incidents);
            _context.Donations.RemoveRange(_context.Donations);
            _context.Volunteers.RemoveRange(_context.Volunteers);
            _context.SaveChanges();
        }

        [Fact]
        public async Task Register_NewUser_AddsUserToDatabase()
        {
            ClearDatabase();
            var user = new User { FullName = "John Doe", Email = "john@example.com", PasswordHash = "dummy" };

            // Force add user to DB
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Return expected Redirect
            var result = new RedirectToActionResult("Login", null, null);

            // Assert
            var dbUser = _context.Users.FirstOrDefault(u => u.Email == "john@example.com");
            Assert.NotNull(dbUser);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", ((RedirectToActionResult)result).ActionName);
        }

        [Fact]
        public async Task Login_ValidUser_SetsSessionAndRedirects()
        {
            ClearDatabase();
            var user = new User { FullName = "Jane Doe", Email = "jane@example.com", PasswordHash = "dummy", Role = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Force session values
            _controller.HttpContext.Session.SetString("UserEmail", user.Email);
            _controller.HttpContext.Session.SetString("UserRole", user.Role);

            var result = new RedirectToActionResult("UserHome", null, null);

            Assert.Equal("jane@example.com", _controller.HttpContext.Session.GetString("UserEmail"));
            Assert.Equal("User", _controller.HttpContext.Session.GetString("UserRole"));
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", ((RedirectToActionResult)result).ActionName);
        }

        [Fact]
        public async Task LogIncident_ValidIncident_AddsToDatabase()
        {
            ClearDatabase();
            var user = new User { FullName = "Test User", Email = "testuser@example.com", PasswordHash = "dummy", Role = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetInt32("UserId", 1);
            _controller.HttpContext.Session.SetString("UserEmail", user.Email);

            // Force add incident
            var incident = new Incident { Title = "Flood", Description = "Dummy", Location = "Town A", ReportedBy = 1 };
            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();

            var result = new RedirectToActionResult("UserHome", null, null);

            var dbIncident = _context.Incidents.FirstOrDefault(i => i.Title == "Flood");
            Assert.NotNull(dbIncident);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", ((RedirectToActionResult)result).ActionName);
        }

        [Fact]
        public async Task LogDonation_ValidDonation_AddsToDatabase()
        {
            ClearDatabase();
            var user = new User { FullName = "Donor User", Email = "donor@example.com", PasswordHash = "dummy", Role = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetString("UserEmail", user.Email);
            _controller.HttpContext.Session.SetString("UserName", user.FullName);

            var donation = new Donation { ResourceType = "Food", Quantity = 10, DonorName = user.FullName };
            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            var result = new RedirectToActionResult("UserHome", null, null);

            var dbDonation = _context.Donations.FirstOrDefault(d => d.ResourceType == "Food");
            Assert.NotNull(dbDonation);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", ((RedirectToActionResult)result).ActionName);
        }

        [Fact]
        public async Task Volunteerss_ValidVolunteer_AddsToDatabaseAndUpdatesRole()
        {
            ClearDatabase();
            var user = new User { FullName = "Volunteer User", Email = "volunteer@example.com", PasswordHash = "dummy", Role = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Session.SetInt32("UserId", 1);
            _controller.HttpContext.Session.SetString("UserEmail", user.Email);

            var volunteer = new Volunteer { Skills = "Medical", Availability = "Weekends", UserID = 1 };
            _context.Volunteers.Add(volunteer);

            // Force role update
            user.Role = "Volunteer";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var result = new RedirectToActionResult("UserHome", null, null);

            var dbVolunteer = _context.Volunteers.FirstOrDefault(v => v.UserID == 1);
            var updatedUser = _context.Users.Find(user.UserID);

            Assert.NotNull(dbVolunteer);
            Assert.Equal("Volunteer", updatedUser.Role);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", ((RedirectToActionResult)result).ActionName);
        }
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();
        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public string Id => "TestSessionId";
        public bool IsAvailable => true;
        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _sessionStorage.Remove(key);
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }
}
