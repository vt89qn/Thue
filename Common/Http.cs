using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;

namespace Thue.Common;

public class Client : HttpClient
{
	public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36";
	private readonly SocketsHttpHandler handler;
	public Client(SocketsHttpHandler handler) : base(handler)
	{
		this.handler = handler;
		handler.ConnectTimeout = TimeSpan.FromSeconds(5);
		Timeout = TimeSpan.FromSeconds(10);
		handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
	}
	public Client() : this(new SocketsHttpHandler()) { }
	public SocketsHttpHandler Handler => handler;
	public string UserAgent { get; set; } = DefaultUserAgent;
	public void BindToIp6(IPAddress ip)
	{
		handler.ConnectCallback = async (context, token) =>
		{
			try
			{
				var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
				socket.Bind(new IPEndPoint(ip, 0));
				await socket.ConnectAsync(context.DnsEndPoint, token);
				return new NetworkStream(socket, ownsSocket: true);
			}
			catch (Exception)
			{
				throw;
			}
		};
	}
	public Request<HttpResponseMessage> Get([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri) => Get<HttpResponseMessage>(requestUri);
	public Request<TResult> Get<TResult>() => Get<TResult>("");
	public Request<TResult> Get<TResult>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri)
		=> Request<TResult>.Create(this, requestUri).Method(HttpMethod.Get).Ua(UserAgent);
	public Request<TResult> Post<TResult>() => Post<TResult>("");
	public Request<TResult> Post<TResult>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri)
		=> Request<TResult>.Create(this, requestUri).Method(HttpMethod.Post).Ua(UserAgent);

	public Request<TResult> Put<TResult>() => Put<TResult>("");
	public Request<TResult> Put<TResult>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri)
		=> Request<TResult>.Create(this, requestUri).Method(HttpMethod.Put).Ua(UserAgent);
}
public class Request<T>(HttpClient http)
{
	private HttpMethod method;
	private Uri requestUri;
	private Dictionary<string, string> headers;
	private HttpContent content;
	private NameValueCollection query;
	private string userAgent;
	private CancellationToken cancellationToken = default;
	private Action<Exception> onError;
	public static Request<T> Create(HttpClient http, [StringSyntax(StringSyntaxAttribute.Uri)] string stringUri)
	{
		return new Request<T>(http).Url(stringUri);
	}
	public TaskAwaiter<T> GetAwaiter()
	{
		return SendAsync().GetAwaiter();
	}

	public async Task<T> SendAsync(CancellationToken ct = default)
	{
		try
		{
			if (ct == default) ct = this.cancellationToken;
			var requestUri = this.requestUri;
			if (requestUri?.IsAbsoluteUri == false && http.BaseAddress != null)
			{
				requestUri = new Uri(http.BaseAddress, requestUri);
			}
			var builder = new UriBuilder(requestUri);
			if (query != null)
			{
				var query = HttpUtility.ParseQueryString(builder.Query);
				foreach (string key in this.query)
				{
					query[key] = this.query[key];
				}
				builder.Query = query.ToString();
			}
			var request = new HttpRequestMessage(method, builder.Uri);

			if (headers != null)
			{
				foreach (var header in headers)
				{
					if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
					{
						request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
					}
				}
			}
			if (!string.IsNullOrEmpty(userAgent) && request.Headers.UserAgent.Count == 0)
			{
				request.Headers.UserAgent.ParseAdd(userAgent);
			}

			if (content != null)
			{
				request.Content = content;
			}

			if (typeof(T) == typeof(HttpResponseMessage))
			{
				return (T)(object)(await http.SendAsync(request, ct));
			}

			using var response = await http.SendAsync(request, ct);
			if (response?.Content == null) return default;
			if (typeof(T) == typeof(string))
			{
				return (T)(object)await response.Content.ReadAsStringAsync(ct);
			}
			else if (typeof(T) == typeof(byte[]))
			{
				return (T)(object)await response.Content.ReadAsByteArrayAsync(ct);
			}
			return await response.Content.ReadFromJsonAsync<T>(ct);
		}
		catch (Exception ex)
		{
			onError?.Invoke(ex);
		}
		return default;
	}

	public Request<T> Method(HttpMethod method)
	{
		this.method = method;
		return this;
	}
	private Request<T> Url([StringSyntax(StringSyntaxAttribute.Uri)] string stringUri)
	{
		return Url(new Uri(stringUri, UriKind.RelativeOrAbsolute));
	}
	private Request<T> Url(Uri uri)
	{
		this.requestUri = uri;
		return this;
	}
	public Request<T> Header(Dictionary<string, string> headers)
	{
		this.headers = headers;
		return this;
	}
	/// <summary>
	/// HttpContent
	/// </summary>
	/// <typeparam name="TContent"></typeparam>
	/// <param name="content"></param>
	/// <returns></returns>
	public Request<T> Content<TContent>(TContent content) where TContent : HttpContent
	{
		this.content = content;
		return this;
	}
	public Request<T> Content<TContent>(TContent content, JsonSerializerOptions options = null)
	{
		this.content = JsonContent.Create(content, options: options);
		return this;
	}
	public Request<T> Content(string content, string contentType = "text/plain")
	{
		this.content = new StringContent(content, encoding: default, mediaType: contentType);
		return this;
	}
	public Request<T> Content(NameValueCollection content)
	{
		var dict = content.AllKeys.ToDictionary(k => k, k => content[k]);
		this.content = new FormUrlEncodedContent(dict);
		return this;
	}
	public Request<T> Query(NameValueCollection query)
	{
		this.query = query;
		return this;
	}
	public Request<T> Query(string query)
	{
		this.query = HttpUtility.ParseQueryString(query);
		return this;
	}
	public Request<T> Ua(string ua)
	{
		this.userAgent = ua;
		return this;
	}
	public Request<T> CancellationToken(CancellationToken cancellationToken)
	{
		this.cancellationToken = cancellationToken;
		return this;
	}
	public Request<T> OnError(Action<Exception> onError)
	{
		this.onError = onError;
		return this;
	}
}