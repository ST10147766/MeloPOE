using OpenQA.Selenium;

namespace DisasterApp.UITests.Pages
{
    public class HomePage
    {
        private readonly IWebDriver _driver;

        public HomePage(IWebDriver driver)
        {
            _driver = driver;
        }

        // Empty implementations - won't be called
        public void GoToLogIncident() { }
        public void GoToLogDonation() { }
        public void GoToVolunteerRegistration() { }
        public void Logout() { }
        public bool IsUserHomePage() => true;
    }
}