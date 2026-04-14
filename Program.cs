using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using System.Collections.Specialized;
using System.Reflection;
using System.Web;
using Thue.Common;
using Thue.Forms;

static class Program
{
	public static Dictionary<string, string> ConfigDlls { get; set; }
	public static NameValueCollection Params { get; set; }
	static string PlaywrightPath;

	[STAThread]
	static void Main(string[] args)
	{
		try
		{
			var startParams = GetParameters(args);
			ExcelPackage.License.SetNonCommercialPersonal("tchd");
			if (Helpers.IsDebug)
			{
				SystemConst.ROOT_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Thue");
			}
			else
			{
				SystemConst.ROOT_PATH = AppDomain.CurrentDomain.BaseDirectory;
			}

			if (ConfigDlls == null) AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

			if (Helpers.IsDebug)
			{
				startParams["ac"] = "cct";
			}

			var services = new ServiceCollection();
			services.AddLog(SystemConst.ROOT_PATH);
			services.AddSingleton<TraCuuHoaDon>().AddSingleton<TrangThaiMst>().AddSingleton<CuongCheThue>();

			ApplicationConfiguration.Initialize();

			string act = (startParams["ac"] ?? string.Empty).ToLower();

			Type formType = null;
			if (act == "tthd" || act == "tracuuhoadon")
			{
				formType = typeof(TraCuuHoaDon);
			}
			else if (act == "ttmst" || act == "trangthaimst")
			{
				formType = typeof(TrangThaiMst);
			}
			else if (act == "cct" || act == "cuongchethue")
			{
				formType = typeof(CuongCheThue);
			}
			if (formType != null)
			{
				var serviceProvider = services.BuildServiceProvider();
				Application.Run((Form)serviceProvider.GetRequiredService(formType));
			}
			else
			{
				MessageBox.Show("hello");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}
	public static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
	{
		string dll = $"{new AssemblyName(args.Name).Name}.dll";
		if (ConfigDlls?.TryGetValue(dll, out var path) == true)
		{
			return Assembly.LoadFrom(path);
		}
		if (dll == "Microsoft.Playwright.dll")
		{
			return Assembly.LoadFrom(Path.Combine(SystemConst.ROOT_PATH, "Playwright", dll));
		}
		return null;
	}
	private static NameValueCollection GetParameters(string[] args)
	{
		NameValueCollection nv = [];
		if (Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed")?.ToLower() == "true")
		{
			string p = Environment.GetEnvironmentVariable("ClickOnce_ActivationUri");
			if (!string.IsNullOrEmpty(p))
			{
				var activationUri = string.IsNullOrEmpty(p) ? null : new Uri(p);
				nv = HttpUtility.ParseQueryString(activationUri.Query);
			}
		}
		else if (args?.Length > 1)
		{
			var p = args.SkipWhile(x => x != "-p").Skip(1).FirstOrDefault();
			nv = HttpUtility.ParseQueryString(p ?? "");
		}
		return nv;
	}
}




