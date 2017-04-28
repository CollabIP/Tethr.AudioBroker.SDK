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
using Common.Logging;
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
    public class TethrSession : ITethrSession
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TethrSession));
        private readonly object _authLock = new object();
        private readonly Uri _hostUri;
        private readonly string _apiUser;
        private readonly SecureString _apiPassword;

        private TokenResponse _apiToken;

        public TethrSession(Uri hostUri, string apiUser, string apiPassword)
        {
            _hostUri = hostUri;
            _apiUser = apiUser;
            _apiPassword = ToSecureString(apiPassword);
        }

        public TethrSession(string connectionStringName = "Tethr")
        {
            var connectionString = TethrConnectionStringBuilder.Read(connectionStringName);
            _hostUri = new Uri(connectionString.Uri, UriKind.Absolute);
            _apiUser = connectionString.ApiUser;
            _apiPassword = ToSecureString(connectionString.Password);
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

        public IWebProxy Proxy { get; set; } = WebRequest.DefaultWebProxy;

        public void ClearAuthToken()
        {
            lock (_authLock)
            {
                _apiToken = null;
            }
        }

        public async Task<T> GetAsync<T>(string resourcePath)
        {
            LogConnection(_hostUri, resourcePath);
            using (var client = new HttpClient(CreateHttpClientHandler()) { BaseAddress = _hostUri })
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
                using (var message = await client.GetAsync(resourcePath))
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
        }

        public async Task<TOut> PostMutliPartAsync<TOut>(string resourcePath, object info, Stream buffer, string dataPartMediaType = "application/octet-stream")
        {
            LogConnection(_hostUri, resourcePath);
            using (var content = new MultipartFormDataContent(Guid.NewGuid().ToString()))
            {
                var infoContent = new StringContent(JsonConvert.SerializeObject(info), Encoding.UTF8, "application/json");
                var streamContent = new StreamContent(buffer);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(dataPartMediaType);
                content.Add(infoContent, "info");
                content.Add(streamContent, "data");
                using (var client = new HttpClient(CreateHttpClientHandler()))
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
                    using (var message = await client.PostAsync(new Uri(_hostUri, resourcePath), content))
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

                        return default(TOut);
                    }
                }
            }
        }

        public async Task<TOut> PostAsync<TOut>(string resourcePath, object body)
        {
            LogConnection(_hostUri, resourcePath);
            using (var client = new HttpClient(CreateHttpClientHandler()) { BaseAddress = _hostUri })
            {
                using (HttpContent r = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
                    using (var message = await client.PostAsync(resourcePath, r))
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
        }

        public async Task PostAsync(string resourcePath, object body)
        {
            LogConnection(_hostUri, resourcePath);
            using (var client = new HttpClient(CreateHttpClientHandler()) { BaseAddress = _hostUri })
            {
                using (HttpContent r = new StringContent(
                    body == null ? string.Empty : JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthToken());
                    using (var message = await client.PostAsync(resourcePath, r))
                    {
                        EnsureAuthorizedStatusCode(message);
                        message.EnsureSuccessStatusCode();
                    }
                }
            }
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
                        // we would need to pull in another 3rd party library, and we 
                        var t = GetClientCredentialsAsync(_hostUri, _apiUser, _apiPassword).GetAwaiter().GetResult();
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

        private async Task<TokenResponse> GetClientCredentialsAsync(Uri hostUri, string clientId, SecureString clientSecret)
        {
            using (var client = new HttpClient(CreateHttpClientHandler()))
            {
                Log.Info($"Requesting new token from {hostUri.Host}");

                using (HttpContent r = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                            {"grant_type", "client_credentials"},
                            {"client_secret", ToUnsecureString(clientSecret)},
                            {"client_id", clientId}
                    }))

                using (var response = await client.PostAsync(new Uri(hostUri, "/Token"), r))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException($"Server returned {response.StatusCode} to request to get Access Token.");

                    response.EnsureSuccessStatusCode();
                    var t = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

                    Log.Debug($"Token received, type: {t.TokenType}, expires in {t.ExpiresInSeconds} seconds");
                    if (t.TokenType != "bearer")
                    {
                        throw new InvalidOperationException("Can only support Bearer tokens");
                    }

                    t.CreatedTimeStamp = DateTime.Now;
                    return t;
                }
            }
        }

        private void LogConnection(Uri requestUri, string resourcePath)
        {
            if(!Log.IsDebugEnabled)
                return;

            var uri = new Uri(requestUri, resourcePath);
            var proxy = Proxy;
            var connectThrough = proxy.GetProxy(uri);
            var message = $"Making a request to {uri}";

            if ((connectThrough?.Equals(uri) ?? true) == false)
            {
                message += " through " + connectThrough;
            }

            Log.Debug(message);
        }

        private static string ToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

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
            foreach (char c in text)
            {
                secure.AppendChar(c);
            }

            return secure;
        }

        private HttpClientHandler CreateHttpClientHandler()
        {
            var httpHandler = new HttpClientHandler { UseCookies = false };
            var proxy = Proxy;
            if (proxy != null)
            {
                httpHandler.Proxy = proxy;
                httpHandler.UseProxy = true;
            }

            return httpHandler;
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
    }
}