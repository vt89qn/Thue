using Microsoft.Playwright;

namespace Thue.Common;

class Browser
{
	private static IPlaywright playwright;
	private static IBrowser browser;

	public static async Task<IBrowserContext> NewContext(bool headless, bool cacheResource = true, BrowserNewContextOptions options = null)
	{
		await LaunchBrowserAsync(headless);
		options ??= new BrowserNewContextOptions();
		options.UserAgent ??= Client.DefaultUserAgent;
		options.ViewportSize ??= new ViewportSize { Width = 1200, Height = 900 };

		var context = await browser.NewContextAsync(options);
		if (cacheResource)
		{
			await context.CacheResourcesAsync();
		}
		return context;
	}

	public static async Task LaunchBrowserAsync(bool headless = true, string channel = "chrome", IEnumerable<string> args = null, CancellationToken ct = default)
	{
		if (browser != null) return;

		await Locking.WaitAsync(ct);
		try
		{
			if (browser != null) return;

			playwright = await Playwright.CreateAsync();

			args ??= [];
			args = [.. args, "--disable-blink-features=AutomationControlled"];
			var options = new BrowserTypeLaunchOptions
			{
				Headless = headless,
				Channel = channel,
				Args = args
			};

			browser = await playwright.Chromium.LaunchAsync(options);
		}
		finally
		{
			Locking.Release();
		}
	}
}
