using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Serilog;
using Serilog.Events;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thue.Common;

static class ServiceCollectionExtensions
{
	public static ServiceCollection AddLog(this ServiceCollection services, string rootPath)
	{
		string logPath = Path.Combine(rootPath, "Logs");
		if (!Directory.Exists(logPath))
			Directory.CreateDirectory(logPath);


		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug().MinimumLevel.Override("Microsoft", LogEventLevel.Warning).MinimumLevel.Override("System", LogEventLevel.Warning)
			.WriteTo.Logger(lc => lc
					.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
					.WriteTo.File(
								path: Path.Combine(logPath, "info-.log")
								, restrictedToMinimumLevel: LogEventLevel.Information
								, rollingInterval: RollingInterval.Day
								, retainedFileCountLimit: 3
								, outputTemplate: "{Timestamp:HHmmss.fff}\t\t{Message:lj}{NewLine}"))
			.WriteTo.Logger(lc => lc
					.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
					.WriteTo.File(
								path: Path.Combine(logPath, "warning-.log")
								, restrictedToMinimumLevel: LogEventLevel.Warning
								, rollingInterval: RollingInterval.Day
								, retainedFileCountLimit: 3
								, outputTemplate: "{Timestamp:HHmmss.fff}\t\t{Message:lj}{NewLine}"))
			.WriteTo.Logger(lc => lc
					.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
					.WriteTo.File(
								path: Path.Combine(logPath, "error-.log")
								, restrictedToMinimumLevel: LogEventLevel.Error
								, rollingInterval: RollingInterval.Day
								, retainedFileCountLimit: 3
								, outputTemplate: "{Timestamp:HHmmss.fff}\t\t{Message:lj}{NewLine}{Exception}")

							)
			.CreateLogger();

		services.AddLogging(loggingBuilder =>
		{
			loggingBuilder.ClearProviders();
			loggingBuilder.AddSerilog();
		});
		return services;
	}
}
public static class DataGridViewExtensions
{
	public static void ApplyColumnSetting<T>(this DataGridView gridView, List<T> dataSource, Dictionary<string, GridDropdown> dropdownColumns = null, List<string> checkboxComlumns = null) where T : IGridSetting
	{
		gridView.AutoGenerateColumns = false;
		gridView.DataSource = new BindingList<T>(dataSource);
		var settings = T.Setting.Split('|').Select(x => x.Split(',')).ToList();
		var index = 0;
		foreach (var columnSetting in settings)
		{
			var name = columnSetting[0].Trim();
			var text = columnSetting[1].Trim();
			var width = int.Parse(columnSetting[2].Trim());

			if (!gridView.Columns.Contains(name))
			{
				if (dropdownColumns?.ContainsKey(name) == true)
				{
					DataGridViewComboBoxColumn col = new()
					{
						Name = name,
						DisplayStyleForCurrentCellOnly = true,
						ValueMember = nameof(GridDropdown<>.Value),
						DisplayMember = nameof(GridDropdown<>.Text),
						DataSource = dropdownColumns[name].GetSource()
					};
					gridView.Columns.Add(col);
				}
				else if (checkboxComlumns?.Contains(name) == true)
				{
					DataGridViewCheckBoxColumn col = new()
					{
						Name = name
					};
					gridView.Columns.Add(col);
				}
				else
				{
					gridView.Columns.Add(name, text);
				}
			}

			var column = gridView.Columns[name];
			column.Visible = true;
			column.SortMode = DataGridViewColumnSortMode.NotSortable;
			column.HeaderText = text;
			column.DataPropertyName = name;
			if (width == -1)
			{
				column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			}
			else
			{
				column.Width = width;
			}
			column.DisplayIndex = index++;
		}
	}
}
static class LoggerExtensions
{
	public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, [CallerMemberName] string callerName = "")
	{
		logger.LogError(exception, "An error occurred in {CallerName}", callerName);
	}
}
public static class PlayWrightExtensions
{
	private static readonly Dictionary<string, (byte[] BodyBytes, string ContentType)> cacheResources = [];
	public static Task CacheResourcesAsync(this IBrowserContext context)
	{
		return context.RouteAsync("**/*.{js,css,png,jpg,svg}", async route =>
		{
			try
			{
				var url = route.Request.Url;
				if (!cacheResources.TryGetValue(url, out var cached))
				{
					var response = await route.FetchAsync();
					if (response.Ok)
					{
						var body = await response.BodyAsync();
						cached = (body, response.Headers.GetValueOrDefault("content-type", "text/plain"));
						cacheResources[url] = cached;
					}
					else
					{
						await route.ContinueAsync();
						return;
					}
				}
				await route.FulfillAsync(new RouteFulfillOptions
				{
					BodyBytes = cached.BodyBytes,
					ContentType = cached.ContentType,
				});
			}
			catch
			{
			}
		});
	}
	public static async Task<bool> TryFillAsync(this IPage page, string selector, string text, int delayAfter = 0, int timeOut = 5000)
	{
		try
		{
			await page.Locator(selector).FillAsync(text, new LocatorFillOptions { Timeout = timeOut });
			if (delayAfter > 0)
				await page.WaitForTimeoutAsync(delayAfter);
			return true;
		}
		catch { return false; }
	}

	public static async Task<bool> TryClickAsync(this IPage page, string selector, int delayAfter = 0, int timeOut = 5000)
	{
		try
		{
			await page.Locator(selector).ClickAsync(new LocatorClickOptions { Timeout = timeOut });
			if (delayAfter > 0)
				await page.WaitForTimeoutAsync(delayAfter);
			return true;
		}
		catch
		{ return false; }
	}
	public static async Task<bool> TryCheckAsync(this IPage page, string selector, int delayAfter = 0, int timeOut = 5000)
	{
		try
		{
			await page.Locator(selector).CheckAsync(new LocatorCheckOptions { Timeout = timeOut });
			if (delayAfter > 0)
				await page.WaitForTimeoutAsync(delayAfter);
			return true;
		}
		catch
		{ return false; }
	}
}
public static class StringExtension
{
	public static string ToBase64(this byte[] data) => data == null ? default : Convert.ToBase64String(data);
}