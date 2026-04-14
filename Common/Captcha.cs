using System.Text.Json.Nodes;

namespace Thue.Common;

public class Captcha
{
	private static readonly Client http = new();
	public record AtrCaptchaResponse(string text);
	public record AtrCaptchaRequest(string body);

	public static async Task<string> OcrLocalAsync(string base64Image, CancellationToken ct)
	{
		var response = await http.Post<JsonObject>($"http://localhost:3123/captcha-ocr").Content(new { body = base64Image }).SendAsync(ct);
		return response?["text"]?.GetValue<string>();
	}
	public static async Task<string> OcrOnlineAsync(string base64Image, CancellationToken ct)
	{
		var response = await http.Post<JsonObject>($"https://atr.vt89qn.com/predict?app=thue").Content(new { body = base64Image }).SendAsync(ct);
		return response?["text"]?.GetValue<string>();
	}
}
