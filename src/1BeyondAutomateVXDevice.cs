using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using OneBeyondAutomateVxEpi.ApiObjects;
using OneBeyondAutomateVxEpi.GenericClients;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;


namespace OneBeyondAutomateVxEpi
{
	public class OneBeyondAutomateVx : EssentialsBridgeableDevice
	{
		private const string ApiPath = "/api";

		#region IRestfulComms

		private readonly IRestfulComms _client;

		private int _responseCode;
		private string _responseContent;
		private string _responseSuccessMessage;
		private string _responseErrorMessage;

		public int ResponseCode
		{
			get { return _responseCode; }
			set
			{
				_responseCode = value;
				Debug.Console(AutomateVxDebug.Verbose, this, "ResponseCode: {0}", _responseCode);
				//ResponseCodeFeedback.FireUpdate();
			}
		}

		public string ResponseContent
		{
			get { return _responseContent; }
			set
			{
				_responseContent = value;
				Debug.Console(AutomateVxDebug.Verbose, this, "ResponseContent: {0}", _responseContent);
				//ResponseContentFeedback.FireUpdate();
			}
		}

		public string ResponseSuccessMessage
		{
			get { return _responseSuccessMessage; }
			set
			{
				_responseSuccessMessage = value;
				Debug.Console(AutomateVxDebug.Verbose, this, "ResponseSuccessMessage: {0}", _responseSuccessMessage);
				ResponseSuccessMessageFeedback.FireUpdate();
			}
		}

		public string ResponseErrorMessage
		{
			get { return _responseErrorMessage; }
			set
			{
				_responseErrorMessage = value;
				Debug.Console(AutomateVxDebug.Verbose, this, "ResponseErrorMessage: {0}", _responseErrorMessage);
				ResponseErrorMessageFeedback.FireUpdate();
			}
		}

		public IntFeedback ResponseCodeFeedback { get; private set; }
		public StringFeedback ResponseContentFeedback { get; private set; }
		public StringFeedback ResponseSuccessMessageFeedback { get; private set; }
		public StringFeedback ResponseErrorMessageFeedback { get; private set; }

		#endregion

		private string _token;
		public string Token
		{
			get { return _token; }
			private set
			{
				if (value == _token) return;
				_token = value;
				LoginSuccessfulFeedback.FireUpdate();

				if (!string.IsNullOrEmpty(_token))
					Poll();
			}
		}

		private bool _autoSwitchIsOn;
		public bool AutoSwitchIsOn
		{
			get { return _autoSwitchIsOn; }
			private set
			{
				_autoSwitchIsOn = value;
				AutoSwitchIsOnFeedback.FireUpdate();
			}
		}

		private bool _outputIsOn;
		public bool OutputIsOn
		{
			get { return _outputIsOn; }
			private set
			{
				if (value == _outputIsOn) return;
				_outputIsOn = value;
				OutputIsOnFeedback.FireUpdate();
			}
		}

		private bool _streamIsOn;
		public bool StreamIsOn
		{
			get { return _streamIsOn; }
			private set
			{
				if (value == _streamIsOn) return;
				_streamIsOn = value;
				StreamIsOnFeedback.FireUpdate();
			}
		}

		private bool _recordIsOn;
		public bool RecordIsOn
		{
			get { return _recordIsOn; }
			private set
			{
				if (value == _recordIsOn) return;
				_recordIsOn = value;
				RecordIsOnFeedback.FireUpdate();
			}
		}

		private bool _isoRecordIsOn;
		public bool IsoRecordIsOn
		{
			get { return _isoRecordIsOn; }
			private set
			{
				if (value == _isoRecordIsOn) return;
				_isoRecordIsOn = value;
				IsoRecordIsOnFeedback.FireUpdate();
			}
		}

		private int _cameraAddress;
		public int CameraAddress
		{
			get { return _cameraAddress; }
			private set
			{
				if (value == _cameraAddress) return;
				_cameraAddress = value;
				CameraAddressFeedback.FireUpdate();
			}
		}

		private NameWithIdString _currentLayout;
		public NameWithIdString CurrentLayout
		{
			get { return _currentLayout; }
			set
			{
				if (value == _currentLayout) return;
				_currentLayout = value;
				CurrentLayoutNameFeedback.FireUpdate();
				CurrentLayoutIdFeedback.FireUpdate();
			}
		}

