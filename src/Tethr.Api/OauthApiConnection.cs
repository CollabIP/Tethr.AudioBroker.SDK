using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Tethr.Api
{
    public interface IOauthApiConnection
    {
        void Initialize(Uri hostUri, string apiUser, string apiPassword, string webProxy);
        Task<T> GetAsync<T>(string requestUri);
        Task<TOut> PostMutliPartAsync<TOut>(string requestUri, Stream audioStream, object info) where TOut : new();
        Task<TOut> PostAsync<TOut>(string requestUri, object content);
        Task PostAsync(string requestUri, object content);
    }

    public class OauthApiConnection : IOauthApiConnection
    {
        private TokenResponse _apiToken;
        private readonly object _authLock = new object();
        private Uri _hostUri;
        private string _apiUser;
        private string _apiPassword;

        public void Initialize(Uri hostUri, string apiUser, string apiPassword, string webProxy)
        {
            _hostUri = hostUri;
            _apiUser = apiUser;
            _apiPassword = apiPassword;

            if (string.IsNullOrEmpty(webProxy)) return;

            var proxy = new WebProxy(webProxy);
            WebRequest.DefaultWebProxy = proxy;
        }

        public async Task<T> GetAsync<T>(string requestUri)
        {
            using (var client = new HttpClient { BaseAddress = _hostUri })
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthTokenAsync());
                using (var message = await client.GetAsync(requestUri))
                {
                    if (message.StatusCode == HttpStatusCode.Unauthorized || message.StatusCode == HttpStatusCode.Forbidden)
                        throw new AuthenticationException();
                    if (message.StatusCode == HttpStatusCode.NotFound)
                        return default(T);

                    message.EnsureSuccessStatusCode();
                    if (message.Content.Headers.ContentType != null && message.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var s = await message.Content.ReadAsStreamAsync())
                        {
                            return s.JsonDeserialize<T>();
                        }
                    }

                    throw new ApplicationException($"Unexpected content type ({message.Content.Headers.ContentType}) returned from server.");
                }
            }
        }

        public async Task<TOut> PostMutliPartAsync<TOut>(string requestUri, Stream binaryStream, object info) where TOut : new()
        {
            using (var content = new MultipartFormDataContent(Guid.NewGuid().ToString()))
            {
                var infoContent = new StringContent(JsonConvert.SerializeObject(info), Encoding.UTF8, "application/json");
                var streamContent = new StreamContent(binaryStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(infoContent, "info");
                content.Add(streamContent, "data");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthTokenAsync());
                    // ReSharper disable once AccessToDisposedClosure
                    using (var message = await client.PostAsync(new Uri(_hostUri, requestUri), content))
                    {
                        if (message.StatusCode == HttpStatusCode.Unauthorized)
                            throw new AuthenticationException();

                        message.EnsureSuccessStatusCode();
                        if (message.Content.Headers.ContentType != null && message.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var s = await message.Content.ReadAsStreamAsync())
                            {
                                return s.JsonDeserialize<TOut>();
                            }
                        }

                        return new TOut();
                    }
                }
            }
        }

        public async Task<TOut> PostAsync<TOut>(string requestUri, object content)
        {
            using (var client = new HttpClient { BaseAddress = _hostUri })
            {
                using (HttpContent r = new StringContent(
                    JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthTokenAsync());
                    using (var message = await client.PostAsync(requestUri, r))
                    {
                        if (message.StatusCode == HttpStatusCode.Unauthorized || message.StatusCode == HttpStatusCode.Forbidden)
                            throw new AuthenticationException();
                        message.EnsureSuccessStatusCode();
                        if (message.Content.Headers.ContentType != null && message.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var s = await message.Content.ReadAsStreamAsync())
                            {
                                return s.JsonDeserialize<TOut>();
                            }
                        }

                        throw new ApplicationException($"Unexpected content type ({message.Content.Headers.ContentType}) returned from server.");
                    }
                }
            }
        }

        public async Task PostAsync(string requestUri, object content)
        {
            using (var client = new HttpClient { BaseAddress = _hostUri })
            {
                using (HttpContent r = new StringContent(
                    content == null ? string.Empty : JsonConvert.SerializeObject(content),
                    Encoding.UTF8,
                    "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetApiAuthTokenAsync());
                    using (var message = await client.PostAsync(requestUri, r))
                    {
                        if (message.StatusCode == HttpStatusCode.Unauthorized || message.StatusCode == HttpStatusCode.Forbidden)
                            throw new AuthenticationException();
                        message.EnsureSuccessStatusCode();
                    }
                }
            }
        }

        public string GetApiAuthTokenAsync(bool force = false)
        {
            if (force || _apiToken?.IsValid != true)
            {
                lock (_authLock)
                {
                    if (force || _apiToken?.IsValid != true)
                    {
                        Console.WriteLine("Getting Token from " + _hostUri.ToString());
                        var t = GetClientCredentialsAsync(_hostUri, _apiUser, _apiPassword).GetAwaiter().GetResult();
                        _apiToken = t;
                    }
                }
            }

            return _apiToken.GetToken();
        }

        private async Task<TokenResponse> GetClientCredentialsAsync(Uri hostUri, string clientId, string clientSecret)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (HttpContent r = new FormUrlEncodedContent(
                        new Dictionary<string, string>
                        {
                            {"grant_type", "client_credentials"},
                            {"client_secret", clientSecret},
                            {"client_id", clientId}
                        }))

                    using (var response = await client.PostAsync(new Uri(hostUri, "/Token"), r))
                    {
                        response.EnsureSuccessStatusCode();
                        var t = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());
                        if (t.TokenType != "bearer")
                        {
                            throw new InvalidOperationException("Can only support Bearer tokens");
                        }

                        t.CreatedTimeStamp = DateTime.Now;
                        return t;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AuthenticationException($"Unable to get Token for {clientId} from {hostUri.Host}, {ex.Message}", ex);
            }
        }

        public class TokenResponse
        {
            private byte[] _accessToken;

            [JsonProperty("access_token", Required = Required.Always)]
            public string AccessToken
            {
                get { return null; }
                set { _accessToken = string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value); }
            }

            public string GetToken()
            {
                return _accessToken == null ? null : Encoding.UTF8.GetString(_accessToken);
            }

            [JsonProperty("token_type", Required = Required.Always)]
            public string TokenType;

            [JsonProperty("expires_in", Required = Required.Always)]
            // ReSharper disable once InconsistentNaming
            public long ExpiresInSeconds;

            public DateTime CreatedTimeStamp { get; set; }

            [JsonIgnore]
            public bool IsValid => CreatedTimeStamp + TimeSpan.FromSeconds(ExpiresInSeconds - 45) > DateTime.Now;
        }
    }
}