using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bogus;

namespace PlaywrightMultiCompany.Pages
{
    public class MultiCompanySignup
    {
        private readonly IPage _page;
        private string? _verificationCode;
        private bool _isConsoleListenerAttached = false;
        private readonly Faker _faker = new();

        public MultiCompanySignup(IPage page)
        {
            _page = page;
            AttachConsoleListener();
        }

        private void AttachConsoleListener()
        {
            if (_isConsoleListenerAttached) return;

            _page.Console += (_, msg) =>
            {
                Console.WriteLine($"[Console Log]: {msg.Text}");
                var match = Regex.Match(msg.Text, @"\b\d{4,6}\b");
                if (match.Success)
                {
                    _verificationCode = match.Value;
                    Console.WriteLine($"[Captured Verification Code]: {_verificationCode}");
                }
            };

            _isConsoleListenerAttached = true;
        }

        public async Task SignupAsync()
        {
            await _page.GotoAsync("https://qasignup.e-bizsoft.net/Signup", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000
            });
        }

        public async Task AdministratorInfoAsync()
        {
            var fullName = _faker.Name.FullName();
            var phone = _faker.Phone.PhoneNumber("##########");
            var email = _faker.Internet.Email();
            var password = "Aa1234567";

            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Full Name" }).FillAsync(fullName);
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Phone" }).FillAsync(phone);
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Admin Email" }).FillAsync(email);
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(password);
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password", Exact = true }).FillAsync(password);
            await _page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
            await _page.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
        }

