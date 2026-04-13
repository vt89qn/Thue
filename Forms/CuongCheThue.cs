using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using OfficeOpenXml;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using Thue.Common;

namespace Thue.Forms;

public partial class CuongCheThue : Form
{

	private CancellationTokenSource runningCts;
	private Task runningTask;
	private bool isRunning = false;

	private List<CuongCheThueGridModel> gridModels = [];
	private IBrowserContext browserContext;

	private readonly ILogger logger;

	string excelFilePath = string.Empty;
	const string ExcelSheetName = "CUONG CHE THUE";
	const string ScreenshotFolderName = "screenshot_cct";

	public CuongCheThue(ILogger<CuongCheThue> logger)
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
			var mst = (workSheet.Cells[iRow, ++iCol].Value ?? "").ToString();
			if (string.IsNullOrWhiteSpace(mst)) break;
			var updateAtCellValue = workSheet.Cells[iRow, iCol + 13].Value;
			gridModels.Add(new CuongCheThueGridModel
			{
				RowIdx = iRow,
				ColIdx = iCol,
				Stt = stt++,
				Mst = mst,
				UpdateAt = updateAtCellValue is DateTime updateAt ? updateAt : updateAtCellValue is double oa ? DateTime.FromOADate(oa) : DateTime.MinValue,
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
				if (model.IsSuccess) continue;
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

	async Task RunningAsync(CuongCheThueGridModel model, int retryOnError, CancellationToken ct)
	{
		var http = new Client();
		var cookies = new CookieContainer();
		http.Handler.UseCookies = true;
		http.Handler.CookieContainer = cookies;

		for (int i = 1; i <= retryOnError; i++)
		{
			try
			{
				model.Info = $"{i} | Đang tải trang chủ...";

				var res = await http.Get<string>("https://web.gdt.gov.vn/wps/portal/Home/nt/cc");
				if (string.IsNullOrEmpty(res))
				{
					model.Info = $"{i} | Không tải được trang chủ";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}
				model.Info = $"{i} | Đang tải captcha...";

				var captchaUrl = res.Split("class=\"captchaImg\" src=\"")[1].Split('"')[0];
				if (string.IsNullOrEmpty(captchaUrl))
				{
					model.Info = $"{i} | Không lấy được link captcha";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}
				var captchaBytes = await http.GetByteArrayAsync($"https://web.gdt.gov.vn{captchaUrl}", ct);
				if (captchaBytes == null)
				{
					model.Info = $"{i} | Không tải được captcha";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}

				model.Info = $"{i} | Đang ocr captcha...";
				var processedBytes = Helpers.FillWhiteAndConvertToJpg(captchaBytes);
				var captchaText = await Captcha.OcrAsync(processedBytes.ToBase64(), ct);
				captchaText = Regex.Replace(captchaText ?? "", @"[^a-zA-Z0-9]", "");
				if (captchaText?.Length != 5)
				{
					model.Info = $"{i} | Ocr captcha không thành công";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}
				captchaText = captchaText.ToLower();

				var checkUrl = Regex.Match(res, @"document\.getElementById\('frm_dlt'\)\.action = '(.*?)';").Groups[1].Value;
				if (string.IsNullOrEmpty(checkUrl))
				{
					model.Info = $"{i} | Không lấy được link tra cứu";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}

				model.Info = $"{i} | Đợi 4 giây trước khi tra cứu...";
				await Task.Delay(TimeSpan.FromSeconds(4), ct);

				var queryResult = await http.Post<string>($"https://web.gdt.gov.vn{checkUrl}").Content(new NameValueCollection
				{
					["cmd"] = "search",
					["loaiQd"] = "1",
					["tin"] = model.Mst,
					["captcha"] = captchaText,
					["pageNumber"] = "1"
				});
				var htmlDecodeResult = WebUtility.HtmlDecode(queryResult ?? "");
				if (htmlDecodeResult.Contains("Mã xác nhận không hợp lệ!"))
				{
					model.Info = $"{i} | Sai mã xác nhận, thử lại sau 3 giây.";
					await Task.Delay(TimeSpan.FromSeconds(3), ct);
					continue;
				}
				if (htmlDecodeResult.Contains("Không tìm thấy kết quả phù hợp!"))
				{
					model.Ten = "Không tìm thấy kết quả phù hợp";
					model.IsSuccess = true;
					model.NeedScreenShot = false;
				}
				else if (htmlDecodeResult.Contains(@"table class=""ta_border"""))
				{
					var tableFromIdx = htmlDecodeResult.IndexOf(@"table class=""ta_border""");
					var tableToIdx = htmlDecodeResult.IndexOf("</table>", tableFromIdx);
					if (tableFromIdx == -1 || tableToIdx == -1)
					{
						model.Info = $"{i} | Đọc bảng thông tin tra cứu không thành công.";
						await Task.Delay(TimeSpan.FromSeconds(3), ct);
						continue;
					}
					var tableContent = htmlDecodeResult.Substring(tableFromIdx, tableToIdx - tableFromIdx + "</table>".Length);
					var regex = new Regex(@"<tr>.*?<td.*?>(?<stt>.*?)<\/td>.*?<td>.*?<b>(?<mst>.*?)<\/b>.*?<\/td>.*?<td>.*?<b>(?<ten>.*?)<\/b>.*?<\/td>.*?<td>(?<soqd>.*?)<\/td>.*?<td>.*?(?<ngay>.*?)<\/td>.*?<td>.*?(?<cqt>.*?)<\/td>.*?<td>.*?(?<sotien>.*?)<\/td>.*?<td>.*?(?<bienphap>.*?)<\/td>.*?<td>.*?(?<thongbao>.*?)<\/td>.*?<td>.*?(?<hieuluctu>.*?)<\/td>.*?<td>.*?(?<hieulucden>.*?)<\/td>.*?<td>.*?(?<thoigian>.*?)<\/td>.*?<td>.*?(?<trangthai>.*?)<\/td>", RegexOptions.Singleline);
					var matches = regex.Matches(tableContent);
					foreach (Match match in matches)
					{
						var mst = match.Groups["mst"].Value.Trim();
						if (mst.Equals(model.Mst, StringComparison.OrdinalIgnoreCase))
						{
							model.Ten = WebUtility.HtmlDecode(match.Groups["ten"].Value.Trim());
							model.SoQd = WebUtility.HtmlDecode(match.Groups["soqd"].Value.Trim());
							model.NgayQd = WebUtility.HtmlDecode(match.Groups["ngay"].Value.Trim());
							model.Cqt = WebUtility.HtmlDecode(match.Groups["cqt"].Value.Trim());
							model.SoTien = WebUtility.HtmlDecode(match.Groups["sotien"].Value.Trim());
							model.BienPhap = WebUtility.HtmlDecode(match.Groups["bienphap"].Value.Trim());
							model.ThongBao = WebUtility.HtmlDecode(match.Groups["thongbao"].Value.Trim());
							model.HieuLucTu = WebUtility.HtmlDecode(match.Groups["hieuluctu"].Value.Trim());
							model.HieuLucDen = WebUtility.HtmlDecode(match.Groups["hieulucden"].Value.Trim());
							model.ThoiGian = WebUtility.HtmlDecode(match.Groups["thoigian"].Value.Trim());
							model.TrangThai = WebUtility.HtmlDecode(match.Groups["trangthai"].Value.Trim());
							model.IsSuccess = true;
							model.NeedScreenShot = true;

							break;
						}
					}
				}

				if (model.IsSuccess)
				{
					if (model.NeedScreenShot)
					{
						model.Info = $"{i} | Đang chụp ảnh màn  hình...";
						var extractRes = await ExtractResult(captchaBytes, queryResult);
						var screenshotPath = Path.Combine(Path.GetDirectoryName(excelFilePath), ScreenshotFolderName);
						if (!Directory.Exists(screenshotPath))
						{
							Directory.CreateDirectory(screenshotPath);
						}
						var fileName = $"{model.Mst}_{DateTime.Now:yyyyMMddHHmmss}.png";
						File.WriteAllBytes(Path.Combine(screenshotPath, fileName), extractRes);

						model.ScreenShot = $"{ScreenshotFolderName}/{fileName}";
					}
					model.IsPendingSave = true;
					model.Info = $"Hoàn thành";
					return;
				}

				model.Info = $"{i} | Trường hợp ngoại lệ, vui lòng liên hệ hỗ trợ.";
				await Task.Delay(TimeSpan.FromSeconds(5), ct);
				continue;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				model.Info = $"{i} | Có lỗi xảy ra , vui lòng liên hệ hỗ trợ. {ex.Message}";
				logger.LogError(ex);
				await Task.Delay(TimeSpan.FromSeconds(3), ct);
			}
		}
	}

	private async Task<byte[]> ExtractResult(byte[] captchaBytes, string queryResult)
	{

		var page = await browserContext.NewPageAsync();
		await page.RouteAsync("**/wps/portal/Home/nt/cc", async route =>
			{
				if (route.Request.Method == "GET")
				{
					await route.FulfillAsync(new()
					{
						Status = 200,
						ContentType = "text/html; charset=utf-8",
						Body = queryResult
					});
				}
			});
		await page.RouteAsync("**/wps/PA_CKTT/captcha.png?*", async route =>
		{
			if (route.Request.Method == "GET")
			{
				await route.FulfillAsync(new()
				{
					Status = 200,
					ContentType = "image/png",
					BodyBytes = captchaBytes
				});
			}
		});
		await page.GotoAsync("https://web.gdt.gov.vn/wps/portal/Home/nt/cc", new() { Timeout = 5000 });
		byte[] screenshot = await page.Locator("#layoutContainers").ScreenshotAsync(new() { Type = ScreenshotType.Png });
		await page.CloseAsync();
		return screenshot;
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
			var iCol = 0;
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Tên NTT";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Số QĐ cưỡng chế";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Ngày ban hành QĐ cưỡng chế";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "CQT";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Số tiền cưỡng chế(VND)";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Biện pháp cưỡng chế";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Thông báo ngừng sử dụng hóa đơn";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Hiệu lực từ";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Hiệu lực đến";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Thời gian thực hiện cưỡng chế";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Trạng thái QĐCC";

			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Ảnh tham chiếu";
			workSheet.Cells[1, gridModels.First().ColIdx + ++iCol].Value = "Cập nhật lúc";
			foreach (var model in gridModels)
			{
				if (model.IsSuccess && model.IsPendingSave)
				{
					iCol = 0;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.Ten;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.SoQd;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.NgayQd;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.Cqt;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = (model.SoTien ?? "").Replace(".", "");
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.BienPhap;

					//workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.ThongBao;
					++iCol;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.HieuLucTu;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.HieuLucDen;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.ThoiGian;
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = model.TrangThai;

					if (!string.IsNullOrEmpty(model.ScreenShot))
					{
						workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].SetHyperlink(new ExcelHyperLink(model.ScreenShot, UriKind.Relative));
						workSheet.Cells[model.RowIdx, model.ColIdx + iCol].Value = Path.GetFileName(model.ScreenShot);
					}
					else
					{
						iCol++;
					}
					workSheet.Cells[model.RowIdx, model.ColIdx + ++iCol].Value = DateTime.Now;
					workSheet.Cells[model.RowIdx, model.ColIdx + +iCol].Style.Numberformat.Format = SystemConst.DateTimeFormat;
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
