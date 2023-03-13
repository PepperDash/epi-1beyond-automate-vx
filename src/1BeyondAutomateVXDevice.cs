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
	public partial class OneBeyondAutomateVX : EssentialsBridgeableDevice
    {
        // Fields, propeties and feedbacks for device feedback            ********************
        // located in separate file: 1BeyondAutomateVXDeviceProperties.cs ********************


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

            InitializeFeedbacks();
            
        }

        private void InitializeFeedbacks()
        {
            AutoSwitchIsOnFB = new BoolFeedback(() => AutoSwitchIsOn);
            RecordIsOnFB = new BoolFeedback(() => RecordIsOn);
            IsoRecordIsOnFB = new BoolFeedback(() => IsoRecordIsOn);
            StreamIsOnFB = new BoolFeedback(() => StreamIsOn);
            OutputIsOnFB = new BoolFeedback(() => OutputIsOn);

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



        public void GetAutoSwitchStatus()
        {
            var method = _apiPrefix + "AutoSwitchStatus";

            var res = MakeRequest(method, null);

            if (res.Results != true)
                AutoSwitchIsOn = false;
            else
                AutoSwitchIsOn = true;
        }

        public void SetAutoSwitch(bool state)
        {
            var method = "";

            if (state)
            {
                method = _apiPrefix + "StartAutoSwitch";
            }
            else
            {
                method = _apiPrefix + "StopAutoSwitch";
            }

            var res = MakeRequest(method, null);

            if (res.Message != "AutoSwitching Started Successfully")
                AutoSwitchIsOn = false;
            else
                AutoSwitchIsOn = true;
        }


        public void GetRecordStatus()
        {
            var method = _apiPrefix + "RecordStatus";

            var res = MakeRequest(method, null);

            if (res.Results != true)
                RecordIsOn = false;
            else
                RecordIsOn = true;
        }

        public enum eRecordOperation
        {
            start = 0,
            stop = 1,
            pause = 2,
        }

        public void SetRecord(eRecordOperation operation)
        {
            var method = "";

            if (operation == eRecordOperation.start)
            {
                method = _apiPrefix + "StartRecord";
            }
            else if (operation == eRecordOperation.pause)
            {
                method = _apiPrefix + "PauseRecord";
            }
            else if (operation == eRecordOperation.stop)
            {
                method = _apiPrefix + "StopRecord";
            }

            var res = MakeRequest(method, null);

            if (res.Message != "Record Started Successfully")
                RecordIsOn = false;
            else
                RecordIsOn = true;
        }


        public void GetIsoRecordStatus()
        {
            var method = _apiPrefix + "ISORecordStatus";

            var res = MakeRequest(method, null);

            if (res.Results != true)
                IsoRecordIsOn = false;
            else
                IsoRecordIsOn = true;
        }

        public void SetIsoRecord(bool state)
        {
            var method = "";

            if (state)
            {
                method = _apiPrefix + "StartISORecord";
            }
            else
            {
                method = _apiPrefix + "StopISORecord";
            }

            var res = MakeRequest(method, null);

            if (res.Message != "Started ISO Recording Successfully")
                IsoRecordIsOn = false;
            else
                IsoRecordIsOn = true;
        }


        public void GetStreamStatus()
        {
            var method = _apiPrefix + "StreamStatus";

            var res = MakeRequest(method, null);

            if (res.Results != true)
                StreamIsOn = false;
            else
                StreamIsOn = true;
        }

        public void SetStream(bool state)
        {
            var method = "";

            if (state)
            {
                method = _apiPrefix + "StartStream";
            }
            else
            {
                method = _apiPrefix + "StopStream";
            }

            var res = MakeRequest(method, null);

            if (res.Message != "Streaming Started Successfully")
                StreamIsOn = false;
            else
                StreamIsOn = true;
        }


        public void GetOutputStatus()
        {
            var method = _apiPrefix + "OutputStatus";

            var res = MakeRequest(method, null);

            if (res.Results != true)
                OutputIsOn = false;
            else
                OutputIsOn = true;
        }

        public void SetOutput(bool state)
        {
            var method = "";

            if (state)
            {
                method = _apiPrefix + "StartOutput";
            }
            else
            {
                method = _apiPrefix + "StopOutput";
            }

            var res = MakeRequest(method, null);

            if (res.Message != "Outputing Started Successfully")
                OutputIsOn = false;
            else
                OutputIsOn = true;
        }


        public void GetLayouts()
        {
            var method = _apiPrefix + "GetLayouts";

            var res = MakeRequest(method, null);

            if (res.Layouts != null)
                Layouts = res.Layouts;
        }


        public void GetLayoutStatus()
        {
            var method = _apiPrefix + "LayoutStatus";

            var res = MakeRequest(method, null);

            if (res.Layout != null)
                Layout = res.Layout;
        }

        public void SetLayout(uint id)
        {
            var method = _apiPrefix + "ChangeLayout";

            var res = MakeRequest(method, new Id(id.ToString()));

            if (res.Layout != null)
                Layout = res.Layout;
        } 



    }
}