        public async Task EnterVerificationCodeAsync()
        {
            Console.WriteLine("[Waiting for Verification Code input...]");
            var verificationInput = _page.GetByRole(AriaRole.Textbox, new() { Name = "Verification Code" });
            await verificationInput.WaitForAsync(new() { Timeout = 30000 });

            int retries = 30;
            while (retries-- > 0 && string.IsNullOrEmpty(_verificationCode))
            {
                await Task.Delay(1000);
            }

            if (string.IsNullOrEmpty(_verificationCode))
                throw new Exception("[Error] Verification code not captured from console logs.");

            Console.WriteLine($"[Entering Verification Code]: {_verificationCode}");
            await verificationInput.FillAsync(_verificationCode);
            await _page.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
            await _page.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Company Name" }).WaitForAsync(new() { Timeout = 30000 });
        }
           public async Task CompanySubscriptionAsync()
{
    Console.WriteLine("[INFO]: Creating Parent Company");
    var parentCompanyData = GenerateCompanyData();
    var parentAdminData = GenerateAdminData();
    await FillCompanyForm(parentCompanyData);
    // await FillAdminForm(parentAdminData, isParent: true);
    await _page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

    // Click "Yes" for Subsidiary
    await ClickYesForSubsidiary();

    // Create subsidiaries
    for (int i = 1; i <= 5; i++)
    {
        Console.WriteLine($"[INFO]: Creating Subsidiary {i}");
        var subsidiaryData = GenerateCompanyData();
        var adminData = (i <= 2) ? GenerateAdminData() : parentAdminData; // First 2 have different admins
        var accountingSystem = (i % 3 == 0) ? "2" : "3"; // Alternate accounting system
        
        await FillCompanyForm(subsidiaryData, accountingSystem);
        await FillAdminForm(adminData, isParent: i > 2);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Add Company" }).ClickAsync();
        await Task.Delay(3000);
    }
    
    Console.WriteLine("[SUCCESS]: Parent Company and 5 Subsidiaries Created Successfully.");
}

private async Task FillCompanyForm((string name, string phone, string address, string city, string zip) companyData, string accountingSystem = "2")
{
    await _page.GetByRole(AriaRole.Textbox, new() { Name = "Company Name" }).FillAsync(companyData.name);
    await _page.Locator("#txtmultiPhone").FillAsync(companyData.phone);
    await _page.GetByRole(AriaRole.Textbox, new() { Name = "Address Line 1" }).FillAsync(companyData.address);
    await _page.GetByRole(AriaRole.Textbox, new() { Name = "City" }).FillAsync(companyData.city);
    await _page.Locator("#cbomultistate").SelectOptionAsync(new[] { "195" });
    await _page.GetByRole(AriaRole.Textbox, new() { Name = "Zip/Postal Code" }).FillAsync(companyData.zip);
    await _page.Locator("#ddlmultiChooseApplication").SelectOptionAsync(accountingSystem);
    await _page.GetByRole(AriaRole.Textbox, new() { Name = "0" }).FillAsync("5");
    await _page.Locator("div#overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
}

private (string name, string phone, string address, string city, string zip) GenerateCompanyData()
{
    return (
        _faker.Company.CompanyName(),
        _faker.Phone.PhoneNumber("##########"),
        _faker.Address.StreetAddress(),
        _faker.Address.City(),
        _faker.Address.ZipCode()
    );
}

private (string fullName, string contact, string email, string password) GenerateAdminData()
{
    return (
        _faker.Name.FullName(),
        _faker.Phone.PhoneNumber("##########"),
        _faker.Internet.Email(),
        "Aa1234567" // Static password for consistency
    );
}

private async Task FillAdminForm((string fullName, string contact, string email, string password) adminData, bool isParent)
{
    var adminCheckbox = _page.GetByRole(AriaRole.Checkbox, new() { Name = "Different admin info for" });

    if (!isParent) // Different Admin Required
    {
        if (!(await adminCheckbox.IsCheckedAsync()))
        {
            await adminCheckbox.CheckAsync();
        }

        await _page.GetByRole(AriaRole.Textbox, new() { Name = "Full Name" }).FillAsync(adminData.fullName);
        await _page.Locator("#txtAdminContact").FillAsync(adminData.contact);
        await _page.GetByRole(AriaRole.Textbox, new() { Name = "Admin Email" }).FillAsync(adminData.email);
        await _page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(adminData.password);
        await _page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(adminData.password);
    }

    await _page.Locator("div#overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
}

private async Task ClickYesForSubsidiary()
{
    var yesButton = _page.GetByRole(AriaRole.Button, new() { Name = "Yes" });
    await yesButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    await yesButton.ScrollIntoViewIfNeededAsync();
    await yesButton.ClickAsync(new() { Force = true });

    Console.WriteLine("[Modal]: Clicked on 'Yes' button.");

    await Task.WhenAll(
        _page.Locator("div.sweet-alert").WaitForAsync(new() { State = WaitForSelectorState.Hidden }),
        _page.Locator(".sweet-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden })
    );
}
 
//   public async Task CompanySubscriptionAsync()
//     {
//         Console.WriteLine("[INFO]: Creating Parent Company");

//         var parentCompanyData = GenerateCompanyData();
//         var parentAdminData = GenerateAdminData();
//         await FillCompanyForm(parentCompanyData);
//         await FillAdminForm(parentAdminData, isParent: true);
//         await _page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

//         // Click "Yes" for Subsidiary
//         await ClickYesForSubsidiary();

//         // **Create first subsidiary with a different admin**
//         Console.WriteLine("[INFO]: Creating First Subsidiary");
//         var firstSubsidiaryData = GenerateCompanyData();
//         var firstAdminData = GenerateAdminData();
//         await FillCompanyForm(firstSubsidiaryData);
//         await FillAdminForm(firstAdminData, isParent: false); // Different admin

//         // Click "Add Company"
//         await _page.GetByRole(AriaRole.Button, new() { Name = "Add Company" }).ClickAsync();
//         await Task.Delay(3000);

//         // **Create second subsidiary with another different admin**
//         Console.WriteLine("[INFO]: Creating Second Subsidiary");
//         var secondSubsidiaryData = GenerateCompanyData();
//         var secondAdminData = GenerateAdminData();
//         await FillCompanyForm(secondSubsidiaryData);
//         await FillAdminForm(secondAdminData, isParent: false); // Different admin

//         // Click "Add Company"
//         await _page.GetByRole(AriaRole.Button, new() { Name = "Add Company" }).ClickAsync();
//         await Task.Delay(3000);

//         // **Create third subsidiary using the parent company's admin**
//         Console.WriteLine("[INFO]: Creating Third Subsidiary (Same Admin as Parent)");
//         var thirdSubsidiaryData = GenerateCompanyData();
//         await FillCompanyForm(thirdSubsidiaryData);
//         await FillAdminForm(parentAdminData, isParent: true); // Same as Parent Admin

//         // Click "Add Company"
//         await _page.GetByRole(AriaRole.Button, new() { Name = "Add Company" }).ClickAsync();
//         await Task.Delay(3000);

//         // **Create fourth subsidiary using the parent company's admin**
//         Console.WriteLine("[INFO]: Creating Fourth Subsidiary (Same Admin as Parent)");
//         var fourthSubsidiaryData = GenerateCompanyData();
//         await FillCompanyForm(fourthSubsidiaryData);
//         await FillAdminForm(parentAdminData, isParent: true); // Same as Parent Admin

//         // Click "Add Company"
//         await _page.GetByRole(AriaRole.Button, new() { Name = "Add Company" }).ClickAsync();
//         await Task.Delay(3000);

//         Console.WriteLine("[SUCCESS]: Parent Company and Subsidiaries Created Successfully.");

//         // Console.WriteLine("[INFO]: Clicking 'Next' Button...");
//         // await _page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
//     }

//     // **Utility Functions**
//     private async Task FillCompanyForm((string name, string phone, string address, string city, string zip) companyData)
//     {
//         await _page.GetByRole(AriaRole.Textbox, new() { Name = "Company Name" }).FillAsync(companyData.name);
//         await _page.Locator("#txtmultiPhone").FillAsync(companyData.phone);
//         await _page.GetByRole(AriaRole.Textbox, new() { Name = "Address Line 1" }).FillAsync(companyData.address);
//         await _page.GetByRole(AriaRole.Textbox, new() { Name = "City" }).FillAsync(companyData.city);
//         await _page.Locator("#cbomultistate").SelectOptionAsync(new[] { "195" });
//         await _page.GetByRole(AriaRole.Textbox, new() { Name = "Zip/Postal Code" }).FillAsync(companyData.zip);
//         await _page.Locator("#ddlmultiChooseApplication").SelectOptionAsync("2");
//         await _page.GetByRole(AriaRole.Textbox, new() { Name = "0" }).FillAsync("5");
//         await _page.Locator("div#overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
//     }

//     private async Task FillAdminForm((string fullName, string contact, string email, string password) adminData, bool isParent)
//     {
//         var adminCheckbox = _page.GetByRole(AriaRole.Checkbox, new() { Name = "Different admin info for" });

//         if (!isParent) // Only check for different admins
//         {
//             if (!(await adminCheckbox.IsCheckedAsync()))
//             {
//                 await adminCheckbox.CheckAsync();
//             }

//             await _page.GetByRole(AriaRole.Textbox, new() { Name = "Full Name" }).FillAsync(adminData.fullName);
//             await _page.Locator("#txtAdminContact").FillAsync(adminData.contact);
//             await _page.GetByRole(AriaRole.Textbox, new() { Name = "Admin Email" }).FillAsync(adminData.email);
//             await _page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(adminData.password);
//             await _page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(adminData.password);
//         }

//         await _page.Locator("div#overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
//     }

//     private async Task ClickYesForSubsidiary()
//     {
//         var yesButton = _page.GetByRole(AriaRole.Button, new() { Name = "Yes" });
//         await yesButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
//         await yesButton.ScrollIntoViewIfNeededAsync();
//         await yesButton.ClickAsync(new() { Force = true });

//         Console.WriteLine("[Modal]: Clicked on 'Yes' button.");

//         await Task.WhenAll(
//             _page.Locator("div.sweet-alert").WaitForAsync(new() { State = WaitForSelectorState.Hidden }),
//             _page.Locator(".sweet-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden })
//         );
//     }

//     // **Data Generators**
//     private (string name, string phone, string address, string city, string zip) GenerateCompanyData()
//     {
//         return (
//             _faker.Company.CompanyName(),
//             _faker.Phone.PhoneNumber("##########"),
//             _faker.Address.StreetAddress(),
//             _faker.Address.City(),
//             _faker.Address.ZipCode()
//         );
        
//     }
//  private (string fullName, string contact, string email, string password) GenerateAdminData()
//     {
//         return (
//             _faker.Name.FullName(),
//             _faker.Phone.PhoneNumber("##########"),
//             _faker.Internet.Email(),
//             "Aa1234567"
//         );
//     }
    
       public async Task UpdateSecondSubsidiaryAsync()
{
    var secondSubsidiary = _page.Locator("#gridView tr").Nth(2); // Select second row

    // Check if row exists
    if (await secondSubsidiary.CountAsync() == 0)
    {
        Console.WriteLine("[ERROR]: Second subsidiary row not found!");
        throw new Exception("No second subsidiary available.");
    }

    // Click the Edit button inside the row
    var editButton = secondSubsidiary.Locator("#editbutton");
    await editButton.ClickAsync();
    await Task.Delay(1500); // Small delay for UI update

    // Generate Fake Company Name
    var faker = new Faker();
    string newCompanyName = faker.Company.CompanyName();

    // Fill updated company name
    var companyNameField = _page.Locator("#txtmultiCompanyName");
    await companyNameField.WaitForAsync();  // Wait for input to appear
    await companyNameField.FillAsync(newCompanyName);

    // Click Update Button
    var updateButton = _page.Locator("#addbutton");
    await updateButton.WaitForAsync();  // Ensure button exists
    await updateButton.ScrollIntoViewIfNeededAsync();
    await updateButton.ClickAsync(new() { Force = true });

    Console.WriteLine($"[SUCCESS]: Company name updated to '{newCompanyName}'");
}

    public async Task DeleteSubsidiaryAsync()
{
    var secondSubsidiary = _page.Locator("#gridView tr").Nth(3); // Select second row

    // Check if row exists
    if (await secondSubsidiary.CountAsync() == 0)
    {
        Console.WriteLine("[ERROR]: Second subsidiary row not found!");
        throw new Exception("No second subsidiary available.");

       
    }

    // Click the Delete button inside the row
    var deleteButton = secondSubsidiary.Locator("#deletebutton");
    await deleteButton.ClickAsync();
    await Task.Delay(1500); // Small delay for UI update

    // Wait for the confirmation popup
    var confirmDialog = _page.Locator(".sweet-alert.visible");
    await confirmDialog.WaitForAsync();

    // Click "OK" button in the confirmation popup
    var confirmButton = confirmDialog.Locator("button.confirm");
    await confirmButton.ClickAsync();
    await Task.Delay(1500); // Allow some time for deletion

    Console.WriteLine("[SUCCESS]: Second subsidiary deleted successfully!");

    Console.WriteLine("[INFO]: Clicking 'Next' Button...");
    await _page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

}


    public async Task BillingInfoAsync()
{
    Console.WriteLine("[Action]: Starting Billing Information process...");

    // Handle overlay if visible
    var overlay = _page.Locator("div#overlay");
    if (await overlay.IsVisibleAsync())
    {
        Console.WriteLine("[Info]: Waiting for overlay to hide.");
        await overlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15000 });
    }
    var billingCheckbox = _page.Locator("input#chkBillingSameAsCompany").First;

    await billingCheckbox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
    await billingCheckbox.ScrollIntoViewIfNeededAsync();

    if (!await billingCheckbox.IsCheckedAsync())
    {
        await billingCheckbox.CheckAsync(new() { Force = true });
        Console.WriteLine("[Action]: Checked 'Billing Same As Company'.");
    }
    else
    {
        Console.WriteLine("[Info]: 'Billing Same As Company' checkbox was already checked.");
    }

    // Wait and click the Next button
    var billingNextButton = _page.Locator("input[onclick*=\"ValidateForm('.reqBilling'\"]");
    await billingNextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
    await billingNextButton.ScrollIntoViewIfNeededAsync();
    await billingNextButton.ClickAsync(new() { Force = true });

    Console.WriteLine("[Action]: Clicked on Billing section Next button.");
    Console.WriteLine("[Success]: Billing Information submitted successfully.");
}
     public async Task PaymentInfoAsync()
        {
            var accountTitle = _faker.Name.FullName();
            var accountNumber = _faker.Finance.Account();
            var routingNumber = _faker.Random.ReplaceNumbers("#########");

            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Account Title" }).FillAsync(accountTitle);
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Account Number" }).FillAsync(accountNumber);
            await _page.Locator("#ddlAccountType").SelectOptionAsync(new SelectOptionValue { Label = "Checking Account" });
            await _page.GetByRole(AriaRole.Textbox, new() { Name = "Routing/ABA Number" }).FillAsync(routingNumber);

            var reviewCheckbox = _page.GetByRole(AriaRole.Checkbox, new() { Name = "Please review and accept ACH" });
            await reviewCheckbox.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await reviewCheckbox.ScrollIntoViewIfNeededAsync();

            if (!await reviewCheckbox.IsCheckedAsync())
            {
                await reviewCheckbox.CheckAsync(new() { Force = true });
            }

            Console.WriteLine("[Action]: Checked 'Please review and accept ACH'.");

            var nextButton = _page.Locator("#btnSubmit");
            await nextButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await nextButton.ScrollIntoViewIfNeededAsync();
            await nextButton.ClickAsync(new() { Force = true });
        }
           
    public async Task PlaceOrderAsync()
{
    var overlay = _page.Locator("#overlay");
    if (await overlay.IsVisibleAsync())
        await overlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

    var understandCheckbox = _page.Locator("#chkUnderstand").First;
    await understandCheckbox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
    await understandCheckbox.ScrollIntoViewIfNeededAsync();

    if (!await understandCheckbox.IsCheckedAsync())
        await understandCheckbox.CheckAsync(new() { Force = true });

    var placeOrderButton = _page.GetByRole(AriaRole.Button, new() { Name = "Place Order" });
    await placeOrderButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
    await placeOrderButton.ScrollIntoViewIfNeededAsync();
    await placeOrderButton.ClickAsync(new() { Force = true });

    Console.WriteLine("[Success]: Order placed successfully.");
}

























           
public async Task VerifyOrderSummaryAsync()
{
    // Subscription Info
    var subscriptionLocator = _page.Locator("//*[@id='DivData']/table/tbody/tr[2]/td[1]/p");
    await subscriptionLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

    var subscriptionText = await subscriptionLocator.InnerTextAsync();
    if (string.IsNullOrEmpty(subscriptionText))
        throw new Exception("[Error]: Missing subscription info.");
    Console.WriteLine($"[Debug]: Subscription info: {subscriptionText}");

    // Admin Info
    var adminLocator = _page.Locator("//*[@id='DivData']/table/tbody/tr[2]/td[2]/p");
    await adminLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

    var adminText = await adminLocator.InnerTextAsync();
    if (string.IsNullOrEmpty(adminText))
        throw new Exception("[Error]: Missing admin info.");
    Console.WriteLine($"[Debug]: Admin info: {adminText}");

    Console.WriteLine("[Success]: Both subscription and admin info verified.");

    // Click on the "Get Started" button
    await _page.GetByRole(AriaRole.Button, new() { Name = "Get Started" }).ClickAsync();
}
    }}