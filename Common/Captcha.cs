namespace Thue.Common;

public class Captcha
{
	private static readonly Client http = new();
	public record AtrCaptchaResponse(string text);
	public record AtrCaptchaRequest(string body);

	public static async Task<string> OcrAsync(string base64Image, CancellationToken ct)
	{
		var response = await http.Post<AtrCaptchaResponse>($"http://localhost:3123/captcha-ocr").Content<AtrCaptchaRequest>(new(body: base64Image)).SendAsync(ct);
		return response?.text;
	}
}
