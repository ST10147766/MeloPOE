using OpenQA.Selenium;

namespace DisasterApp.UITests.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
        }

        // Empty implementations - won't be called
        public void NavigateToLogin() { }
        public void Login(string email, string password) { }
        public void GoToRegistration() { }
        public bool IsLoginPage() => true;
    }
}