// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharp.Cryptography;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using Newtonsoft.Json;


namespace PDT.OneBeyondAutomateVx.EPI
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	public class OneBeyondAutomateVX : EssentialsBridgeableDevice
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private OneBeyondAutomateVXConfigObject _config;

        private HttpClient _httpClient;

        private HttpsClient _httpsClient;

        private int _port;

        private string _url;

        private string _apiPrefix = "/api/";

        private bool _isHttps = false;

        private string _token;

        private string _base64Login;

		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="comms"></param>
        public OneBeyondAutomateVX(string key, string name, OneBeyondAutomateVXConfigObject config)
			: base(key, name)
		{



			Debug.Console(0, this, "Constructing new {0} instance", name);

			_config = config;

            _port = _config.Control.TcpSshProperties.Port;

            // Encode the user:pass as base64
            _base64Login = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _config.Control.TcpSshProperties.Username, _config.Control.TcpSshProperties.Password)));

            if (_config.Control.Method == eControlMethod.Http)
            {
                _url = string.Format("http://{0}:{1}", _config.Control.TcpSshProperties.Address, _port);

                Debug.Console(0, this, "Using HTTP for server at: {0} on port: {1}", _url, _port);
                _httpClient = new HttpClient();
            }
            else if (_config.Control.Method == eControlMethod.Https)
            {
                _url = string.Format("https://{0}:{1}", _config.Control.TcpSshProperties.Address, _port);

                Debug.Console(0, this, "Using HTTPS for server at: {0} on port: {1}", _url, _port);
                _httpsClient = new HttpsClient();
            }

            
        }


        #region Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new EssentialsPluginTemplateBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            // TODO [ ] Implement bridge links as needed

            // links to bridge


            UpdateFeedbacks();

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                UpdateFeedbacks();
            };
        }

        private void UpdateFeedbacks()
        {
            // TODO [ ] Update as needed for the plugin being developed

        }

        #endregion


        private HttpClientRequest GetHttpRequest(string url)
        {
            var req = new HttpClientRequest();
            req.Url.Parse(url);

            return req;
        }

        private HttpsClientRequest GetHttpsRequest(string url)
        {
            var req = new HttpsClientRequest();
            req.Url.Parse(url);

            return req;
        }


        /// <summary>
        /// Makes a request of the correct type and returns the data from the response
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private ResponseObjectBase MakeRequest(string url, object reqData)
        {
            var resData = new ResponseObjectBase();
            var authHeader = !string.IsNullOrEmpty(_token) ? _token : _base64Login;

            if (!_isHttps)
            {
                var req = GetHttpRequest(url);

                req.Header.AddHeader(new HttpHeader("Authorization", authHeader));
                req.Header.ContentType = "application/json";
                if (reqData != null)
                    req.ContentString = JsonConvert.SerializeObject(reqData);
                    
                _httpClient.DispatchAsync(req, (res, e) => {
                    switch (res.Code)
                    {
                        case 200:
                        {
                            Debug.Console(2, this, "Successful Request");
                            
                            var content = JsonConvert.DeserializeObject<ResponseObjectBase>(res.ContentString);

                            if (resData != null)
                                resData = content;

                            break;
                        }
                        case 404:
                        {
                            Debug.Console(2, this, "Request path not found");
                            break;
                        }
                        default:
                        {
                            Debug.Console(2, this, "Request Error: {0}", e);
                                
                            break;
                        }
                    }

                });
            }
            else
            {
                var req = GetHttpsRequest(url);

                req.Header.AddHeader(new HttpsHeader("Authorization", authHeader));
                req.Header.ContentType = "application/json";
                if (reqData != null)
                    req.ContentString = JsonConvert.SerializeObject(reqData);

                _httpsClient.DispatchAsync(req, (res, e) =>
                {
                    switch (res.Code)
                    {
                        case 200:
                        {
                            Debug.Console(2, this, "Successful Request");

                            var content = JsonConvert.DeserializeObject<ResponseObjectBase>(res.ContentString);

                            if (resData != null)
                                resData = content;

                            break;
                        }
                        case 404:
                        {
                            Debug.Console(2, this, "Request path not found");
                            break;
                        }
                        default:
                        {
                            Debug.Console(2, this, "Request Error: {0}", e);

                            break;
                        }
                    }

                });
            }

            return resData;
        }


        /// <summary>
        /// Attempts to get an authorization token
        /// </summary>
        public void GetToken()
        {
            var data = MakeRequest(_url + "/Get-Token", null);

            if (!string.IsNullOrEmpty(data.Token))
                _token = data.Token;
            else if (!string.IsNullOrEmpty(data.Error))
                Debug.Console(0, this, data.Error);
        }





    }
}