		private NameWithIdInt _currentRoomConfig;
		public NameWithIdInt CurrentRoomConfig
		{
			get { return _currentRoomConfig; }
			set
			{
				if (value == _currentRoomConfig) return;
				_currentRoomConfig = value;
				CurrentRoomConfigNameFeedback.FireUpdate();
				CurrentRoomConfigIdFeedback.FireUpdate();
			}
		}

		private NameWithIdInt _currentScenario;
		public NameWithIdInt CurrentScenario
		{
			get { return _currentScenario; }
			set
			{
				if (value == _currentScenario) return;
				_currentScenario = value;
				CurrentScenarioNameFeedback.FireUpdate();
				CurrentScenarioIdFeedback.FireUpdate();
			}
		}

		public BoolFeedback LoginSuccessfulFeedback;
		public BoolFeedback AutoSwitchIsOnFeedback;
		public BoolFeedback RecordIsOnFeedback;
		public BoolFeedback IsoRecordIsOnFeedback;
		public BoolFeedback StreamIsOnFeedback;
		public BoolFeedback OutputIsOnFeedback;
		public IntFeedback CameraAddressFeedback;
		public IntFeedback CamerasCountFeedback;
		public IntFeedback LayoutsCountFeedback;
		public StringFeedback CurrentLayoutNameFeedback;
		public IntFeedback CurrentLayoutIdFeedback;
		public IntFeedback RoomConfigsCountFeedback;
		public StringFeedback CurrentRoomConfigNameFeedback;
		public IntFeedback CurrentRoomConfigIdFeedback;
		public IntFeedback ScenariosCountFeedback;
		public StringFeedback CurrentScenarioNameFeedback;
		public IntFeedback CurrentScenarioIdFeedback;

		public List<Camera> Cameras { get; set; }
		public List<NameWithIdString> Layouts { get; set; }
		public List<NameWithIdInt> RoomConfigs { get; set; }
		public List<NameWithIdInt> Scenarios { get; set; }

		public event EventHandler CamerasChanged;
		public event EventHandler LayoutsChanged;
		public event EventHandler RoomConfigsChanged;
		public event EventHandler ScenariosChanged;

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
			Debug.Console(AutomateVxDebug.Trace, this, "Constructing new {0} instance", name);

			try
			{
				AutomateVxDebug.ResetDebugLevels();

				_client = client;
				if (_client == null)
				{
					Debug.Console(AutomateVxDebug.Trace, this, Debug.ErrorLogLevel.Error,
						"Failed to construct '{1}' using method {0}",
						config.Control.Method, name);
					return;
				}

				//ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
				//ResponseContentFeedback = new StringFeedback(() => ResponseContent);
				ResponseSuccessMessageFeedback = new StringFeedback(() => ResponseSuccessMessage);
				ResponseErrorMessageFeedback = new StringFeedback(() => ResponseErrorMessage);

				LoginSuccessfulFeedback = new BoolFeedback(() => !string.IsNullOrEmpty(Token));
				AutoSwitchIsOnFeedback = new BoolFeedback(() => AutoSwitchIsOn);
				RecordIsOnFeedback = new BoolFeedback(() => RecordIsOn);
				IsoRecordIsOnFeedback = new BoolFeedback(() => IsoRecordIsOn);
				StreamIsOnFeedback = new BoolFeedback(() => StreamIsOn);
				OutputIsOnFeedback = new BoolFeedback(() => OutputIsOn);
				CameraAddressFeedback = new IntFeedback(() => CameraAddress);
				CamerasCountFeedback = new IntFeedback(() => Cameras.Count);
				LayoutsCountFeedback = new IntFeedback(() => Layouts.Count);
				CurrentLayoutNameFeedback = new StringFeedback(() => CurrentLayout.Name);
				CurrentLayoutIdFeedback = new IntFeedback(() => ConvertIdToInt(CurrentLayout.Id));
				RoomConfigsCountFeedback = new IntFeedback(() => RoomConfigs.Count);
				CurrentRoomConfigNameFeedback = new StringFeedback(() => CurrentRoomConfig.Name);
				CurrentRoomConfigIdFeedback = new IntFeedback(() => CurrentRoomConfig.Id);
				ScenariosCountFeedback = new IntFeedback(() => Scenarios.Count);
				CurrentScenarioNameFeedback = new StringFeedback(() => CurrentScenario.Name);
				CurrentScenarioIdFeedback = new IntFeedback(() => CurrentScenario.Id);

				if (Cameras == null)
					Cameras = new List<Camera>();
				if (Layouts == null)
					Layouts = new List<NameWithIdString>();
				if (RoomConfigs == null)
					RoomConfigs = new List<NameWithIdInt>();
				if (Scenarios == null)
					Scenarios = new List<NameWithIdInt>();

				_client.ResponseReceived += OnResponseReceived;
			}
			catch (Exception ex)
			{
				Debug.Console(AutomateVxDebug.Notice, this, Debug.ErrorLogLevel.Error, "OneBeyondAutomateVx Exception Message: {0}", ex.Message);
				Debug.Console(AutomateVxDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OneBeyondAutomateVx Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(AutomateVxDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OneBeyondAutomateVx Inner Exception {0}", ex.InnerException);
			}
		}

		/// <summary>
		/// Initialize EPI
		/// </summary>
		public override void Initialize()
		{
			GetToken();
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
			//ResponseCodeFeedback.LinkInputSig(trilist.UShortInput[0]);
			//ResponseContentFeedback.LinkInputSig(trilist.StringInput[0]);
			ResponseSuccessMessageFeedback.LinkInputSig(trilist.StringInput[joinMap.SuccessMessage.JoinNumber]);
			ResponseErrorMessageFeedback.LinkInputSig(trilist.StringInput[joinMap.ErrorMessage.JoinNumber]);

			trilist.SetSigTrueAction(joinMap.Authenticate.JoinNumber, GetToken);
			LoginSuccessfulFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Authenticate.JoinNumber]);

