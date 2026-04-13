using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using OfficeOpenXml;
using Svg;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Thue.Common;

namespace Thue.Forms;

public partial class TraCuuHoaDon : Form
{

	private CancellationTokenSource runningCts;
	private Task runningTask;
	private bool isRunning = false;

	private List<TraCuuHoaDonGridModel> gridModels = [];
	private readonly List<LoaiHoaDon> loaiHoaDons = LoaiHoaDon.List;
	private IBrowserContext browserContext;

	private readonly ILogger logger;

	string excelFilePath = string.Empty;
	const string ExcelSheetName = "HOA DON";
	const string ScreenshotFolderName = "screenshot_tchd";

	public TraCuuHoaDon(ILogger<TraCuuHoaDon> logger)
	{
		InitializeComponent();
		this.Icon = Properties.Resources.icon;
		this.logger = logger;
	}
	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		this.btnStartStop.Enabled = false;
		this.lblStatus.Text = $"Import file excel có sheet [{ExcelSheetName}] để bắt đầu";
	}
	protected override async void OnFormClosing(FormClosingEventArgs e)
	{
		if (runningCts?.IsCancellationRequested == false)
		{
			runningCts.Cancel();
		}
		if (runningTask != null && !runningTask.IsCompleted)
		{
			try
			{
				await runningTask;
			}
			catch (Exception ex)
			{
				logger.LogError(ex);
			}
		}
		base.OnFormClosing(e);
	}
	private async void BtnStartStop_Click(object sender, EventArgs e)
	{
		if (!isRunning)
		{
			isRunning = true;
			btnStartStop.Text = btnStartStop.ToolTipText = "  Dừng lại  ";
			btnStartStop.Image = Properties.Resources.stop;
			btnStartStop.Enabled = true;
			btnImport.Enabled = txtRetryOnError.Enabled = txtThreadCount.Enabled = false;
			runningCts = new CancellationTokenSource();
			try
			{
				browserContext ??= await Browser.NewContext(headless: true);
				runningTask = RunningAsync(runningCts.Token);
				await runningTask;
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				logger.LogError(ex);
			}
			finally
			{
				await UpdateExcelAsync();

				isRunning = false;
				btnStartStop.Text = btnStartStop.ToolTipText = "  Bắt đầu  ";
				btnStartStop.Image = Properties.Resources.start;
				btnStartStop.Enabled = true;
				btnImport.Enabled = txtRetryOnError.Enabled = txtThreadCount.Enabled = true;

				runningCts.Dispose();
				runningCts = null;
			}
		}
		else
		{
			btnStartStop.Text = btnStartStop.ToolTipText = "  Đang dừng  ";
			btnStartStop.Enabled = false;
			runningCts.Cancel();
		}
	}
	private void BtnImport_Click(object sender, EventArgs e)
	{
		//yêu cầu user chọn file excel
		using var ofd = new OpenFileDialog();
		ofd.Filter = "Excel files |*.xls;*.xlsx";
		ofd.FilterIndex = 2;
		ofd.RestoreDirectory = true;

		if (ofd.ShowDialog() != DialogResult.OK) return;


		gridModels = [];

		//Get the path of specified file
		excelFilePath = ofd.FileName;
		using var package = new ExcelPackage(new FileInfo(excelFilePath));

		var workSheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name.Equals(ExcelSheetName, StringComparison.OrdinalIgnoreCase));
		if (workSheet == null)
		{
			MessageBox.Show($"Không tìm thấy sheet có tên [{ExcelSheetName}]", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		lblStatus.Text = $"File excel : {excelFilePath}";

		var hasStt = (workSheet.Cells[1, 1].Value ?? "").ToString().Equals("STT", StringComparison.OrdinalIgnoreCase);
		int stt = 1;
		for (int iRow = 2; ; iRow++)
		{
			var iCol = hasStt ? 1 : 0;
			var mstNguoiBan = (workSheet.Cells[iRow, ++iCol].Value ?? "").ToString();
			if (string.IsNullOrWhiteSpace(mstNguoiBan)) break;

			gridModels.Add(new TraCuuHoaDonGridModel
			{
				RowIdx = iRow,
				ColIdx = iCol,
				Stt = stt++,
				MstNguoiBan = mstNguoiBan,
				KyHieuHoaDon = (workSheet.Cells[iRow, ++iCol].Value ?? "").ToString(),
				SoHoaDon = (workSheet.Cells[iRow, ++iCol].Value ?? "").ToString(),
				TongTienThanhToan = (workSheet.Cells[iRow, ++iCol].Value ?? "").ToString(),
				LoaiHoaDon = (workSheet.Cells[iRow, ++iCol].Value ?? "").ToString(),
				UpdateAt = workSheet.Cells[iRow, iCol + 3].Value is DateTime updateAt ? updateAt : workSheet.Cells[iRow, iCol + 3].Value is double oa ? DateTime.FromOADate(oa) : DateTime.MinValue,
			});
		}

		foreach (var model in gridModels)
		{
			if (model.UpdateAt > DateTime.Now.AddHours(-12))
			{
				model.Info = $"Tra cứu lúc {model.UpdateAt.ToString(SystemConst.DateTimeFormat)}, bỏ qua";
				model.IsSuccess = true;
				continue;
			}
			model.IsSuccess = false;
		}
		ReloadGrid();
		btnStartStop.Enabled = true;
	}
	async Task RunningAsync(CancellationToken ct)
	{
		try
		{
			if (gridModels.Count == 0) return;
			var tasks = new List<Task>();
			if (!int.TryParse(txtThreadCount.Text, out var threadCount) || threadCount <= 0)
			{
				threadCount = 3;
			}
			if (!int.TryParse(txtRetryOnError.Text, out var retryOnError) || retryOnError < 0)
			{
				retryOnError = 3;
			}

			foreach (var model in gridModels)
			{
				if (model.IsSuccess) return;
				var task = RunningAsync(model, retryOnError, ct);
				tasks.Add(task);
				if (tasks.Count >= threadCount)
				{
					var completedTask = await Task.WhenAny(tasks);
					tasks.Remove(completedTask);
				}
				await Task.Delay(200, ct);
			}
			await Task.WhenAll(tasks);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			logger.LogError(ex);
		}
	}

	async Task RunningAsync(TraCuuHoaDonGridModel model, int retryOnError, CancellationToken ct)
	{
		var loaiHd = loaiHoaDons.FirstOrDefault(x => x.title == model.LoaiHoaDon);
		if (loaiHd == null)
		{
			model.Info = "Mã loại hóa đơn không hợp lệ";
			return;
		}

		var http = new Client();
		var cookies = new CookieContainer();
		http.Handler.UseCookies = true;
		http.Handler.CookieContainer = cookies;

		for (int i = 1; i <= retryOnError; i++)
		{
			try
			{
				model.Info = $"{i} | Đang tải captcha...";

				var resCaptcha = await http.Get<JsonObject>("https://hoadondientu.gdt.gov.vn:30000/captcha");
				var captchaKey = resCaptcha?["key"]?.GetValue<string>();
				var captchaContent = resCaptcha?["content"]?.GetValue<string>();

				if (string.IsNullOrEmpty(captchaContent))
				{
					model.Info = $"{i} | Không lấy được captcha";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}

				var svgDocument = SvgDocument.FromSvg<SvgDocument>(captchaContent);

				using var original = svgDocument.Draw();
				using var newBitmap = new Bitmap(original.Width, original.Height);

				using (var g = Graphics.FromImage(newBitmap))
				{
					g.Clear(Color.White);
					g.DrawImage(original, 0, 0);
				}
				using var output = new MemoryStream();
				newBitmap.Save(output, ImageFormat.Jpeg);
				var bytes = output.ToArray();

				var processedBytes = bytes;
				model.Info = $"{i} | Đang ocr captcha...";
				var captchaText = await Captcha.OcrAsync(processedBytes.ToBase64(), ct);
				captchaText = Regex.Replace(captchaText ?? "", @"[^a-zA-Z0-9]", "");
				if (captchaText?.Length != 6)
				{
					model.Info = $"{i} | Ocr captcha không thành công";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}
				captchaText = captchaText.ToLower();
				model.Info = $"{i} | Đợi 4 giây trước khi tra cứu...";
				await Task.Delay(TimeSpan.FromSeconds(4), ct);

				using var httpResponse = await http.Get($"https://hoadondientu.gdt.gov.vn:30000/query/guest-invoices").Query($"khmshdon={loaiHd.value.khmshdon}&hdon={loaiHd.value.hdon}&nbmst={model.MstNguoiBan}&khhdon={model.KyHieuHoaDon}&shdon={model.SoHoaDon}&tgtttbso={model.TongTienThanhToan}&cvalue={captchaText}&ckey={captchaKey}");
				if (httpResponse?.IsSuccessStatusCode != true)
				{
					var code = (int)(httpResponse?.StatusCode ?? 0);
					var msg = "Không xác định";
					try
					{
						var res = await httpResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
						msg = res?["message"].GetValue<string>() ?? msg;
					}
					catch
					{
					}
					model.Info = $"{i} | Tra cứu thất bại: {code} / {msg}";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}

				var queryResult = await httpResponse?.Content?.ReadAsStringAsync(ct);

				model.Info = $"{i} | Đang chụp ảnh màn  hình...";
				var extractRes = await ExtractResult(model, resCaptcha, queryResult, ct);
				var screenshotPath = Path.Combine(Path.GetDirectoryName(excelFilePath), ScreenshotFolderName);
				if (!Directory.Exists(screenshotPath))
				{
					Directory.CreateDirectory(screenshotPath);
				}
				var fileName = $"{model.MstNguoiBan}_{model.KyHieuHoaDon}_{model.SoHoaDon}_{DateTime.Now:yyyyMMddHHmmss}.png";
				File.WriteAllBytes(Path.Combine(screenshotPath, fileName), extractRes.Screenshot);
				model.Result = extractRes.Result;

				model.Info = $"Hoàn thành";
				model.IsSuccess = true;
				model.IsPendingSave = true;
				model.ScreenShot = $"{ScreenshotFolderName}/{fileName}";
				return;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger.LogError(ex);
				model.Info = $"{i} | Có lỗi xảy ra , vui lòng liên hệ hỗ trợ. {ex.Message}";
				await Task.Delay(TimeSpan.FromSeconds(3), ct);
			}
		}
	}
	byte[] cacheHomePage = null;
	private async Task<(byte[] Screenshot, string Result)> ExtractResult(TraCuuHoaDonGridModel model, JsonObject captcha, string queryResult, CancellationToken ct)
	{
		var page = await browserContext.NewPageAsync();
		await page.RouteAsync("**/captcha", async route =>
			{
				if (route.Request.Method == "GET")
				{
					await route.FulfillAsync(new()
					{
						Status = 200,
						ContentType = "application/json",
						Body = captcha.ToJsonString()
					});
				}
			});
		await page.RouteAsync("**/query/guest-invoices?*", async route =>
		{
			if (route.Request.Method == "GET")
			{
				await route.FulfillAsync(new()
				{
					Status = 200,
					ContentType = !string.IsNullOrEmpty(queryResult) ? "application/json" : "",
					Body = queryResult
				});
			}
		});
		await page.RouteAsync("https://hoadondientu.gdt.gov.vn/", async route =>
		{
			if (route.Request.Method == "GET")
			{
				if (cacheHomePage == null)
				{
					var response = await route.FetchAsync();
					if (response.Ok)
					{
						cacheHomePage = await response.BodyAsync();
					}
				}
				if (cacheHomePage != null)
				{
					await route.FulfillAsync(new()
					{
						Status = 200,
						ContentType = "text/html; charset=utf-8",
						BodyBytes = cacheHomePage
					});
				}
				else
				{
					await route.ContinueAsync();
				}
			}
		});
		await page.GotoAsync("https://hoadondientu.gdt.gov.vn", new() { Timeout = 5000 });
		await page.TryClickAsync("button[type='button'][aria-label='Close'].ant-modal-close");
		await page.TryFillAsync("#nbmst", model.MstNguoiBan);
		await page.Locator("#lhdon div.ant-select-selection-selected-value span").EvaluateAsync($"el => el.innerText = '{model.LoaiHoaDon}'");
		await page.TryFillAsync("#khhdon", model.KyHieuHoaDon);
		await page.TryFillAsync("#shdon", model.SoHoaDon);
		await page.TryFillAsync("#tgtttbso", model.TongTienThanhToan);
		await page.TryFillAsync("#cvalue", "12345");
		await page.TryClickAsync("button[type='submit']");
		await page.WaitForSelectorAsync("div.ant-row-flex.ant-row-flex-top", new() { Timeout = 2000 });
		byte[] screenshot = await page.Locator("div.ant-row-flex.ant-row-flex-top").ScreenshotAsync(new() { Type = ScreenshotType.Png });

		var paragraphs = page.Locator(".styles__SearchContentBox-sc-1ljhobs-0 p");
		var texts = await paragraphs.AllTextContentsAsync();
		string result = string.Join("\r\n", texts).Trim();

		await page.CloseAsync();
		return (screenshot, result);
	}

	private void ReloadGrid()
	{
		dataGrid.ApplyColumnSetting(gridModels);
		for (int iCol = 0; iCol < dataGrid.Columns.Count; iCol++)
		{
			dataGrid.Columns[iCol].ReadOnly = true;
		}
	}

	async Task UpdateExcelAsync()
	{
		try
		{
			if (!gridModels.Any(x => x.IsSuccess && x.IsPendingSave)) return;
			while (Helpers.IsFileLocked(excelFilePath))
			{
				MessageBox.Show($"File excel [{Path.GetFileName(excelFilePath)}] đang được mở, vui lòng đóng file", "File đang mở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			//ghi file excel sau khi chạy xong
			using var package = new ExcelPackage(new FileInfo(excelFilePath));
			var workSheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name.Equals(ExcelSheetName, StringComparison.OrdinalIgnoreCase));
			if (workSheet == null)
			{
				MessageBox.Show($"Không tìm thấy sheet có tên '{ExcelSheetName}'", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//ghi header
			workSheet.Cells[1, gridModels.First().ColIdx + 5].Value = "Kết quả";
			workSheet.Cells[1, gridModels.First().ColIdx + 6].Value = "Ảnh tham chiếu";
			workSheet.Cells[1, gridModels.First().ColIdx + 7].Value = "Cập nhật lúc";

			foreach (var model in gridModels)
			{
				if (model.IsSuccess && model.IsPendingSave)
				{
					model.IsPendingSave = false;
					// Update the worksheet with the success information
					workSheet.Cells[model.RowIdx, model.ColIdx + 5].Value = model.Result;

					workSheet.Cells[model.RowIdx, model.ColIdx + 6].SetHyperlink(new ExcelHyperLink(model.ScreenShot, UriKind.Relative));
					workSheet.Cells[model.RowIdx, model.ColIdx + 6].Value = Path.GetFileName(model.ScreenShot);

					workSheet.Cells[model.RowIdx, model.ColIdx + 7].Value = DateTime.Now;
					workSheet.Cells[model.RowIdx, model.ColIdx + 7].Style.Numberformat.Format = SystemConst.DateTimeFormat;
				}
			}
			await package.SaveAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex);
			MessageBox.Show($"Lỗi khi cập nhật file excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
