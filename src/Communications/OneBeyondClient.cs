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
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            var username = controlConfig.TcpSshProperties.Username ?? "";
            var password = controlConfig.TcpSshProperties.Password ?? "";

            var authorizationBase64 = AuthenticationHelpers.EncodeBase64(key, username, password);

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authorizationBase64);

            Debug.LogVerbose(this, @"
{0}
>>>>> GenericClientHttps: 
Key = {1}
Host = {2}
Port = {3}
Username = {4}
Password = {5}
AuthBase64 = {6}
{0}", _separator, Key, baseAddress, port, username, password, authorizationBase64);
        }

        /// <summary>
        /// Sends the HTTP request and parses the response
        /// </summary>
        /// <typeparam name="T">Type of the response object</typeparam>
        /// <param name="method">HTTP method to use for the request</param>
        /// <param name="uri">The URI for the request</param>
        /// <param name="data">serialized data to send</param>
        /// <returns></returns>
        public T SendRequest<T>(HttpMethod method, string uri, string data = "")
        {
            var url = new Uri(uri);

            try
            {
                using (var request = new HttpRequestMessage(method, url))
                {
                    if (method == HttpMethod.Post || method == HttpMethod.Put)
                    {
                        request.Content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
                    }

                    Debug.LogVerbose(this, @"
{0}
>>>>> SendRequest
url: {1}
content: {2}
requestType: {3}
authHeaderValue: {4}
{0}", _separator, request.RequestUri, request.Content?.ReadAsStringAsync().Result, request.Method, _httpClient.DefaultRequestHeaders.Authorization);

                    using (var response = _httpClient.SendAsync(request).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            this.LogDebug("Unable to get sources for {Host}: {response.StatusCode} - {response.ReasonPhrase}");

                            Debug.LogVerbose(this, @"
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