			trilist.SetSigFalseAction(joinMap.Sleep.JoinNumber, SetSleep);
			trilist.SetSigFalseAction(joinMap.Wake.JoinNumber, SetWake);
			trilist.SetSigFalseAction(joinMap.GoHome.JoinNumber, GoHome);

			LinkAutoSwitchToApi(trilist, joinMap);
			LinkRecordToApi(trilist, joinMap);
			LinkIsoRecordToApi(trilist, joinMap);
			LinkOutputToApi(trilist, joinMap);
			LinkStreamToApi(trilist, joinMap);
			LinkCamerasToApi(trilist, joinMap);
			LinkLayoutsToApi(trilist, joinMap);
			LinkRoomConfigToApi(trilist, joinMap);
			LinkScenariosToApi(trilist, joinMap);

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				if (LoginSuccessfulFeedback != null)
					LoginSuccessfulFeedback.FireUpdate();

				if (AutoSwitchIsOnFeedback != null)
					AutoSwitchIsOnFeedback.FireUpdate();

				if (RecordIsOnFeedback != null)
					RecordIsOnFeedback.FireUpdate();

				if (IsoRecordIsOnFeedback != null)
					IsoRecordIsOnFeedback.FireUpdate();

				if (OutputIsOnFeedback != null)
					OutputIsOnFeedback.FireUpdate();

				if (StreamIsOnFeedback != null)
					StreamIsOnFeedback.FireUpdate();

				if (CamerasCountFeedback != null)
					CamerasCountFeedback.FireUpdate();

				if (CameraAddressFeedback != null)
					CameraAddressFeedback.FireUpdate();

				if (LayoutsCountFeedback != null)
					LayoutsCountFeedback.FireUpdate();

				if (CurrentLayoutIdFeedback != null)
					CurrentLayoutIdFeedback.FireUpdate();

				if (CurrentLayoutNameFeedback != null)
					CurrentLayoutNameFeedback.FireUpdate();

				if (RoomConfigsCountFeedback != null)
					RoomConfigsCountFeedback.FireUpdate();

				if (CurrentRoomConfigIdFeedback != null)
					CurrentRoomConfigIdFeedback.FireUpdate();

				if (CurrentRoomConfigNameFeedback != null)
					CurrentRoomConfigNameFeedback.FireUpdate();

				if (ScenariosCountFeedback != null)
					ScenariosCountFeedback.FireUpdate();

				if (CurrentScenarioIdFeedback != null)
					CurrentScenarioIdFeedback.FireUpdate();

