using Microsoft.Playwright;
using Xunit;

namespace PlaywrightSearch;

public sealed class BrowsersTestData : TheoryData<string, string>
{
    public BrowsersTestData()
    {
        bool useBrowserStack = UseBrowserStack;

        Add(BrowserType.Chromium, null);

        if (useBrowserStack || !OperatingSystem.IsWindows())
        {
            Add(BrowserType.Chromium, "chrome");
        }

        if (useBrowserStack || OperatingSystem.IsWindows())
        {
            Add(BrowserType.Chromium, "msedge");
        }

        Add(BrowserType.Firefox, null);
    }

    public static bool IsRunningInGitHubActions { get; } = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    public static bool UseBrowserStack => BrowserStackCredentials() != default;

    public static (string UserName, string AccessToken) BrowserStackCredentials()
    {
        string userName = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
        string accessToken = Environment.GetEnvironmentVariable("BROWSERSTACK_TOKEN");

        return (userName, accessToken);
    }
}
