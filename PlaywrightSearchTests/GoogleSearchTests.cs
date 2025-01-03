using Microsoft.Playwright;
using Xunit;

namespace PlaywrightSearch;

public class GoogleSearchTests(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        int exitCode = Program.Main(["install"]);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright exited with code {exitCode}.");
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Theory]
    [ClassData(typeof(BrowsersTestData))]
    public async Task Search_For(string browserType, string browserChannel)
    {
        var options = new BrowserFixtureOptions()
        {
            BrowserType = browserType,
            BrowserChannel = browserChannel,

        };

        if (BrowsersTestData.UseBrowserStack)
        {
            options.BrowserStackCredentials = BrowsersTestData.BrowserStackCredentials();
            options.UseBrowserStack = true;
        }

        var browser = new BrowserFixture(options, outputHelper);
        await browser.WithPageAsync(async (page) =>
        {
            await page.GotoAsync("https://www.google.com/");
            await page.WaitForLoadStateAsync();

            IElementHandle element = await page.QuerySelectorAsync("text='Accept all'");

            if (element is not null)
            {
                await element.ClickAsync();
                await element.WaitForElementStateAsync(ElementState.Hidden);
                await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            }

            await page.FillAsync("[name='q']", "playwright");
            await page.ClickAsync("input[value='Google Search']");
            await page.WaitForSelectorAsync("id=rcnt");
            //await page.ClickAsync($"a:has-text({searchTerm})");
        });
    }
}
