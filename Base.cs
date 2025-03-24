using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PlaywrightMultiCompany.Pages
{
    public class BaseTest
    {
        protected IPlaywright Playwright { get; private set; } = default!;
        protected IBrowser Browser { get; private set; } = default!;
        protected IBrowserContext Context { get; private set; } = default!;
        protected IPage Page { get; private set; } = default!;

        [OneTimeSetUp] // Runs once before all tests
        public async Task Init()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        }

        [SetUp] // Runs before each test
        public async Task Setup()
        {
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = "chrome",
                Headless = false,
                SlowMo = 500
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoDir = "videos/",
                ViewportSize = new() { Width = 1280, Height = 720 }
            });

            Page = await Context.NewPageAsync();
        }

        [TearDown] // Runs after each test
        public async Task TearDown()
        {
            try
            {
                var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
                var testName = TestContext.CurrentContext.Test.Name;

                if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    var screenshotPath = Path.Combine("Screenshots", $"{testName}.png");
                    Directory.CreateDirectory("Screenshots");
                    await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during TearDown: {ex.Message}");
            }

            if (Context != null)
            {
                await Context.CloseAsync();
            }

            if (Browser != null)
            {
                await Browser.CloseAsync();
            }

            Playwright?.Dispose();
        }

        [OneTimeTearDown] // Runs once after all tests
        public void Close()
        {
            // Any final cleanup if needed
        }
    }
}
