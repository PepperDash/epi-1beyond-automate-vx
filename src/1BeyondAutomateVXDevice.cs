using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using OneBeyondAutomateVxEpi.GenericClients;
using System;


namespace OneBeyondAutomateVxEpi
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	/// 

	public partial class OneBeyondAutomateVx : EssentialsBridgeableDevice
	{
		private static readonly string Separator = new string('-', 50);

		#region IRestfulComms

		private readonly IRestfulComms _client;

		private int _responseCode;

		public int ResponseCode
		{
			get { return _responseCode; }
			set
			{
				_responseCode = value;
				ResponseCodeFeedback.FireUpdate();
			}
		}

		public IntFeedback ResponseCodeFeedback { get; private set; }

		private string _responseContent;

		public string ResponseContent
		{
			get { return _responseContent; }
			set
			{
				_responseContent = value;
				ResponseContentFeedback.FireUpdate();
			}
		}

		public StringFeedback ResponseContentFeedback { get; private set; }


		private string _responseError;

		public string ResponseError
		{
			get { return _responseError; }
			set
			{
				_responseError = value;

				ResponseErrorFeedback.FireUpdate();
			}
		}

		public StringFeedback ResponseErrorFeedback { get; private set; }


		#endregion

		// Fields, propeties and feedbacks for device feedback            ********************
		// located in separate file: 1BeyondAutomateVXDeviceProperties.cs ********************
		//private OneBeyondAutomateVXConfigObject _config;

		//private HttpClient _httpClient;

		//private HttpsClient _httpsClient;

		//private readonly int _httpPort = 3579; //default

		//private readonly int _httpsPort = 4443; //default

		//private string _url;

		private const string ApiPath = "/api/";

		//private bool _isHttps = false;

		private string _token = null;

		public string Token
		{
			get { return _token; }
			private set
			{
				if (value != _token)
				{
					_token = value;
					LoginSuccessfulFb.FireUpdate();
				}
			}
		}

		private readonly string _base64Login;


		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="client"></param>
		public OneBeyondAutomateVx(string key, string name, OneBeyondAutomateVXConfigObject config, IRestfulComms client)
			: base(key, name)
		{

			AutomateVxDebug.ResetDebugLevels();

			Debug.Console(AutomateVxDebug.Trace, this, "Constructing new {0} instance", name);

			_client = client;
			//_config = config;

			// Encode the user:pass as base64
			//_base64Login = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _config.Control.TcpSshProperties.Username, _config.Control.TcpSshProperties.Password)));

			//if (_config.Control.Method == eControlMethod.Http)
			//{
			//    if(_config.Control.TcpSshProperties.Port != 0)
			//        _httpPort = _config.Control.TcpSshProperties.Port;

			//    _url = string.Format("http://{0}:{1}", _config.Control.TcpSshProperties.Address, _httpPort);

			//    Debug.Console(AutomateVxDebug.Trace, this, "Using HTTP for server at: {0} on port: {1}", _url, _httpPort);
			//    _httpClient = new HttpClient();
			//}
			//else if (_config.Control.Method == eControlMethod.Https)
			//{
			//    if (_config.Control.TcpSshProperties.Port != 0)
			//        _httpsPort = _config.Control.TcpSshProperties.Port;

			//    _url = string.Format("https://{0}:{1}", _config.Control.TcpSshProperties.Address, _httpsPort);

			//    Debug.Console(AutomateVxDebug.Trace, this, "Using HTTPS for server at: {0} on port: {1}", _url, _httpsPort);
			//    _httpsClient = new HttpsClient();
			//}

			ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
			ResponseContentFeedback = new StringFeedback(() => ResponseContent);
			ResponseErrorFeedback = new StringFeedback(() => ResponseError);

			InitializeFeedbacks();

		}

		private void InitializeFeedbacks()
		{
			LoginSuccessfulFb = new BoolFeedback(() => !string.IsNullOrEmpty(Token));
			AutoSwitchIsOnFb = new BoolFeedback(() => AutoSwitchIsOn);
			RecordIsOnFb = new BoolFeedback(() => RecordIsOn);
			IsoRecordIsOnFb = new BoolFeedback(() => IsoRecordIsOn);
			StreamIsOnFb = new BoolFeedback(() => StreamIsOn);
			OutputIsOnFb = new BoolFeedback(() => OutputIsOn);
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

			Debug.Console(AutomateVxDebug.Notice, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(AutomateVxDebug.Trace, "Linking to Bridge Type {0}", GetType().Name);

			// Linked Feedbacks
			trilist.SetSigTrueAction(joinMap.Authenticate.JoinNumber, GetToken);
			LoginSuccessfulFb.LinkInputSig(trilist.BooleanInput[joinMap.Authenticate.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.AutoSwitchOn.JoinNumber, () => SetAutoSwitch(true));
			AutoSwitchIsOnFb.LinkInputSig(trilist.BooleanInput[joinMap.AutoSwitchOn.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.AutoSwitchOff.JoinNumber, () => SetAutoSwitch(true));
			AutoSwitchIsOnFb.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoSwitchOff.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.RecordStart.JoinNumber, () => SetRecord(eRecordOperation.start));
			RecordIsOnFb.LinkInputSig(trilist.BooleanInput[joinMap.RecordStart.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.RecordPause.JoinNumber, () => SetRecord(eRecordOperation.pause));

			trilist.SetSigFalseAction(joinMap.RecordStop.JoinNumber, () => SetRecord(eRecordOperation.stop));
			RecordIsOnFb.LinkComplementInputSig(trilist.BooleanInput[joinMap.RecordStop.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.IsoRecordOn.JoinNumber, () => SetIsoRecord(true));
			IsoRecordIsOnFb.LinkInputSig(trilist.BooleanInput[joinMap.IsoRecordOn.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.IsoRecordOff.JoinNumber, () => SetIsoRecord(true));
			IsoRecordIsOnFb.LinkComplementInputSig(trilist.BooleanInput[joinMap.IsoRecordOff.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.StreamOn.JoinNumber, () => SetStream(true));
			StreamIsOnFb.LinkInputSig(trilist.BooleanInput[joinMap.StreamOn.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.StreamOff.JoinNumber, () => SetStream(true));
			StreamIsOnFb.LinkComplementInputSig(trilist.BooleanInput[joinMap.StreamOff.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.OutputOn.JoinNumber, () => SetOutput(true));
			OutputIsOnFb.LinkInputSig(trilist.BooleanInput[joinMap.OutputOn.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.OutputOff.JoinNumber, () => SetOutput(true));
			OutputIsOnFb.LinkComplementInputSig(trilist.BooleanInput[joinMap.OutputOff.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.Sleep.JoinNumber, SetSleep);
			trilist.SetSigFalseAction(joinMap.Wake.JoinNumber, SetWake);

			trilist.SetSigFalseAction(joinMap.GoHome.JoinNumber, GoHome);

			trilist.SetSigFalseAction(joinMap.GetAutoSwitchStatus.JoinNumber, GetAutoSwitchStatus);
			trilist.SetSigFalseAction(joinMap.GetRecordStatus.JoinNumber, GetRecordStatus);
			trilist.SetSigFalseAction(joinMap.GetIsoRecordStatus.JoinNumber, GetIsoRecordStatus);
			trilist.SetSigFalseAction(joinMap.GetStreamStatus.JoinNumber, GetStreamStatus);
			trilist.SetSigFalseAction(joinMap.GetOutputStatus.JoinNumber, GetOutputStatus);
			trilist.SetSigFalseAction(joinMap.GetCurrentLayout.JoinNumber, GetLayoutStatus);
			trilist.SetSigFalseAction(joinMap.GetLayouts.JoinNumber, GetLayouts);

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

			trilist.SetSigFalseAction(joinMap.GetCameraStatus.JoinNumber, GetCameraStatus);
			trilist.SetSigFalseAction(joinMap.GetCameras.JoinNumber, GetCameras);

			trilist.SetUShortSigAction(joinMap.LiveCameraPreset.JoinNumber, (p) => SetCameraPreset(CameraAddressFB.UShortValue, p));

			trilist.SetSigFalseAction(joinMap.RecallCameraPreset.JoinNumber, () =>
				{
					var camId = trilist.GetUshort(joinMap.CameraToRecallPresetOn.JoinNumber);
					var presetId = trilist.GetUshort(joinMap.CameraPresetToRecall.JoinNumber);

					if (camId == 0 || presetId == 0)
					{
						Debug.Console(AutomateVxDebug.Trace, this,
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
						Debug.Console(AutomateVxDebug.Trace, this, "Destination folder must be specified to copy files");
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

			LayoutChanged += (o, a) => UpdateCurrentLayout(trilist, joinMap);

			LayoutsChanged += (o, a) => UpdateLayoutNames(trilist, joinMap);

			RoomConfigChanged += (o, a) => UpdateRoomConfig(trilist, joinMap);

			RoomConfigsChanged += (o, a) => UpdateRoomConfigNames(trilist, joinMap);

			CamerasChanged += (o, a) => UpdateCameras(trilist, joinMap);

			FileCopySuccessful += (o, a) => CrestronInvoke.BeginInvoke((i) =>
			{
				trilist.SetBool(joinMap.CopyFilesSuccesfulFB.JoinNumber, true);
				CrestronEnvironment.Sleep(100);
				trilist.SetBool(joinMap.CopyFilesSuccesfulFB.JoinNumber, false);
			}, null);


			RecordingSpaceAvailableChanged += (o, a) => UpdateAvailableSpace(trilist, joinMap);

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				//trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

				SetInitialFbValues(trilist, joinMap);
			};
		}


		private void SetInitialFbValues(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
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
			if (Layout == null || Layout.Id == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateCurrentLayout: Layout or Layout.Id is null");
				return;
			}

			// convert from the letter A-Z back to 1-26
			char c = System.Convert.ToChar(Layout.Id);
			var val = (int)c;

			trilist.SetUshort(joinMap.ChangeLayout.JoinNumber, (ushort)val);
		}

		private void UpdateLayoutNames(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (Layouts == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateLayoutNames: Layouts is null");
				return;
			}

			trilist.SetUshort(joinMap.NumberOfLayouts.JoinNumber, (ushort)Layouts.Count);

			if (Layouts.Count == 0)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateLayoutNames: Layouts.Count == 0");
				return;
			}

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
			if (RoomConfig == null || RoomConfig.Id == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateRoomConfig: RoomConfig or RoomConfig.Id is null");
				return;
			}

			var roomConfig = System.Convert.ToUInt16(RoomConfig.Id);

			trilist.SetUshort(joinMap.ChangeRoomConfig.JoinNumber, roomConfig);
		}

		private void UpdateRoomConfigNames(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (RoomConfigs == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateRoomConfigNames: RoomConfig is null");
				return;
			}

			trilist.SetUshort(joinMap.NumberOfRoomConfigs.JoinNumber, (ushort)RoomConfigs.Count);

			if (RoomConfigs.Count == 0)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateRoomConfigNames: RoomConfigs.Count == 0");
				return;
			}

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
			if (Cameras == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateCameras: Cameras is null");
				return;
			}

			trilist.SetUshort(joinMap.NumberOfCameras.JoinNumber, (ushort)Cameras.Count);
		}

		private void UpdateAvailableSpace(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (RecordingSpace == null || RecordingSpace.AvailableGigabytes == null || RecordingSpace.TotalGigabytes == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "UpdateAvailableSpace: RecordingSpace is null");
				return;
			}

			var availableSpace = System.Convert.ToUInt16(RecordingSpace.AvailableGigabytes);

			trilist.SetUshort(joinMap.StorageSpaceAvailableGB.JoinNumber, (ushort)availableSpace);

			var totalSpace = System.Convert.ToUInt16(RecordingSpace.TotalGigabytes);

			trilist.SetUshort(joinMap.StorageSpaceTotalGB.JoinNumber, (ushort)totalSpace);
		}


		#endregion


		private HttpClientRequest GetHttpRequest(string url, Crestron.SimplSharp.Net.Http.RequestType type)
		{
			Debug.Console(AutomateVxDebug.Verbose, this, "GetHttpRequest: url = {0}, type = {1}", url, type.ToString());
			var req = new HttpClientRequest();
			req.Url.Parse(url);
			req.RequestType = type;

			return req;
		}

		private HttpsClientRequest GetHttpsRequest(string url, Crestron.SimplSharp.Net.Https.RequestType type)
		{
			Debug.Console(AutomateVxDebug.Verbose, this, "GetHttpsRequest: url = {0}, type = {1}", url, type.ToString());
			var req = new HttpsClientRequest();
			req.Url.Parse(url);
			req.RequestType = type;

			return req;
		}

		private void OnResponseReceived(object sender, GenericClientResponseEventArgs args)
		{
			try
			{
				Debug.Console(AutomateVxDebug.Verbose, this,
					"OnResponseReceived: Code = {0} | ContentString = {1}",
					args.Code, args.ContentString);

				ResponseCode = args.Code;

				if (string.IsNullOrEmpty(args.ContentString))
				{
					Debug.Console(AutomateVxDebug.Notice, this, "OnResponseReceived: args.ContentString is null or empty");
					return;
				}

				ResponseContent = args.ContentString;
				Debug.Console(AutomateVxDebug.Verbose, this, "OnResponseReceived: ResponseContent = {0}", ResponseContent);

				//var jToken = IsValidJson(args.ContentString);
				//if (jToken == null)
				//{
				//    Debug.Console(AutomateVxDebug.Notice, this, "OnResponseReceived: IsValidJson failed, passing ContentString as string");
				//    return;
				//}

				//var feedback = JsonConvert.DeserializeObject<RootDevObject>(ResponseContent);
				//Debug.Console(AutomateVxDebug.Notice, "OnResponseReceived: Begin parsing deserialized JSON objects");

			}
			catch (Exception ex)
			{
				Debug.Console(AutomateVxDebug.Notice, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Exception Message: {0}", ex.Message);
				Debug.Console(AutomateVxDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(AutomateVxDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Inner Exception {0}", ex.InnerException);
			}
		}


		/// <summary>
		/// Makes a request of the correct type and returns the data from the response
		/// </summary>
		/// <param name="url"></param>
		/// <param name="reqData"></param>
		/// <returns></returns>
		//private TResponse MakeRequest<TResponse, TRequest>(string url, TRequest reqData) where TResponse : new()
		//{
		//    Debug.Console(AutomateVxDebug.Verbose, this, "MakeRequest: url = {0}, reqdata = {1}", url, reqData.ToString());

		//    var resData = new TResponse();
		//    var authHeader = !string.IsNullOrEmpty(Token) ? Token : _base64Login;

		//    if (!_isHttps)
		//    {
		//        var req = GetHttpRequest(url, Crestron.SimplSharp.Net.Http.RequestType.Post);


		//        req.Header.AddHeader(new HttpHeader("Authorization", authHeader));
		//        req.Header.ContentType = "application/json";
		//        if (reqData != null)
		//            req.ContentString = JsonConvert.SerializeObject(reqData);

		//        _httpClient.DispatchAsync(req, (res, e) => {
		//            switch (res.Code)
		//            {
		//                case 200:
		//                {
		//                    Debug.Console(AutomateVxDebug.Verbose, this, "Successful Request");

		//                    var content = JsonConvert.DeserializeObject<TResponse>(res.ContentString);

		//                    if (content != null && !CheckForError(content as ResponseObjectBase))
		//                    {
		//                        resData = content;
		//                    }
		//                    break;
		//                }
		//                case 404:
		//                {
		//                    Debug.Console(AutomateVxDebug.Verbose, this, "Request path not found");
		//                    break;
		//                }
		//                default:
		//                {
		//                    Debug.Console(AutomateVxDebug.Verbose, this, "Request Error: {0}", e);
		//                    OnErrorMessageReceived(e.ToString());
		//                    break;
		//                }
		//            }

		//        });
		//    }
		//    else
		//    {
		//        var req = GetHttpsRequest(url, Crestron.SimplSharp.Net.Https.RequestType.Post);

		//        req.Header.AddHeader(new HttpsHeader("Authorization", authHeader));
		//        req.Header.ContentType = "application/json";
		//        if (reqData != null)
		//            req.ContentString = JsonConvert.SerializeObject(reqData);

		//        _httpsClient.DispatchAsync(req, (res, e) =>
		//        {
		//            switch (res.Code)
		//            {
		//                case 200:
		//                {
		//                    Debug.Console(AutomateVxDebug.Verbose, this, "Successful Request: {0}", req.Url);
		//                    Debug.Console(AutomateVxDebug.Verbose, this, @"Response: \n{0}", res.ContentString);

		//                    var content = JsonConvert.DeserializeObject<TResponse>(res.ContentString);

		//                    if (content != null && !CheckForError(content as ResponseObjectBase))
		//                    {
		//                        resData = content;
		//                    }
		//                    break;
		//                }
		//                case 404:
		//                {

		//                    Debug.Console(AutomateVxDebug.Verbose, this, "Failed Request: {0}", req.Url);
		//                    Debug.Console(AutomateVxDebug.Verbose, this, "Request path not found");
		//                    break;
		//                }
		//                default:
		//                {
		//                    OnErrorMessageReceived(e.ToString());
		//                    break;
		//                }
		//            }

		//        });
		//    }

		//    return resData;
		//}

		/// <summary>
		/// Checks for error message and fires event if found
		/// </summary>
		/// <param name="res"></param>
		private bool CheckForError(ResponseObjectBase res)
		{
			if (res.Status.ToLower() == "error")
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

			LoginSuccessfulFb.FireUpdate();

			return false;
		}


		/// <summary>
		/// Attempts to get an authorization token
		/// </summary>
		public void GetToken()
		{
			var url = string.Format("{0}{1}", _url, "Get-Token");
			Debug.Console(AutomateVxDebug.Verbose, this, "GetToken: url = {0}", url);
			var data = MakeRequest<ResponseObjectBase, object>(url, null);

			if (!string.IsNullOrEmpty(data.Token))
			{
				Token = data.Token;
				Debug.Console(AutomateVxDebug.Verbose, this, "GetToken: Token = {0}", Token);
			}
			else if (!string.IsNullOrEmpty(data.Error))
				Debug.Console(AutomateVxDebug.Trace, this, data.Error);
		}

		public void ClearToken()
		{
			Token = null;
		}

		public void GetAutoSwitchStatus()
		{
			var url = ApiPath + "AutoSwitchStatus";

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
				url = ApiPath + "StartAutoSwitch";
			}
			else
			{
				url = ApiPath + "StopAutoSwitch";
			}

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Message != "AutoSwitching Started Successfully")
				AutoSwitchIsOn = false;
			else
				AutoSwitchIsOn = true;
		}


		public void GetRecordStatus()
		{
			var url = ApiPath + "RecordStatus";

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
				url = ApiPath + "StartRecord";
			}
			else if (operation == eRecordOperation.pause)
			{
				url = ApiPath + "PauseRecord";
			}
			else if (operation == eRecordOperation.stop)
			{
				url = ApiPath + "StopRecord";
			}

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Message != "Record Started Successfully")
				RecordIsOn = false;
			else
				RecordIsOn = true;
		}


		public void GetIsoRecordStatus()
		{
			var url = ApiPath + "ISORecordStatus";

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
				method = ApiPath + "StartISORecord";
			}
			else
			{
				method = ApiPath + "StopISORecord";
			}

			var res = MakeRequest<ResponseObjectBase, object>(method, null);

			if (res.Message != "Started ISO Recording Successfully")
				IsoRecordIsOn = false;
			else
				IsoRecordIsOn = true;
		}


		public void GetStreamStatus()
		{
			var url = ApiPath + "StreamStatus";

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
				url = ApiPath + "StartStream";
			}
			else
			{
				url = ApiPath + "StopStream";
			}

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Message != "Streaming Started Successfully")
				StreamIsOn = false;
			else
				StreamIsOn = true;
		}


		public void GetOutputStatus()
		{
			var url = ApiPath + "OutputStatus";

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
				url = ApiPath + "StartOutput";
			}
			else
			{
				url = ApiPath + "StopOutput";
			}

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Message != "Outputing Started Successfully")
				OutputIsOn = false;
			else
				OutputIsOn = true;
		}


		public void GetLayouts()
		{
			var url = ApiPath + "GetLayouts";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Layouts != null)
				Layouts = res.Layouts;
		}


		public void GetLayoutStatus()
		{
			var url = ApiPath + "LayoutStatus";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Layout != null)
				Layout = res.Layout;
		}



		public void SetLayout(string layout)
		{
			var url = ApiPath + "ChangeLayout";

			var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(layout));

			if (res.Layout != null)
				Layout = res.Layout;
		}


		public void GetRoomConfigStatus()
		{
			var url = ApiPath + "RoomConfigStatus";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Layout != null)
				RoomConfig = res.Layout;
		}

		public void GetRoomConfigs()
		{
			var url = ApiPath + "GetRoomConfigs";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Layouts != null)
				RoomConfigs = res.Layouts;
		}

		public void SetRoomConfig(uint id)
		{
			var url = ApiPath + "ChangeRoomConfig";

			var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(id.ToString()));

			if (res.Layout != null)
				RoomConfig = res.Layout;
		}

		public void ForceSetRoomConfig(uint id)
		{
			var url = ApiPath + "ForceChangeRoomConfig";

			var res = MakeRequest<ResponseObjectBase, ID>(url, new ID(id.ToString()));

			if (res.Layout != null)
				RoomConfig = res.Layout;
		}

		public void GoHome()
		{
			var url = ApiPath + "GoHome";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);
		}


		public void GetCameras()
		{
			var url = ApiPath + "GetCameras";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Cameras != null)
				Cameras = res.Cameras;
		}

		public void GetCameraStatus()
		{
			var url = ApiPath + "CameraStatus";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Address != null)
				CameraAddress = System.Convert.ToInt32(res.Address);
		}

		public void SetCamera(uint address)
		{
			var url = ApiPath + "ManualSwitchCamera";

			var res = MakeRequest<ResponseObjectBase, CamAddress>(url, new CamAddress(address.ToString()));

			if (res.Layout != null)
				RoomConfig = res.Layout;
		}

		public void SetCameraPreset(uint camId, uint presetId)
		{
			var url = ApiPath + "CallCameraPreset";

			var res = MakeRequest<ResponseObjectBase, CameraPreset>(url, new CameraPreset(camId.ToString(), presetId.ToString()));

			if (!string.IsNullOrEmpty(res.Message))
				OnSuccessMessageReceived(res.Message);
		}

		public void SaveCameraPreset(uint camId, uint presetId)
		{
			var url = ApiPath + "SaveCameraPreset";

			var res = MakeRequest<ResponseObjectBase, CameraPreset>(url, new CameraPreset(camId.ToString(), presetId.ToString()));

			if (!string.IsNullOrEmpty(res.Message))
				OnSuccessMessageReceived(res.Message);
		}


		public void ImportCameraPresets()
		{
			var url = ApiPath + "ImportCameraPresets";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);
		}

		public void ExportCameraPresets()
		{
			var url = ApiPath + "ExportCameraPresets";

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
			var url = ApiPath + "CopyFiles";

			var res = MakeRequest<ResponseObjectBase, FilesParams>(url, new FilesParams(dest, logDest, delete));

			if (res.Status == "OK" && res.Message == "Successfully backed up information")
				OnFileCopySuccessful();
		}

		public void GetStorageSpaceAvailable()
		{
			var url = ApiPath + "StorageSpaceAvail";

			// TODO: Test the formatting of this command, API documentation is not clear on the value of the drives to send
			// https://sdkcon78221.crestron.com/sdk/Automate-VX-API/Content/Topics/Automate-API/API-Reference/StorageSpaceAvail-API.htm
			var res = MakeRequest<ResponseObjectBase, DrivesParams>(url, new DrivesParams("C:\\, D:\\, L:\\"));

			if (res.Drives != null)
				Drives = res.Drives;
		}

		public void GetRecordingSpaceAvailable()
		{
			var url = ApiPath + "RecodingSpaceAvail";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.AvailableGigabytes != null && res.TotalGigabytes != null)
			{
				RecordingSpace = new RecordingSpace(res.AvailableGigabytes, res.TotalGigabytes);
			}
		}


		public void SetSleep()
		{
			var url = ApiPath + "Sleep";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);
		}

		public void SetWake()
		{
			var url = ApiPath + "Wake";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);
		}


		public void Restart()
		{
			var url = ApiPath + "Restart";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);
		}


		public void SetCloseWirecast()
		{
			var url = ApiPath + "CloseWirecast";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);
		}


		public void GetScenarios()
		{
			var url = ApiPath + "GetScenarios";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Scenarios != null)
				Scenarios = res.Scenarios;
		}


		public void GetScenarioStatus()
		{
			var url = ApiPath + "ScenarioStatus";

			var res = MakeRequest<ResponseObjectBase, object>(url, null);

			if (res.Scenario != null)
				Scenario = res.Scenario;
		}

		public void SetScenario(uint id)
		{
			var path = ApiPath + "ChangeScenario";

			_client.SendRequest("GET", path, "");

			var res = MakeRequest<ResponseObjectBase, ID>(path, new ID(id.ToString()));

			if (res.Scenario != null)
				Scenario = res.Scenario;
		}


	}
}

