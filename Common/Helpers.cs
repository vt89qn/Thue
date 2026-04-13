using System.Drawing.Imaging;

namespace Thue.Common;

class Helpers
{
	public static bool IsDebug => System.Diagnostics.Debugger.IsAttached;
	public static byte[] FillWhiteAndConvertToJpg(byte[] imageBytes)
	{
		using var ms = new MemoryStream(imageBytes);
		using var original = new Bitmap(ms);

		using var newBitmap = new Bitmap(original.Width, original.Height);

		using (var g = Graphics.FromImage(newBitmap))
		{
			g.Clear(Color.White);
			g.DrawImage(original, 0, 0);
		}

		using var output = new MemoryStream();
		newBitmap.Save(output, ImageFormat.Jpeg);

		return output.ToArray();
	}
	public static bool IsFileLocked(string filePath)
	{
		if (!File.Exists(filePath)) return false;

		try
		{
			using FileStream stream = new(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			stream.Close();
		}
		catch (IOException)
		{
			return true;
		}
		return false;
	}
}