using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using Xunit;

namespace PlaywrightSearch;

public class BrowserFixture(
    BrowserFixtureOptions options,
    ITestOutputHelper outputHelper)
{
    private const string VideosDirectory = "videos";

    private BrowserFixtureOptions Options { get; } = options;

    public async Task WithPageAsync(
        Func<IPage, Task> action,
        [CallerMemberName] string testName = null)
    {
        string activeTestName = Options.TestName ?? testName;

        using IPlaywright playwright = await Playwright.CreateAsync();
        string videoUrl = null;

        await using (IBrowser browser = await CreateBrowserAsync(playwright, activeTestName))
        {
            BrowserNewContextOptions options = CreateContextOptions();

            await using IBrowserContext context = await browser.NewContextAsync(options);

            if (Options.CaptureTrace)
            {
                await context.Tracing.StartAsync(new TracingStartOptions()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                    Title = activeTestName
                });
            }

            IPage page = await context.NewPageAsync();
            page.Console += (_, e) => outputHelper.WriteLine(e.Text);
            page.PageError += (_, e) => outputHelper.WriteLine(e);

            try
            {
                await action(page);
                await TrySetSessionStatusAsync(page, "passed");
            }
            catch (Exception ex)
            {
                await TryCaptureScreenshotAsync(page, activeTestName);
                await TrySetSessionStatusAsync(page, "failed", ex.Message);
                throw;
            }
            finally
            {
                if (Options.CaptureTrace && !Options.UseBrowserStack)
                {
                    string traceName = GenerateFileName(activeTestName, ".zip");
                    string path = Path.Combine("traces", traceName);

                    await context.Tracing.StopAsync(new TracingStopOptions()
                    {
                        Path = path
                    });
                }

                videoUrl = await TryCaptureVideoAsync(page, activeTestName);
            }
        }

        if (videoUrl is not null)
        { 
            await CaptureBrowserStackVideoAsync(videoUrl, activeTestName);
        }
    }

    protected virtual BrowserNewContextOptions CreateContextOptions()
    {
        var options = new BrowserNewContextOptions()
        {
            Locale = "en-GB",
            TimezoneId = "Europe/London",
        };

        if (Options.CaptureVideo)
        {
            options.RecordVideoDir = Path.GetTempPath();
        }

        return options;
    }

    private async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright, string testName)
    {
        var options = new BrowserTypeLaunchOptions()
        {
            Channel = Options.BrowserChannel,
        };

        if (System.Diagnostics.Debugger.IsAttached)
        {
            options.Headless = false;
            options.SlowMo = 100;
        }

        var browserType = playwright[Options.BrowserType];

        if (Options.UseBrowserStack && Options.BrowserStackCredentials != default)
        {
            string browser;

            if (!string.IsNullOrEmpty(options.Channel))
            {
                browser = options.Channel switch
                {
                    "msedge" => "edge",
                    _ => options.Channel,
                };
            }
            else
            {
                browser = "playwright-" + Options.BrowserType;
            }

            string playwrightVersion =
                Options.PlaywrightVersion ??
                typeof(IBrowser).Assembly.GetName()!.Version.ToString(3);

            var capabilities = new Dictionary<string, string>()
            {
                ["browser"] = browser,
                ["browserstack.accessKey"] = Options.BrowserStackCredentials.AccessKey,
                ["browserstack.username"] = Options.BrowserStackCredentials.UserName,
                ["build"] = Options.Build ?? GetDefaultBuildNumber(),
                ["client.playwrightVersion"] = playwrightVersion,
                ["name"] = testName,
                ["os"] = Options.OperatingSystem,
                ["os_version"] = Options.OperatingSystemVersion,
                ["project"] = Options.ProjectName ?? GetDefaultProject(),
            };

            string json = JsonSerializer.Serialize(capabilities);
            string wsEndpoint = QueryHelpers.AddQueryString(Options.BrowserStackEndpoint.ToString(), "caps", json);

            var connectOptions = new BrowserTypeConnectOptions()
            {
                SlowMo = options.SlowMo,
                Timeout = options.Timeout
            };

            return await browserType.ConnectAsync(wsEndpoint, connectOptions);
        }

        return await browserType.LaunchAsync(options);
    }

    private static string GetDefaultBuildNumber()
    {
        string build = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");

        if (!string.IsNullOrEmpty(build))
        {
            return build;
        }

        return typeof(BrowserFixture).Assembly.GetName().Version.ToString(3);
    }

    private static string GetDefaultProject()
    {
        string project = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");

        if (!string.IsNullOrEmpty(project))
        {
            return project.Split('/')[1];
        }

        return "dotnet-playwright-tests";
    }

    private async Task TrySetSessionStatusAsync(IPage page, string status, string reason = "")
    {
        if (!Options.UseBrowserStack)
        {
            return;
        }

        string json = JsonSerializer.Serialize(new
        {
            action = "setSessionStatus",
            arguments = new
            {
                status,
                reason
            }
        });

        await page.EvaluateAsync("_ => {}", $"browserstack_executor: {json}");
    }

    private string GenerateFileName(string testName, string extension)
    {
        string browserType = Options.BrowserType;

        if (!string.IsNullOrEmpty(Options.BrowserChannel))
        {
            browserType += "_" + Options.BrowserChannel;
        }

        string os =
            OperatingSystem.IsLinux() ? "linux" :
            OperatingSystem.IsMacOS() ? "macos" :
            OperatingSystem.IsWindows() ? "windows" :
            "other";

        string utcNow = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        return $"{testName}_{browserType}_{os}_{utcNow}{extension}";
    }

    private async Task TryCaptureScreenshotAsync(
        IPage page,
        string testName)
    {
        try
        {
            // Generate a unique name for the screenshot
            string fileName = GenerateFileName(testName, ".png");
            string path = Path.Combine("screenshots", fileName);

            await page.ScreenshotAsync(new PageScreenshotOptions()
            {
                Path = path,
            });

            outputHelper.WriteLine($"Screenshot saved to {path}.");
        }
        catch (Exception ex)
        {
            outputHelper.WriteLine("Failed to capture screenshot: " + ex);
        }
    }

    private async Task<string> TryCaptureVideoAsync(
        IPage page,
        string testName)
    {
        if (!Options.CaptureVideo || page.Video is null)
        {
            return null;
        }

        try
        {
            string fileName = GenerateFileName(testName, ".webm");
            string path = Path.Combine(VideosDirectory, fileName);

            if (Options.UseBrowserStack)
            {
                string session = await page.EvaluateAsync<string>("_ => {}", "browserstack_executor: {\"action\":\"getSessionDetails\"}");

                using var document = JsonDocument.Parse(session);
                return document.RootElement.GetProperty("video_url").GetString();
            }
            else
            {
                await page.CloseAsync();
                await page.Video.SaveAsAsync(path);
            }

            outputHelper.WriteLine($"Video saved to {path}.");

            return null;
        }
        catch (Exception ex)
        {
            outputHelper.WriteLine("Failed to capture video: " + ex);
            return null;
        }
    }

    private async Task CaptureBrowserStackVideoAsync(string videoUrl, string testName)
    {
        using var client = new HttpClient();

        for (int i = 0; i < 10; i++)
        {
            using var response = await client.GetAsync(videoUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }

            response.EnsureSuccessStatusCode();

            string extension = Path.GetExtension(response.Content.Headers.ContentDisposition?.FileName) ?? ".mp4";
            string fileName = GenerateFileName(testName, extension);
            string path = Path.Combine(VideosDirectory, fileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(VideosDirectory);
            }

            using var file = File.OpenWrite(path);

            using var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(file);

            outputHelper.WriteLine($"Video saved to {path}.");
            break;
        }
    }
}
