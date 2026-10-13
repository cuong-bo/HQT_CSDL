using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    /// <summary>What came back from the server. <see cref="StatusCode"/> is 0 when the server could not be reached at all.</summary>
    public sealed class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }
        public object Json { get; set; }
        public string Error { get; set; }

        public bool IsNetworkError => StatusCode == 0;

        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        public IDictionary<string, object> Object => Json as IDictionary<string, object>;
    }

    /// <summary>The server refused a request or could not be reached; the message is fit to show to the user.</summary>
    public sealed class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }

    /// <summary>
    /// HTTP + JSON client for the Hue Travel API. Sends the access token, and when the server answers 401 it
    /// trades the refresh token for a new pair once and repeats the request.
    /// </summary>
    public sealed class ApiClient : IDisposable
    {
        public const string Unreachable = "Không kết nối được máy chủ. Hãy kiểm tra mạng hoặc dùng chế độ ngoại tuyến.";

        static ApiClient()
        {
            // .NET Framework allows only 2 connections per host by default: weather, area, routes and lists would queue behind each other.
            System.Net.ServicePointManager.DefaultConnectionLimit = 8;
            System.Net.ServicePointManager.Expect100Continue = false;
        }

        private readonly HttpClient http;
        private readonly string baseUrl;
        private readonly TokenStore store;
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);
        private bool persistSession;

        private readonly ResponseVerifier verifier;

        /// <param name="verifier">When given, every /v1 reply must carry a valid server signature (see <see cref="ResponseVerifier"/>).</param>
        public ApiClient(string baseUrl, TokenStore store, HttpMessageHandler handler = null, ResponseVerifier verifier = null)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.store = store;
            this.verifier = verifier;
            http = handler == null ? new HttpClient() : new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(15);
        }

        public string AccessToken { get; private set; }

        public string RefreshToken { get; private set; }

        /// <summary>Raised when the refresh token no longer works: the person has to sign in again.</summary>
        public event EventHandler SessionExpired;

        public void SetTokens(string access, string refresh, bool persist)
        {
            AccessToken = access;
            RefreshToken = refresh;
            persistSession = persist;
            if (store == null) return;
            if (persist) store.Save(access, refresh);
            else store.Delete();
        }

        public void ClearTokens()
        {
            AccessToken = RefreshToken = null;
            store?.Delete();
        }

        public Task<ApiResponse> GetAsync(string path, CancellationToken ct = default(CancellationToken)) => SendAsync(HttpMethod.Get, path, null, ct);

        public Task<ApiResponse> PostAsync(string path, object body, CancellationToken ct = default(CancellationToken)) => SendAsync(HttpMethod.Post, path, body, ct);

        public Task<ApiResponse> PutAsync(string path, object body, CancellationToken ct = default(CancellationToken)) => SendAsync(HttpMethod.Put, path, body, ct);

        public Task<ApiResponse> DeleteAsync(string path, CancellationToken ct = default(CancellationToken)) => SendAsync(HttpMethod.Delete, path, null, ct);

        public async Task<ApiResponse> SendAsync(HttpMethod method, string path, object body, CancellationToken ct = default(CancellationToken))
        {
            ApiResponse response = await SendOnceAsync(method, path, body, ct).ConfigureAwait(true);
            bool canRefresh = response.StatusCode == 401 && !string.IsNullOrEmpty(RefreshToken)
                              && !path.StartsWith("/v1/auth/", StringComparison.Ordinal);
            if (!canRefresh) return response;
            if (!await TryRefreshAsync(ct).ConfigureAwait(true)) return response;
            return await SendOnceAsync(method, path, body, ct).ConfigureAwait(true);
        }

        private async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            string used = RefreshToken;
            await refreshLock.WaitAsync(ct).ConfigureAwait(true);
            try
            {
                if (RefreshToken != used) return !string.IsNullOrEmpty(AccessToken);   // another request already refreshed
                ApiResponse r = await SendOnceAsync(HttpMethod.Post, "/v1/auth/refresh", new Dictionary<string, object> { { "refreshToken", used } }, ct).ConfigureAwait(true);
                string access = r.IsSuccess ? JsonHelper.Str(r.Object, "accessToken") : null;
                string refresh = r.IsSuccess ? JsonHelper.Str(r.Object, "refreshToken") : null;
                if (access != null && refresh != null)
                {
                    SetTokens(access, refresh, persistSession);
                    return true;
                }
                // Only a definite refusal ends the session. A busy or restarting server (429, 5xx) or a lost network keeps the tokens.
                if (r.StatusCode == 400 || r.StatusCode == 401)
                {
                    ClearTokens();
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                }
                return false;
            }
            finally
            {
                refreshLock.Release();
            }
        }

        private async Task<ApiResponse> SendOnceAsync(HttpMethod method, string path, object body, CancellationToken ct)
        {
            using (var request = new HttpRequestMessage(method, baseUrl + path))
            {
                if (body != null) request.Content = new StringContent(JsonHelper.Serialize(body), Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(AccessToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                try
                {
                    using (HttpResponseMessage reply = await http.SendAsync(request, ct).ConfigureAwait(true))
                    {
                        byte[] bytes = await reply.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
                        string problem;
                        if (!IsGenuine(method, request.RequestUri, reply, bytes, out problem))
                            return new ApiResponse { StatusCode = 0, Error = problem };   // treated like "server not reachable": nothing from it is used
                        string text = Encoding.UTF8.GetString(bytes);
                        object json = JsonHelper.Parse(text);
                        var response = new ApiResponse { StatusCode = (int)reply.StatusCode, Body = text, Json = json };
                        if (!response.IsSuccess) response.Error = ErrorText(json, response.StatusCode);
                        return response;
                    }
                }
                catch (HttpRequestException)
                {
                    return new ApiResponse { StatusCode = 0, Error = Unreachable };
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    return new ApiResponse { StatusCode = 0, Error = Unreachable };   // the 15 second timeout
                }
            }
        }

        private bool IsGenuine(HttpMethod method, Uri uri, HttpResponseMessage reply, byte[] body, out string problem)
        {
            problem = null;
            string path = ResponseVerifier.SignedPath(uri);
            if (verifier == null || !path.StartsWith("/v1/", StringComparison.Ordinal)) return true;
            return verifier.Verify(method.Method, path, HeaderValue(reply, "X-Sig-Time"), HeaderValue(reply, "X-Sig"), body, DateTimeOffset.UtcNow, out problem);
        }

        private static string HeaderValue(HttpResponseMessage reply, string name)
        {
            IEnumerable<string> values;
            if (!reply.Headers.TryGetValues(name, out values)) return null;
            foreach (string value in values) return value;
            return null;
        }

        /// <summary>The human text of a ProblemDetails reply ("detail", else "title"), or a generic message.</summary>
        public static string ErrorText(object json, int status)
        {
            var d = json as IDictionary<string, object>;
            string detail = JsonHelper.Str(d, "detail") ?? JsonHelper.Str(d, "title");
            if (!string.IsNullOrWhiteSpace(detail)) return detail;
            if (status == 401) return "Bạn cần đăng nhập.";
            if (status == 403) return "Bạn không có quyền thực hiện việc này.";
            if (status == 429) return "Thao tác quá nhanh, hãy thử lại sau ít phút.";
            return "Máy chủ báo lỗi (" + status + ").";
        }

        public void Dispose()
        {
            http.Dispose();
            refreshLock.Dispose();
        }
    }
}
