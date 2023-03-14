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

        private const string _apiPrefix = "/api/";

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

            // TODO: Check for data validity
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


        private HttpClientRequest GetHttpRequest(string url, Crestron.SimplSharp.Net.Http.RequestType type)
        {
            var req = new HttpClientRequest();
            req.Url.Parse(url);
            req.RequestType = type;

            return req;
        }

        private HttpsClientRequest GetHttpsRequest(string url, Crestron.SimplSharp.Net.Https.RequestType type)
        {
            var req = new HttpsClientRequest();
            req.Url.Parse(url);
            req.RequestType = type;

            return req;
        }


        /// <summary>
        /// Makes a request of the correct type and returns the data from the response
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private TResponse MakeRequest<TResponse, TRequest>(string url, TRequest reqData) where TResponse : new()
        {
            var resData = new TResponse();
            var authHeader = !string.IsNullOrEmpty(_token) ? _token : _base64Login;

            if (!_isHttps)
            {
                var req = GetHttpRequest(url, Crestron.SimplSharp.Net.Http.RequestType.Post);


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

                            var content = JsonConvert.DeserializeObject<TResponse>(res.ContentString);

                            if (content != null && !CheckForError(content as ResponseObjectBase))
                            {
                                resData = content;
                            }
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
                            OnErrorMessageReceived(e.ToString());
                            break;
                        }
                    }

                });
            }
            else
            {
                var req = GetHttpsRequest(url, Crestron.SimplSharp.Net.Https.RequestType.Post);

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

                            var content = JsonConvert.DeserializeObject<TResponse>(res.ContentString);

                            if (content != null && !CheckForError(content as ResponseObjectBase))
                            {
                                resData = content;
                            }
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
                            OnErrorMessageReceived(e.ToString());
                            break;
                        }
                    }

                });
            }

            return resData;
        }

        /// <summary>
        /// Checks for error message and fires event if found
        /// </summary>
        /// <param name="res"></param>
        private bool CheckForError(ResponseObjectBase res)
        {
            if(res.Status.ToLower() == "error")
            {
                if (!string.IsNullOrEmpty(res.Error))
                {
                    OnErrorMessageReceived(res.Error);
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Attempts to get an authorization token
        /// </summary>
        public void GetToken()
        {
            var data = MakeRequest<ResponseObjectBase, object>(_url + "/Get-Token", null);

            if (!string.IsNullOrEmpty(data.Token))
                _token = data.Token;
            else if (!string.IsNullOrEmpty(data.Error))
                Debug.Console(0, this, data.Error);
        }



        public void GetAutoSwitchStatus()
        {
            var url = _apiPrefix + "AutoSwitchStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Results == null) return;
            
            if (res.Results != true)
                AutoSwitchIsOn = false;
            else
                AutoSwitchIsOn = true;

        }

        public void SetAutoSwitch(bool state)
        {
            var url = "";

            if (state)
            {
                url = _apiPrefix + "StartAutoSwitch";
            }
            else
            {
                url = _apiPrefix + "StopAutoSwitch";
            }

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Message != "AutoSwitching Started Successfully")
                AutoSwitchIsOn = false;
            else
                AutoSwitchIsOn = true;
        }


        public void GetRecordStatus()
        {
            var url = _apiPrefix + "RecordStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

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
            var url = "";

            if (operation == eRecordOperation.start)
            {
                url = _apiPrefix + "StartRecord";
            }
            else if (operation == eRecordOperation.pause)
            {
                url = _apiPrefix + "PauseRecord";
            }
            else if (operation == eRecordOperation.stop)
            {
                url = _apiPrefix + "StopRecord";
            }

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Message != "Record Started Successfully")
                RecordIsOn = false;
            else
                RecordIsOn = true;
        }


        public void GetIsoRecordStatus()
        {
            var url = _apiPrefix + "ISORecordStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

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

            var res = MakeRequest<ResponseObjectBase, object>(method, null);

            if (res.Message != "Started ISO Recording Successfully")
                IsoRecordIsOn = false;
            else
                IsoRecordIsOn = true;
        }


        public void GetStreamStatus()
        {
            var url = _apiPrefix + "StreamStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Results != true)
                StreamIsOn = false;
            else
                StreamIsOn = true;
        }

        public void SetStream(bool state)
        {
            var url = "";

            if (state)
            {
                url = _apiPrefix + "StartStream";
            }
            else
            {
                url = _apiPrefix + "StopStream";
            }

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Message != "Streaming Started Successfully")
                StreamIsOn = false;
            else
                StreamIsOn = true;
        }


        public void GetOutputStatus()
        {
            var url = _apiPrefix + "OutputStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Results != true)
                OutputIsOn = false;
            else
                OutputIsOn = true;
        }

        public void SetOutput(bool state)
        {
            var url = "";

            if (state)
            {
                url = _apiPrefix + "StartOutput";
            }
            else
            {
                url = _apiPrefix + "StopOutput";
            }

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Message != "Outputing Started Successfully")
                OutputIsOn = false;
            else
                OutputIsOn = true;
        }


        public void GetLayouts()
        {
            var url = _apiPrefix + "GetLayouts";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Layouts != null)
                Layouts = res.Layouts;
        }


        public void GetLayoutStatus()
        {
            var url = _apiPrefix + "LayoutStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Layout != null)
                Layout = res.Layout;
        }

        public void SetLayout(uint id)
        {
            var url = _apiPrefix + "ChangeLayout";

            var res = MakeRequest<ResponseObjectBase, Id>(url, new Id(id.ToString()));

            if (res.Layout != null)
                Layout = res.Layout;
        }


        public void GetRoomConfigStatus()
        {
            var url = _apiPrefix + "RoomConfigStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Layout != null)
                RoomConfig = res.Layout;
        }

        public void GetRoomConfigs()
        {
            var url = _apiPrefix + "GetRoomConfigs";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Layouts != null)
                RoomConfigs = res.Layouts;
        }

        public void SetRoomConfig(uint id)
        {
            var url = _apiPrefix + "ChangeRoomConfig";

            var res = MakeRequest<ResponseObjectBase, Id>(url, new Id(id.ToString()));

            if (res.Layout != null)
                RoomConfig = res.Layout;
        }

        public void ForceSetRoomConfig(uint id)
        {
            var url = _apiPrefix + "ForceChangeRoomConfig";

            var res = MakeRequest<ResponseObjectBase, Id>(url, new Id(id.ToString()));

            if (res.Layout != null)
                RoomConfig = res.Layout;
        }

        public void GoHome()
        {
            var url = _apiPrefix + "GoHome";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);
        }


        public void GetCameras()
        {
            var url = _apiPrefix + "GetCameras";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Cameras != null)
                Cameras = res.Cameras;
        }

        public void GetCameraStatus()
        {
            var url = _apiPrefix + "CameraStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Address != null)
                CameraAddress = System.Convert.ToUInt32(res.Address);
        }

        public void SetCamera(uint address)
        {
            var url = _apiPrefix + "ManualSwitchCamera";

            var res = MakeRequest<ResponseObjectBase, Address>(url, new Address(address.ToString()));

            if (res.Layout != null)
                RoomConfig = res.Layout;
        }

        public void SetCameraPreset(uint camId, uint presetId)
        {
            var url = _apiPrefix + "CallCameraPreset";

            var res = MakeRequest<ResponseObjectBase, CameraPreset>(url, new CameraPreset(camId.ToString(), presetId.ToString()));
        }


        public void ImportCameraPresets()
        {
            var url = _apiPrefix + "ImportCameraPresets";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);
        }

        public void ExportCameraPresets()
        {
            var url = _apiPrefix + "ExportCameraPresets";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);
        }


        public void CopyFiles(string dest, string logDest, bool delete)
        {
            var url = _apiPrefix + "CopyFiles";

            var res = MakeRequest<ResponseObjectBase, FilesParams>(url, new FilesParams(dest, logDest, delete));
        }

        public void GetStorageSpaceAvailable()
        {
            var url = _apiPrefix + "StorageSpaceAvail";

            // TODO: Test the formatting of this command, API documentation is not clear on the value of the drives to send
            // https://sdkcon78221.crestron.com/sdk/Automate-VX-API/Content/Topics/Automate-API/API-Reference/StorageSpaceAvail-API.htm
            var res = MakeRequest<ResponseObjectBase, DrivesParams>(url, new DrivesParams("C:\\, D:\\, L:\\"));

            if (res.Drives != null)
                Drives = res.Drives;
        }

        public void GetRecordingSpaceAvailable()
        {
            var url = _apiPrefix + "RecodingSpaceAvail";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.AvailableGigabytes != null && res.TotalGigabytes != null)
            {
                RecordingSpace = new RecordingSpace(res.AvailableGigabytes, res.TotalGigabytes);
            }
        }


        
    }
}

