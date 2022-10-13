using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Tethr.AudioBroker.Session
{
	/// <summary>
	/// Manages the connection to the Tethr server and authentication session.
	/// </summary>
	/// <remarks>
	/// Exposed the Raw action for calling the Tethr server, and maintains the authentication token.
	/// This object is thread safe, and should be reused and often is a singleton kept for the lifecycle of the application.
	/// </remarks>
	public class TethrSession : ITethrSession, IDisposable
	{
		private readonly ILogger<TethrSession> _log = new NullLogger<TethrSession>();
		private static ProductInfoHeaderValue _productInfoHeaderValue;
		private readonly object _authLock = new object();
		private readonly string _apiUser;
		private readonly SecureString _apiPassword;
		private readonly HttpClient _client;
		private TokenResponse _apiToken;

		public TethrSession(Uri hostUri, string apiUser, string apiPassword, ILogger<TethrSession> logger = null) :
			this(hostUri, apiUser, ToSecureString(apiPassword), logger)
		{ }

		public TethrSession(Uri hostUri, string apiUser, SecureString apiPassword, ILogger<TethrSession> logger = null)
		{
			if (logger != null) _log = logger;
			
			if (!hostUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				_log.LogWarning("Not using HTTPS for connection to server");
			}

			_apiUser = apiUser;
			_apiPassword = apiPassword;
			_client = CreateHttpClient(hostUri);
		}

		public TethrSession(TethrSessionOptions options, ILogger<TethrSession> logger = null)
		{
			if (logger != null) _log = logger;
			
			var hostUri = new Uri(options.Uri, UriKind.Absolute);

			if (!hostUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				_log.LogWarning("Not using HTTPS for connection to server");
			}

			_apiUser = options.ApiUser;
			_apiPassword = ToSecureString(options.Password);
			_client = CreateHttpClient(hostUri);
		}

		/// <summary>
		/// When True, if Tethr returns a 401 (Unauthorized), 
		/// automatically reset the OAuth Token and request a new one with the next request
		/// </summary>
		/// <remarks>
		/// Default value is True.
		/// 
		/// Typically the only cause for a 401 is if the Token has been revoked.
		/// By having this true, it would allow a client to retry the request and probably
		/// be successful.
		/// 
		/// If you want more control over how the reset is handled, you can set this to false.
		/// </remarks>
		public bool ResetAuthTokenOnUnauthorized { get; set; } = true;

		public static IWebProxy DefaultProxy { get; set; } = WebRequest.DefaultWebProxy;

		/// <summary>
		/// Add data used to in the HTTP User-Agent Header for requests to Tethr.
		/// </summary>
		/// <param name="product">The name of the product</param>
		/// <param name="version">The version number of the product</param>
		public static void SetProductInfoHeaderValue(string product, string version)
		{
			_productInfoHeaderValue = new ProductInfoHeaderValue(product, version);
		} 

		public void ClearAuthToken()
		{
			lock (_authLock)
			{
				_apiToken = null;
			}
		}

		public async Task<T> GetAsync<T>(string resourcePath)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, CreateUri(resourcePath));
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
			using (var message = await _client.SendAsync(request))
			{
				EnsureAuthorizedStatusCode(message);
				if (message.StatusCode == HttpStatusCode.NotFound)
					throw new KeyNotFoundException("The requested resource was not found in Tethr");

				message.EnsureSuccessStatusCode();
				if (message.Content.Headers.ContentType != null && message.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
				{
					using (var s = await message.Content.ReadAsStreamAsync())
					{
						return s.JsonDeserialize<T>();
					}
				}

				throw new InvalidOperationException($"Unexpected content type ({message.Content.Headers.ContentType}) returned from server.");
			}
		}

		public async Task<TOut> PostMultiPartAsync<TOut>(string resourcePath, object info, Stream buffer,
			string dataPartMediaType = "application/octet-stream")
		{
			return await PostMultiPartAsync<TOut>(resourcePath, JsonConvert.SerializeObject(info), buffer, dataPartMediaType);
		}

		public async Task<TOut> PostMultiPartAsync<TOut>(string resourcePath, string info, Stream buffer, string dataPartMediaType = "application/octet-stream")
		{
			using (var content = new MultipartFormDataContent(Guid.NewGuid().ToString()))
			{
				var infoContent = new StringContent(info, Encoding.UTF8, "application/json");
				var streamContent = new StreamContent(buffer);
				streamContent.Headers.ContentType = new MediaTypeHeaderValue(dataPartMediaType);
				content.Add(infoContent, "info");
				content.Add(streamContent, "data");

				var request = new HttpRequestMessage(HttpMethod.Post, CreateUri(resourcePath)) { Content = content };
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
				using (var message = await _client.SendAsync(request))
				{
					EnsureAuthorizedStatusCode(message);
					message.EnsureSuccessStatusCode();
					if (message.Content.Headers.ContentType != null && message.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
					{
						using (var s = await message.Content.ReadAsStreamAsync())
						{
							return s.JsonDeserialize<TOut>();
						}
					}

					return default;
				}
			}
		}

		public async Task<TOut> PostAsync<TOut>(string resourcePath, object body)
		{
			using (HttpContent content = new StringContent(
				JsonConvert.SerializeObject(body),
				Encoding.UTF8,
				"application/json"))
			{
				var request = new HttpRequestMessage(HttpMethod.Post, CreateUri(resourcePath)) { Content = content };
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
				using (var message = await _client.SendAsync(request))
				{
					EnsureAuthorizedStatusCode(message);
					message.EnsureSuccessStatusCode();
					if (message.Content.Headers.ContentType != null && message.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
					{
						using (var s = await message.Content.ReadAsStreamAsync())
						{
							return s.JsonDeserialize<TOut>();
						}
					}

					throw new InvalidOperationException($"Unexpected content type ({message.Content.Headers.ContentType}) returned from server.");
				}
			}
		}

		public async Task PostAsync(string resourcePath, object body)
		{
			using (HttpContent content = new StringContent(
				body == null ? string.Empty : JsonConvert.SerializeObject(body),
				Encoding.UTF8,
				"application/json"))
			{
				var request = new HttpRequestMessage(HttpMethod.Post, CreateUri(resourcePath)) { Content = content };
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
				using (var message = await _client.SendAsync(request))
				{
					EnsureAuthorizedStatusCode(message);
					message.EnsureSuccessStatusCode();
				}
			}
		}

		private Uri CreateUri(string uri)
		{
			if (string.IsNullOrEmpty(uri))
				return null;
			var u = new Uri(uri, UriKind.Relative);
			_log.LogDebug("Making a request to {U}", u);
			return u;
		}

		private string GetApiAuthToken(bool force = false)
		{
			if (force || _apiToken?.IsValid != true)
			{
				lock (_authLock)
				{
					if (force || _apiToken?.IsValid != true)
					{
						// Would like to have this called via Async, but because we are in a lock
						// we would need to pull in another 3rd party library.
						var t = GetClientCredentialsAsync(_apiUser, _apiPassword).ConfigureAwait(false).GetAwaiter().GetResult();
						_apiToken = t;
					}
				}
			}

			return _apiToken.AccessToken;
		}

		private void EnsureAuthorizedStatusCode(HttpResponseMessage message)
		{
			switch (message.StatusCode)
			{
				case HttpStatusCode.Unauthorized:
					{
						if (ResetAuthTokenOnUnauthorized)
							_apiToken = null;
						throw new AuthenticationException("Request returned 401 (Unauthorized)");
					}
				case HttpStatusCode.Forbidden:
					throw new UnauthorizedAccessException("Request returned 403 (Forbidden)");
			}
		}

		private async Task<TokenResponse> GetClientCredentialsAsync(string clientId, SecureString clientSecret)
		{
			_log.LogInformation("Requesting new token from {ClientBaseAddress}", _client.BaseAddress);

			using (HttpContent r = new FormUrlEncodedContent(
				new Dictionary<string, string>
				{
					{"grant_type", "client_credentials"},
					{"client_secret", ToUnsecureString(clientSecret)},
					{"client_id", clientId}
				}))
			using (var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, CreateUri("/Token")) { Content = r }))
			{
				if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
					throw new UnauthorizedAccessException($"Server returned {response.StatusCode} to request to get Access Token.");

				response.EnsureSuccessStatusCode();
				var t = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

				_log.LogDebug("Token received, type: {TTokenType}, expires in {TExpiresInSeconds} seconds", t?.TokenType, t?.ExpiresInSeconds);
				if (t?.TokenType != "bearer")
				{
					throw new InvalidOperationException("Can only support Bearer tokens");
				}

				t.CreatedTimeStamp = DateTime.Now;
				return t;
			}
		}

		private static string ToUnsecureString(SecureString securePassword)
		{
			if (securePassword == null)
				throw new ArgumentNullException(nameof(securePassword));

			IntPtr unmanagedString = IntPtr.Zero;
			try
			{
				unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
				return Marshal.PtrToStringUni(unmanagedString);
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
			}
		}

		private static SecureString ToSecureString(string text)
		{
			var secure = new SecureString();
			foreach (var c in text)
			{
				secure.AppendChar(c);
			}

			return secure;
		}

		private HttpClient CreateHttpClient(Uri hostUri)
		{
			var version = typeof(TethrSession).Assembly.GetName().Version;
			var message = $"Requests for Tethr to {hostUri} using SDK version {version}";

			var httpHandler = new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false };
			var proxy = DefaultProxy;
			if (proxy != null)
			{
				try
				{
					message += $" through {proxy.GetProxy(hostUri)}";
				}
				catch (PlatformNotSupportedException)
				{
					_log.LogWarning("Not able to get proxy from {Proxy}", proxy);
				}
				
				httpHandler.Proxy = proxy;
				httpHandler.UseProxy = true;
			}

			_log.LogInformation(message);
			var client = new HttpClient(httpHandler, true)
			{
				BaseAddress = hostUri,
				Timeout = TimeSpan.FromMinutes(5),
				DefaultRequestHeaders = { UserAgent =
				{
					new ProductInfoHeaderValue("TethrAudioBroker", version.ToString()),
					new ProductInfoHeaderValue($"({Environment.OSVersion})"),
					new ProductInfoHeaderValue("DotNet-CLR", Environment.Version.ToString())
				} }
			}; 

			if(_productInfoHeaderValue != null)
				client.DefaultRequestHeaders.UserAgent.Add(_productInfoHeaderValue);

			return client;
		}

		internal class TokenResponse
		{
#pragma warning disable 0649 // They are set by Json.net, compiler doesn't think that can happen because it's internal.

			// leaving access token as a string as there is no security gained from anything else and only 
			// slows down the calls.  In normal use cases this is used often for the lifetime of the Token, 
			// meaning that string is in clear text the entire time it's valid anyway.
			[JsonProperty("access_token", Required = Required.Always)]
			public string AccessToken;

			[JsonProperty("token_type", Required = Required.Always)]
			public string TokenType;

			[JsonProperty("expires_in", Required = Required.Always)]
			public long ExpiresInSeconds;
#pragma warning restore 0649

			public DateTime CreatedTimeStamp { get; set; }

			[JsonIgnore]
			public bool IsValid => CreatedTimeStamp + TimeSpan.FromSeconds(ExpiresInSeconds - 45) > DateTime.Now;
		}

		public void Dispose()
		{
			_apiPassword?.Dispose();
			_client?.Dispose();
		}
	}
}