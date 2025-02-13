// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharp.Cryptography;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Devices.Common.Cameras;
using Newtonsoft.Json;


namespace PDT.OneBeyondAutomateVx.EPI
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
    public partial class OneBeyondAutomateVX : EssentialsBridgeableDevice, IHasCameras, IHasCameraAutoMode
    {
        // Fields, propeties and feedbacks for device feedback            ********************
        // located in separate file: 1BeyondAutomateVXDeviceProperties.cs ********************


        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private OneBeyondAutomateVXConfigObject _config;

        private HttpClient _httpClient;

        private HttpsClient _httpsClient;

        private int _httpPort = 3579; //default

        private int _httpsPort = 4443; //default

        private string _url;

        private const string _apiPrefix = "/api/";

        private bool _isHttps = false;

        private string _token = null;

        public string Token
        {
            get { return _token; }
            private set
            {
                if (value != _token)
                {
                    _token = value;
                    LoginSuccessfulFB.FireUpdate();
                }
            }
        }

        private string _base64Login;

        private StandbyStatus _standbyStatus = StandbyStatus.Unknown;


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


            // Encode the user:pass as base64
            _base64Login = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _config.Control.TcpSshProperties.Username, _config.Control.TcpSshProperties.Password)));

            if (_config.Control.Method == eControlMethod.Http)
            {
                if(_config.Control.TcpSshProperties.Port != 0)
                    _httpPort = _config.Control.TcpSshProperties.Port;

                _url = string.Format("http://{0}:{1}", _config.Control.TcpSshProperties.Address, _httpPort);

                Debug.Console(0, this, "Using HTTP for server at: {0} on port: {1}", _url, _httpPort);
                _httpClient = new HttpClient();
            }
            else if (_config.Control.Method == eControlMethod.Https)
            {
                if (_config.Control.TcpSshProperties.Port != 0)
                    _httpsPort = _config.Control.TcpSshProperties.Port;

                _url = string.Format("https://{0}:{1}", _config.Control.TcpSshProperties.Address, _httpsPort);

                Debug.Console(0, this, "Using HTTPS for server at: {0} on port: {1}", _url, _httpsPort);
                _httpsClient = new HttpsClient();
            }

            InitializeFeedbacks();
            
        }

        private void InitializeFeedbacks()
        {
            LoginSuccessfulFB = new BoolFeedback(() => !string.IsNullOrEmpty(Token));
            CameraAutoModeIsOnFeedback = new BoolFeedback(() => AutoSwitchIsOn);
            RecordIsOnFB = new BoolFeedback(() => RecordIsOn);
            IsoRecordIsOnFB = new BoolFeedback(() => IsoRecordIsOn);
            StreamIsOnFB = new BoolFeedback(() => StreamIsOn);
            OutputIsOnFB = new BoolFeedback(() => OutputIsOn);
            CameraAddressFB = new IntFeedback(() => CameraAddress);
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
            var joinMap = new OneBeyondAutomateVxBridgeJoinMap(joinStart);

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

            // Linked Feedbacks
            LoginSuccessfulFB.LinkInputSig(trilist.BooleanInput[joinMap.AuthenticatedFB.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.AutoSwitchOn.JoinNumber, () => SetAutoSwitch(true));
            CameraAutoModeIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoSwitchOn.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.AutoSwitchOff.JoinNumber, () => SetAutoSwitch(true));
            CameraAutoModeIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoSwitchOff.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.RecordStart.JoinNumber, () => SetRecord(eRecordOperation.start));
            RecordIsOnFB.LinkInputSig(trilist.BooleanInput[joinMap.RecordStart.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.RecordPause.JoinNumber, () => SetRecord(eRecordOperation.pause));

            trilist.SetSigFalseAction(joinMap.RecordStop.JoinNumber, () => SetRecord(eRecordOperation.stop));
            RecordIsOnFB.LinkComplementInputSig(trilist.BooleanInput[joinMap.RecordStop.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.IsoRecordOn.JoinNumber, () => SetIsoRecord(true));
            IsoRecordIsOnFB.LinkInputSig(trilist.BooleanInput[joinMap.IsoRecordOn.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.IsoRecordOff.JoinNumber, () => SetIsoRecord(true));
            IsoRecordIsOnFB.LinkComplementInputSig(trilist.BooleanInput[joinMap.IsoRecordOff.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.StreamOn.JoinNumber, () => SetStream(true));
            StreamIsOnFB.LinkInputSig(trilist.BooleanInput[joinMap.StreamOn.JoinNumber]);
                
            trilist.SetSigFalseAction(joinMap.StreamOff.JoinNumber, () => SetStream(true));
            StreamIsOnFB.LinkComplementInputSig(trilist.BooleanInput[joinMap.StreamOff.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.OutputOn.JoinNumber, () => SetOutput(true));
            OutputIsOnFB.LinkInputSig(trilist.BooleanInput[joinMap.OutputOn.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.OutputOff.JoinNumber, () => SetOutput(true));
            OutputIsOnFB.LinkComplementInputSig(trilist.BooleanInput[joinMap.OutputOff.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.Sleep.JoinNumber, () => SetSleep());
            trilist.SetSigFalseAction(joinMap.Wake.JoinNumber, () => SetWake());

            trilist.SetSigFalseAction(joinMap.GoHome.JoinNumber, () => GoHome());

            trilist.SetSigFalseAction(joinMap.GetAutoSwitchStatus.JoinNumber, () => GetAutoSwitchStatus());
            trilist.SetSigFalseAction(joinMap.GetRecordStatus.JoinNumber, () => GetRecordStatus());
            trilist.SetSigFalseAction(joinMap.GetIsoRecordStatus.JoinNumber, () => GetIsoRecordStatus());
            trilist.SetSigFalseAction(joinMap.GetStreamStatus.JoinNumber, () => GetStreamStatus());
            trilist.SetSigFalseAction(joinMap.GetOutputStatus.JoinNumber, () => GetOutputStatus());
            trilist.SetSigFalseAction(joinMap.GetCurrentLayout.JoinNumber, () => GetLayoutStatus());
            trilist.SetSigFalseAction(joinMap.GetLayouts.JoinNumber, () => GetLayouts());

            trilist.SetUShortSigAction(joinMap.ChangeLayout.JoinNumber, (l) =>
            {
                // Check for valid input (1-26)
                if (l < 1 || l > 26)
                    return;

                // convert the integer value passed in (1-26) to the equivalent alphabet character A-Z (capitalized)
                int offset = 64; // decimal value to add as an offset to get the desired ASCII value
                char layout = System.Convert.ToChar(offset + l);
 
                SetLayout(layout.ToString());
            });

            trilist.SetUShortSigAction(joinMap.ChangeRoomConfig.JoinNumber, (rc) => SetRoomConfig(rc));
            trilist.SetUShortSigAction(joinMap.ForceChangeRoomConfig.JoinNumber, (rc) => ForceSetRoomConfig(rc));

            trilist.SetUShortSigAction(joinMap.ChangeCamera.JoinNumber, (c) => SetCamera(c));
            CameraAddressFB.LinkInputSig(trilist.UShortInput[joinMap.ChangeCamera.JoinNumber]);

            trilist.SetSigFalseAction(joinMap.GetCameraStatus.JoinNumber, () => GetCameraStatus());
            trilist.SetSigFalseAction(joinMap.GetCameras.JoinNumber, () => GetCameras());

            trilist.SetUShortSigAction(joinMap.LiveCameraPreset.JoinNumber, (p) => SetCameraPreset(CameraAddressFB.UShortValue, p));

            trilist.SetSigFalseAction(joinMap.RecallCameraPreset.JoinNumber, () => 
                {
                    var camId = trilist.GetUshort(joinMap.CameraToRecallPresetOn.JoinNumber);
                    var presetId = trilist.GetUshort(joinMap.CameraPresetToRecall.JoinNumber);

                    if (camId == 0 || presetId == 0)
                    {
                        Debug.Console(0, this,
                            "Unable to recall preset.  Please specify values for both CameraToRecallPresetOn and CameraPresetToRecall analog joins");
                        return;
                    }

                    SetCameraPreset(camId, presetId);
                });

            trilist.SetSigFalseAction(joinMap.CopyFiles.JoinNumber, () =>
                {
                    var destFolder = trilist.GetString(joinMap.CopyFilesDestination.JoinNumber);
                    var logFolder = trilist.GetString(joinMap.CopyLogDestination.JoinNumber);
                    var delete = trilist.GetBool(joinMap.DeleteFiles.JoinNumber);

                    if (string.IsNullOrEmpty(destFolder))
                    {
                        Debug.Console(0, this, "Destination folder must be specified to copy files");
                        return;
                    }

                    CopyFiles(destFolder, logFolder, delete);

                });

            

            // Subscribe to events as needed

            ErrorMessageReceived += (o, a) =>
                {
                    trilist.SetString(joinMap.ErrorMessage.JoinNumber, a.ErrorMessage);
                };

            SuccessMessageReceived += (o, a) =>
                {
                    trilist.SetString(joinMap.SuccessMessage.JoinNumber, a.SuccessMessage);
                };

            LayoutChanged += (o, a) =>
                {
                    UpdateCurrentLayout(trilist, joinMap);
                };

            LayoutsChanged += (o, a) =>
                {
                    UpdateLayoutNames(trilist, joinMap);
                };

            RoomConfigChanged += (o, a) =>
                {
                    UpdateRoomConfig(trilist, joinMap);
                };

            RoomConfigsChanged += (o, a) =>
                {
                    UpdateRoomConfigNames(trilist, joinMap);

                };

            CameraSelected += (o, a) =>
                {
                    UpdateCameras(trilist, joinMap);
                };

            FileCopySuccessful += (o, a) =>
                {
                    CrestronInvoke.BeginInvoke((i) =>
                    {
                        trilist.SetBool(joinMap.CopyFilesSuccesfulFB.JoinNumber, true);
                        CrestronEnvironment.Sleep(100);
                        trilist.SetBool(joinMap.CopyFilesSuccesfulFB.JoinNumber, false);
                    }, null);
                };


            RecordingSpaceAvailableChanged += (o, a) =>
                {
                    UpdateAvailableSpace(trilist, joinMap);
                };


            bridge.Eisc.OnlineStatusChange += (o, a) =>
                {
                    if (a.DeviceOnLine)
                    {
                        SetInitialFBValues(trilist, joinMap);
                    }
                };

            
        }


        private void SetInitialFBValues(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            UpdateCurrentLayout(trilist, joinMap);
            UpdateLayoutNames(trilist, joinMap);
            UpdateRoomConfig(trilist, joinMap);
            UpdateRoomConfigNames(trilist, joinMap);
            UpdateCameras(trilist, joinMap);
            UpdateAvailableSpace(trilist, joinMap);
        }

        private void UpdateCurrentLayout(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            if (Layout == null)
                return;

            // convert from the letter A-Z back to 1-26
            char c = System.Convert.ToChar(Layout.Id);
            var val = (int)c;

            trilist.SetUshort(joinMap.ChangeLayout.JoinNumber, (ushort)val);

        }

        private void UpdateLayoutNames(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            if (Layouts == null)
                return;

            trilist.SetUshort(joinMap.NumberOfLayouts.JoinNumber, (ushort)Layouts.Count);

            for (uint i = 0; i < joinMap.LayoutName.JoinSpan; i++)
            {
                var name = "";

                if (Layouts.Count < i - 1)
                    name = Layouts[(int)i].Name;

                trilist.SetString(joinMap.LayoutName.JoinNumber + i, name);
            }
        }

        private void UpdateRoomConfig(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            if (RoomConfig == null)
                return;

            var roomConfig = System.Convert.ToUInt16(RoomConfig.Id);

            trilist.SetUshort(joinMap.ChangeRoomConfig.JoinNumber, roomConfig);
        }

        private void UpdateRoomConfigNames(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            if (RoomConfigs == null)
                return;

            trilist.SetUshort(joinMap.NumberOfRoomConfigs.JoinNumber, (ushort)RoomConfigs.Count);

            for (uint i = 0; i < joinMap.RoomConfigName.JoinSpan; i++)
            {
                var name = "";

                if (RoomConfigs.Count < i - 1)
                    name = RoomConfigs[(int)i].Name;

                trilist.SetString(joinMap.RoomConfigName.JoinNumber + i, name);
            }
        }

        private void UpdateCameras(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            trilist.SetUshort(joinMap.NumberOfCameras.JoinNumber, (ushort)Cameras.Count);
        }

        private void UpdateAvailableSpace(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            var availableSpace = System.Convert.ToUInt16(RecordingSpace.AvailableGigabytes);

            trilist.SetUshort(joinMap.StorageSpaceAvailableGB.JoinNumber, (ushort)availableSpace);

            var totalSpace = System.Convert.ToUInt16(RecordingSpace.TotalGigabytes);

            trilist.SetUshort(joinMap.StorageSpaceTotalGB.JoinNumber, (ushort)totalSpace);
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
            var authHeader = !string.IsNullOrEmpty(Token) ? Token : _base64Login;

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
                            Debug.Console(2, this, "Successful Request: {0}", req.Url);
                            Debug.Console(2, this, @"Response: \n{0}", res.ContentString);

                            var content = JsonConvert.DeserializeObject<TResponse>(res.ContentString);

                            if (content != null && !CheckForError(content as ResponseObjectBase))
                            {
                                resData = content;
                            }
                            break;
                        }
                        case 404:
                        {

                            Debug.Console(2, this, "Failed Request: {0}", req.Url);
                            Debug.Console(2, this, "Request path not found");
                            break;
                        }
                        default:
                        {
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
                    // Check for login credentials error and clear token
                    if (res.Error == "Incorrect Username or Password")
                    {
                        ClearToken();
                    }

                    OnErrorMessageReceived(res.Error);
                    return true;
                }
            }

            LoginSuccessfulFB.FireUpdate();

            return false;
        }


        /// <summary>
        /// Attempts to get an authorization token
        /// </summary>
        public void GetToken()
        {
            var data = MakeRequest<ResponseObjectBase, object>(_url + "/Get-Token", null);

            if (!string.IsNullOrEmpty(data.Token))
            {
                Token = data.Token;
            }
            else if (!string.IsNullOrEmpty(data.Error))
                Debug.Console(0, this, data.Error);
        }

        public void ClearToken()
        {
            Token = null;
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



        public void SetLayout(string layout)
        {
            var url = _apiPrefix + "ChangeLayout";

            var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(layout));

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

            var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(id.ToString()));

            if (res.Layout != null)
                RoomConfig = res.Layout;
        }

        public void ForceSetRoomConfig(uint id)
        {
            var url = _apiPrefix + "ForceChangeRoomConfig";

            var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(id.ToString()));

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
                CameraAddress = System.Convert.ToInt32(res.Address);
        }

        public void SetCamera(uint address)
        {
            var url = _apiPrefix + "ManualSwitchCamera";

            var res = MakeRequest<ResponseObjectBase, CamAddress>(url, new CamAddress(address.ToString()));

            if (res.Layout != null)
                RoomConfig = res.Layout;
        }

        public void SetCameraPreset(uint camId, uint presetId)
        {
            var url = _apiPrefix + "CallCameraPreset";

            var res = MakeRequest<ResponseObjectBase, CameraCmdPreset>(url, new CameraCmdPreset(camId.ToString(), presetId.ToString()));

            if (!string.IsNullOrEmpty(res.Message))
                OnSuccessMessageReceived(res.Message);
        }

        public void SaveCameraPreset(uint camId, uint presetId)
        {
            var url = _apiPrefix + "SaveCameraPreset";

            var res = MakeRequest<ResponseObjectBase, CameraCmdPreset>(url, new CameraCmdPreset(camId.ToString(), presetId.ToString()));

            if (!string.IsNullOrEmpty(res.Message))
                OnSuccessMessageReceived(res.Message);
        }

        public void StartCameraPanTilt(uint camId, uint ptDir)
        {
            var url = _apiPrefix + "StartPT";

            var res = MakeRequest<ResponseObjectBase, CameraCmdPanTilt>(url, new CameraCmdPanTilt(camId.ToString(), ptDir.ToString()));

            if (!string.IsNullOrEmpty(res.Message))
                OnSuccessMessageReceived(res.Message);
        }

        public void StopCameraPanTilt(uint camId)
        {
            var url = _apiPrefix + "StopPT";

            var res = MakeRequest<ResponseObjectBase, CameraCmdBase>(url, new CameraCmdBase(camId.ToString()));

            if (!string.IsNullOrEmpty(res.Message))
                OnSuccessMessageReceived(res.Message);
        }

        public void StartCameraZoom(uint camId, uint zDir)
        {
            var url = _apiPrefix + "StartZ";

            var res = MakeRequest<ResponseObjectBase, CameraCmdZoom>(url, new CameraCmdZoom(camId.ToString(), zDir.ToString()));

            if (!string.IsNullOrEmpty(res.Message))
                OnSuccessMessageReceived(res.Message);
        }

        public void StopCameraZoom(uint camId)
        {
            var url = _apiPrefix + "StopZ";

            var res = MakeRequest<ResponseObjectBase, CameraCmdBase>(url, new CameraCmdBase(camId.ToString()));

            if (!string.IsNullOrEmpty(res.Message))
                OnSuccessMessageReceived(res.Message);
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


        /// <summary>
        /// Copy Files on teh device
        /// </summary>
        /// <param name="dest">Destination folder to copy files to</param>
        /// <param name="logDest">Destination for log.  Leave black to turn off logging</param>
        /// <param name="delete">If true, delete source files after copy</param>
        public void CopyFiles(string dest, string logDest, bool delete)
        {
            var url = _apiPrefix + "CopyFiles";

            var res = MakeRequest<ResponseObjectBase, FilesParams>(url, new FilesParams(dest, logDest, delete));

            if (res.Status == "OK" && res.Message == "Successfully backed up information")
                OnFileCopySuccessful();
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


        public void SetSleep()
        {
            var url = _apiPrefix + "Sleep";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Status == "OK" && res.Message == "VX went to sleep successfully")
                _standbyStatus = StandbyStatus.Asleep;
                //unfortunately API does not allow poll for standby status
        }

        public void SetWake()
        {
            var url = _apiPrefix + "Wake";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Status == "OK" && res.Message == "VX woke up successfully")
                _standbyStatus = StandbyStatus.Awake;
        }


        public void Restart()
        {
            var url = _apiPrefix + "Restart";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            //TODO - reset after success send restart cmd
            //if (res.Status == "OK" && res.Message == "Restart command initiated")
            //stop any polling
            //clear token
            //reset standbystatus
        }


        public void SetCloseWirecast()
        {
            var url = _apiPrefix + "CloseWirecast";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);
        }


        public void GetScenarios()
        {
            var url = _apiPrefix + "GetScenarios";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Scenarios != null)
                Scenarios = res.Scenarios;
        }


        public void GetScenarioStatus()
        {
            var url = _apiPrefix + "ScenarioStatus";

            var res = MakeRequest<ResponseObjectBase, object>(url, null);

            if (res.Scenario != null)
                Scenario = res.Scenario;
        }

        public void SetScenario(uint id)
        {
            var url = _apiPrefix + "ChangeScenario";

            var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(id.ToString()));

            if (res.Scenario != null)
                Scenario = res.Scenario;
        }


    }

    public enum StandbyStatus
    {
        Unknown = 0,
        Asleep = 1,
        Awake = 2
    }
}

