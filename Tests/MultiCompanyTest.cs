using NUnit.Framework;
using PlaywrightMultiCompany.Pages;
using System;
using System.Threading.Tasks;

namespace PlaywrightMultiCompany.Tests
{
    [TestFixture]
    public class SignupTest : BaseTest
    {
        [Test]
        public async Task SignupFlowTest()
        {
            string testName = TestContext.CurrentContext.Test.Name;

            try
            {
                var signupPage = new MultiCompanySignup(Page);

                Console.WriteLine("Navigating to Signup Page");
                await signupPage.SignupAsync();

                Console.WriteLine("Filling Company Info");
                await signupPage.AdministratorInfoAsync();

                Console.WriteLine("Entering Verification Code");
                await signupPage.EnterVerificationCodeAsync();

                Console.WriteLine("Filling Billing Info");
                await signupPage.CompanySubscriptionAsync();
                
                Console.WriteLine("Filling Company Details");
                await signupPage.UpdateSecondSubsidiaryAsync();

                Console.WriteLine("Filling Company Details");
                await signupPage.DeleteSubsidiaryAsync();
                
                Console.WriteLine("Filling Company Details");
                await signupPage.BillingInfoAsync();

                Console.WriteLine("Filling Bank Info");
                await signupPage.PaymentInfoAsync();

                Console.WriteLine("Placing Order");
                await signupPage.PlaceOrderAsync();

                Console.WriteLine("Verifying Order Summary");
                await signupPage.VerifyOrderSummaryAsync();

                Console.WriteLine($"Test {testName} completed successfully.");
            }
            catch (Exception ex)
            {
                var screenshotPath = $"Screenshots/{testName}.png";
                await Page.ScreenshotAsync(new() { Path = screenshotPath });
                Console.WriteLine($"Test {testName} failed. Screenshot saved at {screenshotPath}");
                Console.WriteLine($"Error Message: {ex.Message}");
                throw;
            }
        }
    }
}
