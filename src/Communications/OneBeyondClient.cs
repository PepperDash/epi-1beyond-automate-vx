using System;
using System.Net.Http;
using OneBeyondAutomateVxEpi.ApiObjects;
using OneBeyondAutomateVxEpi.GenericClients;
using PepperDash.Core;
using PepperDash.Core.Logging;

namespace OneBeyondAutomateVxEpi.Communications
{


    public class OneBeyondClient : IKeyed
    {
        private static readonly string _separator = new String('-', 50);

        private readonly HttpClient _httpClient = new HttpClient();

        public string Key { get; private set; }

        private string userPassAuth;
        public string Token { get; private set; }

        public OneBeyondClient(string key, ControlPropertiesConfig controlConfig)
        {
            Key = key;

            var port = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
                ? controlConfig.TcpSshProperties.Port
                : 3579;

            var prefix = controlConfig.Method == eControlMethod.Https ? "https://" : "http://";

            var address = controlConfig.TcpSshProperties.Address.Replace("http://", "").Replace("https://", "");

            var baseAddress = String.Format("{0}{1}:{2}",
                    prefix,
                    address,
                    port);

            _httpClient.BaseAddress = new Uri(baseAddress);
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var username = controlConfig.TcpSshProperties.Username ?? "";
            var password = controlConfig.TcpSshProperties.Password ?? "";

            userPassAuth = AuthenticationHelpers.EncodeBase64(key, username, password);
            Token = string.Empty;

            this.LogVerbose(@"
{0}
>>>>> GenericClientHttps: 
Key = {1}
Host = {2}
Port = {3}
Username = {4}
Password = {5}
AuthBase64 = {6}
AuthToken = {7}
{0}", _separator, Key, baseAddress, port, username, password, userPassAuth, Token);
        }

        /// <summary>
        /// Sends the HTTP request and parses the response
        /// </summary>
        /// <typeparam name="T">Type of the response object</typeparam>
        /// <param name="method">HTTP method to use for the request</param>
        /// <param name="uri">The URI for the request</param>
        /// <param name="data">serialized data to send</param>
        /// <returns></returns>
        public T SendRequest<T>(HttpMethod method, string uri, string data = "", string authorization = null)
        {
            try
            {
                // Remove existing Authorization header if present
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                
                // Set Authorization header without scheme (API expects raw encoded value)
                var authValue = !string.IsNullOrEmpty(authorization) ? authorization : userPassAuth;
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authValue);

                // Build the full URL string - HttpRequestMessage accepts a string URI
                var fullUrl = string.Format("{0}/{1}", _httpClient.BaseAddress, uri.TrimStart('/'));
                using (var request = new HttpRequestMessage(method, fullUrl))
                {
                    if (method == HttpMethod.Post || method == HttpMethod.Put)
                    {
                        request.Content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
                    }

                    this.LogVerbose(@"
{0}
>>>>> SendRequest
url: {1}{2}
content: {3}
requestType: {4}
authHeaderValue: {5}
{0}", _separator, _httpClient.BaseAddress, uri, request.Content?.ReadAsStringAsync().Result, request.Method, _httpClient.DefaultRequestHeaders.Authorization);

                    using (var response = _httpClient.SendAsync(request).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            this.LogDebug("Unable to get sources for {Host}: {responseStatusCode} - {responseReasonPhrase}",
                                _httpClient.BaseAddress, response.StatusCode, response.ReasonPhrase);

                            this.LogVerbose(@"
{0}
>>>>> RequestDispatch
request: {1}
error: {2}
{0}", _separator, request, response.StatusCode + " - " + response.ReasonPhrase);

                            return default(T);
                        }

                        var contentString = response.Content.ReadAsStringAsync().Result;

                        if (string.IsNullOrEmpty(contentString))
                        {
                            this.LogDebug("Response content is null or empty");
                            return default(T);
                        }

                        var responseData = ApiResponseParser.ParseResponse<T>(contentString);
                        // if(T == typeof(TokenResponse) && responseData != null)
                        // {
                        //     if (responseData is TokenResponse tokenResponse)
                        //     {
                        //         Token = tokenResponse.Token;
                        //     }
                        // }

                        return responseData;
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError("OneBeyondClient Exception Message: {0}", ex.Message);
                this.LogDebug(ex, "OneBeyondClient Stack Trace: ");
                return default(T);
            }
        }
    }
}