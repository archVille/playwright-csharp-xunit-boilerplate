namespace PlaywrightSearch;


public class BrowserFixtureOptions
{

    public string BrowserType { get; set; }

    public string BrowserChannel { get; set; }

    public string Build { get; set; }

    public bool CaptureTrace { get; set; } = false;

    public bool CaptureVideo { get; set; } = BrowsersTestData.IsRunningInGitHubActions;

    public string OperatingSystem { get; set; }

    public string OperatingSystemVersion { get; set; }

    public string PlaywrightVersion { get; set; }

    public string ProjectName { get; set; }

    public string TestName { get; set; }

    public bool UseBrowserStack { get; set; }

    public (string UserName, string AccessKey) BrowserStackCredentials { get; set; }

    public Uri BrowserStackEndpoint { get; set; } = new("wss://cdp.browserstack.com/playwright", UriKind.Absolute);
}