				if (CurrentScenarioNameFeedback != null)
					CurrentScenarioNameFeedback.FireUpdate();
			};
		}

		private void LinkAutoSwitchToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.GetAutoSwitchStatus.JoinNumber, GetAutoSwitchStatus);
			trilist.SetSigFalseAction(joinMap.AutoSwitchOn.JoinNumber, () => SetAutoSwitch(true));
			trilist.SetSigFalseAction(joinMap.AutoSwitchOff.JoinNumber, () => SetAutoSwitch(false));

			AutoSwitchIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoSwitchOn.JoinNumber]);
			AutoSwitchIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoSwitchOff.JoinNumber]);
		}

		private void LinkRecordToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.GetRecordStatus.JoinNumber, GetRecordStatus);
			trilist.SetSigFalseAction(joinMap.RecordStart.JoinNumber, () => SetRecord(ERecordOperation.Start));
			trilist.SetSigFalseAction(joinMap.RecordStop.JoinNumber, () => SetRecord(ERecordOperation.Stop));
			trilist.SetSigFalseAction(joinMap.RecordPause.JoinNumber, () => SetRecord(ERecordOperation.Pause));

			RecordIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RecordStart.JoinNumber]);
			RecordIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.RecordStop.JoinNumber]);
		}

		private void LinkIsoRecordToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.GetIsoRecordStatus.JoinNumber, GetIsoRecordStatus);
			trilist.SetSigFalseAction(joinMap.IsoRecordOn.JoinNumber, () => SetIsoRecord(true));
			trilist.SetSigFalseAction(joinMap.IsoRecordOff.JoinNumber, () => SetIsoRecord(false));

			IsoRecordIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsoRecordOn.JoinNumber]);
			IsoRecordIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.IsoRecordOff.JoinNumber]);
		}

		private void LinkOutputToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.GetOutputStatus.JoinNumber, GetOutputStatus);
			trilist.SetSigFalseAction(joinMap.OutputOn.JoinNumber, () => SetOutput(true));
			trilist.SetSigFalseAction(joinMap.OutputOff.JoinNumber, () => SetOutput(false));

			OutputIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OutputOn.JoinNumber]);
			OutputIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.OutputOff.JoinNumber]);
		}

		private void LinkStreamToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			// stream
			trilist.SetSigFalseAction(joinMap.GetStreamStatus.JoinNumber, GetStreamStatus);
			trilist.SetSigFalseAction(joinMap.StreamOn.JoinNumber, () => SetStream(true));
			trilist.SetSigFalseAction(joinMap.StreamOff.JoinNumber, () => SetStream(false));

			StreamIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.StreamOn.JoinNumber]);
			StreamIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.StreamOff.JoinNumber]);
		}

		private void LinkCamerasToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.GetCameraStatus.JoinNumber, GetCameraStatus);
			trilist.SetSigFalseAction(joinMap.GetCameras.JoinNumber, GetCameras);
			trilist.SetUShortSigAction(joinMap.ChangeCamera.JoinNumber, (c) => SetCamera(c));
			trilist.SetUShortSigAction(joinMap.LiveCameraPreset.JoinNumber, (p) => SetCameraPreset((uint)CameraAddress, p));
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


			CamerasCountFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfCameras.JoinNumber]);
			CameraAddressFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChangeCamera.JoinNumber]);

			CamerasChanged += (o, a) => OnCamerasChanged(trilist, joinMap);
		}

		private void LinkLayoutsToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetSigFalseAction(joinMap.GetCurrentLayout.JoinNumber, GetLayoutStatus);
			trilist.SetSigFalseAction(joinMap.GetLayouts.JoinNumber, GetLayouts);
			trilist.SetUShortSigAction(joinMap.ChangeLayout.JoinNumber, SetLayout);

			LayoutsCountFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfLayouts.JoinNumber]);
			CurrentLayoutIdFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChangeLayout.JoinNumber]);
			CurrentLayoutNameFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentLayoutName.JoinNumber]);

			LayoutsChanged += (o, a) => OnLayoutsChanged(trilist, joinMap);
		}

		private void LinkRoomConfigToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetUShortSigAction(joinMap.ChangeRoomConfig.JoinNumber, (rc) => SetRoomConfig(rc));
			trilist.SetUShortSigAction(joinMap.ForceChangeRoomConfig.JoinNumber, (rc) => ForceSetRoomConfig(rc));

			RoomConfigsCountFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfRoomConfigs.JoinNumber]);
			CurrentRoomConfigIdFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChangeRoomConfig.JoinNumber]);
			CurrentRoomConfigNameFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentRoomConfigName.JoinNumber]);

			RoomConfigsChanged += (o, a) => OnRoomConfigsChanged(trilist, joinMap);
		}

		private void LinkScenariosToApi(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			trilist.SetUShortSigAction(joinMap.ChangeScenario.JoinNumber, (s) => SetScenario(s));

			ScenariosCountFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfScenarios.JoinNumber]);
			CurrentScenarioIdFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChangeScenario.JoinNumber]);
			CurrentScenarioNameFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentScenarioName.JoinNumber]);

			ScenariosChanged += (o, a) => OnScenariosChanged(trilist, joinMap);
		}

		#endregion

		private void OnResponseReceived(object sender, GenericClientResponseEventArgs args)
		{
			try
			{
				Debug.Console(AutomateVxDebug.Verbose, this,
					"OnResponseReceived: Request = {0} > Code = {1} | ContentString = {2}",
					args.Request, args.Code, args.ContentString);

				var request = args.Request.ToLower();
				var content = args.ContentString;

				switch (request)
				{
					case "get-token":
						{
							var response = ApiResponseParser.ParseTokenResponse(content);
							if (response.Status == "OK")
							{
								Token = response.Token;
								ResponseSuccessMessage = response.Message;
								return;
							}

							// if not OK, clear the token
							ResponseErrorMessage = response.Error;
							ClearToken();

							break;
						}
					case "autoswitchstatus":
						{
							var response = ApiResponseParser.ParseResultResponse(content);
							if (response.Status == "OK")
							{
								Debug.Console(AutomateVxDebug.Verbose, this, "OnResponseRecieved: 'autoswitchstatus' results {0}", response.Results.ToString());
								AutoSwitchIsOn = (response.Results == true);
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "startautoswitch":
					case "stopautoswitch":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								GetAutoSwitchStatus();
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "outputstatus":
						{
							var response = ApiResponseParser.ParseResultResponse(content);
							if (response.Status == "OK")
							{
								OutputIsOn = response.Results;
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "startouput":
					case "stopoutput":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								GetOutputStatus();
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "streamstatus":
						{
							var response = ApiResponseParser.ParseResultResponse(content);
							if (response.Status == "OK")
							{
								StreamIsOn = response.Results;
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "startstream":
					case "stopstream":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								GetStreamStatus();
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "recordstatus":
						{
							var response = ApiResponseParser.ParseRecordStatusResponse(content);
							if (response.Status == "OK")
							{
								RecordIsOn = (response.RecordState != 1);
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "isorecordstatus":
						{
							var response = ApiResponseParser.ParseResultResponse(content);
							if (response.Status == "OK")
							{
								IsoRecordIsOn = response.Results;
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "getcameras":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								Cameras = response.Cameras;
								CamerasCountFeedback.FireUpdate();

								var handler = CamerasChanged;
								if (handler != null)
								{
									handler(this, null);
								}

								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;
							break;
						}
					case "camerastatus":
						{
							var response = ApiResponseParser.ParseCameraAddressResponse(content);
							if (response.Status == "OK")
							{
								CameraAddress = Convert.ToInt16(response.Address);
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "manualswitchcamera":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								GetCameraStatus();
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "getlayouts":
						{
							var response = ApiResponseParser.ParseLayoutsResponse(content);
							if (response.Status == "OK")
							{
								Layouts = response.Layouts;
								LayoutsCountFeedback.FireUpdate();

								var handler = LayoutsChanged;
								if (handler != null)
								{
									handler(this, null);
								}

								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "layoutstatus":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								CurrentLayout = response.Layout;
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "changelayout":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								GetLayoutStatus();
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "getroomconfigs":
						{
							var response = ApiResponseParser.ParseRoomConfigsResponse(content);
							if (response.Status == "OK")
							{
								RoomConfigs = response.RoomConfigs;
								RoomConfigsCountFeedback.FireUpdate();

								var handler = RoomConfigsChanged;
								if (handler != null)
								{
									handler(this, null);
								}

								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "roomconfigstatus":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								CurrentRoomConfig = response.RoomConfig;
								ResponseSuccessMessage = response.Message;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "getscenarios":
						{
							var response = ApiResponseParser.ParseScenariosResponse(content);
							if (response.Status == "OK")
							{
								Scenarios = response.Scenarios;
								ScenariosCountFeedback.FireUpdate();

								var handler = ScenariosChanged;
								if (handler != null)
								{
									handler(this, null);
								}

								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "scenariostatus":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								CurrentScenario = response.Scenario;
								ResponseSuccessMessage = response.Message;

								GetAutoSwitchStatus();
								GetOutputStatus();
								GetStreamStatus();

								return;
							}

							ResponseErrorMessage = response.Error;

							break;
						}
					case "gotoscenario":
						{
							var response = ApiResponseParser.ParseRootResponse(content);
							if (response.Status == "OK")
							{
								GetScenarioStatus();
								ResponseSuccessMessage = response.Message;
								return;
							}

							ResponseErrorMessage = response.Message;
							break;
						}
					default:
						{
							ResponseCode = args.Code;
							ResponseContent = content;
							Debug.Console(AutomateVxDebug.Verbose, this, "OnResponseReceived: Code = {0}, Content = {1}", ResponseCode, ResponseContent);

							break;
						}
				}
			}
			catch (Exception ex)
			{
				Debug.Console(AutomateVxDebug.Notice, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Exception Message for request: {0}", args.Request);
				Debug.Console(AutomateVxDebug.Notice, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Exception Message: {0}", ex.Message);
				Debug.Console(AutomateVxDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(AutomateVxDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Inner Exception {0}", ex.InnerException);
			}
		}

		private void OnCamerasChanged(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (Cameras == null || Cameras.Count == 0)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "OnCamerasChanged: Cameras is null or has not entries");
				return;
			}

			foreach (var camera in Cameras)
			{
				var join = (uint)(joinMap.CameraModels.JoinNumber + camera.Id) - 1;
				var name = string.IsNullOrEmpty(camera.Model) 
					? "" : 
					string.Format("Camera {0} ({1})", camera.Id, camera.Model);

				trilist.SetString(join, name);
			}
		}

		private void OnLayoutsChanged(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (Layouts == null || Layouts.Count == 0)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "OnLayoutsChanged: Layouts is null or has not entries.");
				return;
			}

			foreach (var layout in Layouts)
			{
				var index = ConvertIdToInt(layout.Id);
				var join = (joinMap.LayoutNames.JoinNumber + (uint)index) - 1;
				var name = layout.Name ?? "";

				trilist.SetString(join, name);
			}
		}

		private void OnRoomConfigsChanged(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (RoomConfigs == null || RoomConfigs.Count == 0)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "OnRoomConfigsChanged: RoomConfigs is null or has not entries.");
				return;
			}

			foreach (var rc in RoomConfigs)
			{
				var join = (uint)(joinMap.RoomConfigNames.JoinNumber + rc.Id) - 1;
				var name = rc.Name ?? "";

				trilist.SetString(join, name);
			}
		}

		private void OnScenariosChanged(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
		{
			if (Scenarios == null || Scenarios.Count == 0)
			{
				Debug.Console(AutomateVxDebug.Verbose, this, "OnScenariosChanged: Scenarios is null or has not entries.");
				return;
			}

			foreach (var scenario in Scenarios)
			{
				var join = (uint)(joinMap.ScenarioNames.JoinNumber + scenario.Id) - 1;
				var name = scenario.Name ?? "";

				trilist.SetString(join, name);
			}
		}

		/// <summary>
		/// Clear existing token
		/// </summary>
		public void ClearToken()
		{
			Token = null;
		}

		/// <summary>
		/// Attempts to get an authorization token
		/// </summary>
		public void GetToken()
		{
			_client.SendRequest("POST", "Get-Token", string.Empty);
		}

		/// <summary>
		/// Poll device
		/// </summary>
		public void Poll()
		{
			GetAutoSwitchStatus();
			CrestronEnvironment.Sleep(100);
			GetOutputStatus();
			CrestronEnvironment.Sleep(100);
			GetStreamStatus();
			CrestronEnvironment.Sleep(100);
			GetRoomConfigs();
			CrestronEnvironment.Sleep(100);
			GetLayouts();
			CrestronEnvironment.Sleep(100);
			GetScenarios();
			CrestronEnvironment.Sleep(100);
			GetCameras();
			CrestronEnvironment.Sleep(100);
			GetRoomConfigStatus();
			CrestronEnvironment.Sleep(100);
			GetLayoutStatus();
			CrestronEnvironment.Sleep(100);
			GetScenarioStatus();
			CrestronEnvironment.Sleep(100);
			GetCameraStatus();
		}

		/// <summary>
		/// Get current auto switch status
		/// </summary>
		public void GetAutoSwitchStatus()
		{
			var url = string.Format("{0}/AutoSwitchStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Set auto switch status
		/// </summary>
		/// <param name="state">bool</param>
		public void SetAutoSwitch(bool state)
		{
			var url = state
				? string.Format("{0}/StartAutoSwitch", ApiPath)
				: string.Format("{0}/StopAutoSwitch", ApiPath);

			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get current record status
		/// </summary>
		public void GetRecordStatus()
		{
			var url = string.Format("{0}/RecordStatusResponse", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Record operation states
		/// </summary>
		public enum ERecordOperation
		{
			Start = 0,
			Stop = 1,
			Pause = 2,
		}

		/// <summary>
		/// Set the record operation state
		/// </summary>
		/// <param name="operation">ERecordOperatoin</param>
		public void SetRecord(ERecordOperation operation)
		{
			var url = string.Empty;

			switch (operation)
			{
				case ERecordOperation.Start:
					url = string.Format("{0}/StartRecord", ApiPath);
					break;
				case ERecordOperation.Pause:
					url = string.Format("{0}/PauseRecord", ApiPath);
					break;
				case ERecordOperation.Stop:
					url = string.Format("{0}/StopRecord", ApiPath);
					break;
			}

			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get ISO Record state
		/// </summary>
		public void GetIsoRecordStatus()
		{
			var url = string.Format("{0}/ISORecordStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Set the ISO Record state
		/// </summary>
		/// <param name="state"></param>
		public void SetIsoRecord(bool state)
		{
			var url = (state)
				? string.Format("{0}/StartISORecord", ApiPath)
				: string.Format("{0}/StopISORecord", ApiPath);

			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the current stream state
		/// </summary>
		public void GetStreamStatus()
		{
			var url = string.Format("{0}/StreamStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Set the stream state
		/// </summary>
		/// <param name="state">bool</param>
		public void SetStream(bool state)
		{
			var url = (state)
				? string.Format("{0}/StartStream", ApiPath)
				: string.Format("{0}/StopStream", ApiPath);

			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the current output state
		/// </summary>
		public void GetOutputStatus()
		{
			var url = string.Format("{0}/OutputStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Set the output state
		/// </summary>
		/// <param name="state"></param>
		public void SetOutput(bool state)
		{
			var url = (state)
				? string.Format("{0}/StartOutput", ApiPath)
				: string.Format("{0}/StopOutput", ApiPath);

			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the configured layouts
		/// </summary>
		public void GetLayouts()
		{
			var url = string.Format("{0}/GetLayouts", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the current layout
		/// </summary>
		public void GetLayoutStatus()
		{
			var url = string.Format("{0}/LayoutStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Set the layout
		/// </summary>
		/// <param name="layout"></param>
		public void SetLayout(ushort layout)
		{
			// Check for valid input (1-26)
			if (layout < 1 || layout > 26)
				return;

			var c = ConvertIdToString(layout);

			var url = string.Format("{0}/ChangeLayout", ApiPath);
			var jo = new
			{
				id = c
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		private char ConvertIdToString(int id)
		{
			return (char)(id + 64);
		}

		private int ConvertIdToInt(string id)
		{
			var i = id.ToCharArray();
			return Convert.ToInt16(i[0]) - 64;
		}

		/// <summary>
		/// Get current room configuration 
		/// </summary>
		public void GetRoomConfigStatus()
		{
			var url = string.Format("{0}/RoomConfigStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the available room configurations
		/// </summary>
		public void GetRoomConfigs()
		{
			var url = string.Format("{0}/GetRoomConfigs", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Set the room configuration
		/// </summary>
		/// <param name="configId"></param>
		public void SetRoomConfig(uint configId)
		{
			var url = string.Format("{0}/ChangeRoomConfiguration", ApiPath);
			var jo = new
			{
				id = configId
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Force set the room configuration
		/// </summary>
		/// <param name="configId"></param>
		public void ForceSetRoomConfig(uint configId)
		{
			var url = string.Format("{0}/ForceChangeRoomConfig", ApiPath);
			var jo = new
			{
				id = configId
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Set all cameras to the home position
		/// </summary>
		public void GoHome()
		{
			var url = string.Format("{0}/GoHome", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the available cameras
		/// </summary>
		public void GetCameras()
		{
			var url = string.Format("{0}/GetCameras", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Get the camera status
		/// </summary>
		public void GetCameraStatus()
		{
			var url = string.Format("{0}/CameraStatus", ApiPath);
			_client.SendRequest("POST", url, string.Empty);
		}

		/// <summary>
		/// Manually switch the camera
		/// </summary>
		/// <param name="cameraAddress"></param>
		public void SetCamera(uint cameraAddress)
		{
			var url = string.Format("{0}/ManualSwitchCamera", ApiPath);
			var jo = new
			{
				address = cameraAddress.ToString()
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Recall camera preset
		/// </summary>
		/// <param name="camId"></param>
		/// <param name="presetId"></param>
		public void SetCameraPreset(uint camId, uint presetId)
		{
			var url = string.Format("{0}/CallCameraPreset", ApiPath);
			var jo = new
			{
				cam = camId.ToString(),
				pre = presetId.ToString()
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Save camera preset
		/// </summary>
		/// <param name="camId"></param>
		/// <param name="presetId"></param>
		public void SaveCameraPreset(uint camId, uint presetId)
		{
			var url = string.Format("{0}/SaveCameraPreset", ApiPath);
			var jo = new
			{
				cam = camId.ToString(),
				pre = presetId.ToString()
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Import camera presets
		/// </summary>
		public void ImportCameraPresets()
		{
			var url = string.Format("{0}/ImportCameraPresets", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Export camera presets
		/// </summary>
		public void ExportCameraPresets()
		{
			var url = string.Format("{0}/ExportCameraPresets", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Copy Files on teh device
		/// </summary>
		/// <param name="dest">Destination folder to copy files to</param>
		/// <param name="logDest">Destination for log.  Leave black to turn off logging</param>
		/// <param name="delete">If true, delete source files after copy</param>
		public void CopyFiles(string dest, string logDest, bool delete)
		{
			var url = string.Format("{0}/CopyFiles", ApiPath);
			var jo = new
			{
				destination = dest,
				logDestination = logDest,
				deleteSource = delete
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Get available storage space
		/// </summary>
		/// <param name="driveLetters"></param>
		public void GetStorageSpaceAvailable(string driveLetters)
		{
			if (String.IsNullOrEmpty(driveLetters))
			{
				driveLetters = "C:\\, D:\\, L:\\";
			}

			var url = string.Format("{0}/StorageSpaceAvail", ApiPath);
			var jo = new
			{
				drives = driveLetters
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Get available recording space available
		/// </summary>
		public void GetRecordingSpaceAvailable()
		{
			var url = string.Format("{0}/RecodingSpaceAvail", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Set the system to the sleep state
		/// </summary>
		public void SetSleep()
		{
			var url = string.Format("{0}/Sleep", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Wake the system from sleep state
		/// </summary>
		public void SetWake()
		{
			var url = string.Format("{0}/Wake", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Restart the system
		/// </summary>
		public void Restart()
		{
			var url = string.Format("{0}/Restart", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Close the wirecast
		/// </summary>
		public void SetCloseWirecast()
		{
			var url = string.Format("{0}/CloseWirecast", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Get available scenarios
		/// </summary>
		public void GetScenarios()
		{
			var url = string.Format("{0}/GetScenarios", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Get current scenario status
		/// </summary>
		public void GetScenarioStatus()
		{
			var url = string.Format("{0}/ScenarioStatus", ApiPath);
			var content = string.Empty;
			_client.SendRequest("POST", url, content);
		}

		/// <summary>
		/// Set the scenario
		/// </summary>
		/// <param name="scenarioId"></param>
		public void SetScenario(uint scenarioId)
		{
			var url = string.Format("{0}/GoToScenario", ApiPath);
			var jo = new
			{
				id = scenarioId
			};
			var content = JsonConvert.SerializeObject(jo);
			_client.SendRequest("POST", url, content);
		}
	}
}

