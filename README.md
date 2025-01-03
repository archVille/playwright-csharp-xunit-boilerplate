# playwright-csharp-xunit-boilerplate


## Info

Simple boilerplate project for Playwright - XUnit - UI testing and usage of BrowserStack

## Multiple browser Ability

The test is _[data driven]_ and can run against multiple browsers, which are
determined by the [`BrowsersTestData`] class based on the operating system being
used to run the test.

The `BrowserFixture` class also supports using [BrowserStack Automate] to run
tests, which allows you to run tests for multiple browser.

The `BrowserFixtureOptions` class contains options to customise the behaviour,
such as specifying a build number or project name, amongst other options. 

To enable the use of BrowserStack Automate with the tests, instead of locally
running browser instances, configure the credentials for the tests to use with
the `BROWSERSTACK_USERNAME` and `BROWSERSTACK_TOKEN` environment variables.

## Running

```
dotnet test
```



