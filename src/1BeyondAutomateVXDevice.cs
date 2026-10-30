using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using OneBeyondAutomateVxEpi.ApiObjects;
using OneBeyondAutomateVxEpi.GenericClients;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.Cameras;
using System;
using System.Collections.Generic;
using System.Linq;
using ApiCamera = OneBeyondAutomateVxEpi.ApiObjects.Camera;

namespace OneBeyondAutomateVxEpi
{
    public class OneBeyondAutomateVx : EssentialsBridgeableDevice, IHasCamerasWithControls, IHasCameraAutoMode, IHasPowerControl
    {
        private const string ApiPath = "/api";

        private const int DefaultPollIntervalSeconds = 30;
        private const int MinPollIntervalSeconds = 10;

        // A scenario change seen within this window of our own GoToScenario is treated as ours
        private const double CommandedScenarioWindowSeconds = 120;

        // Requests Poll() re-sends after a new token, so they are not retried individually
        private static readonly HashSet<string> PolledRequests = new HashSet<string>
        {
            "autoswitchstatus", "outputstatus", "streamstatus", "getroomconfigs", "getlayouts", "getscenarios",
            "getcameras", "roomconfigstatus", "layoutstatus", "scenariostatus", "camerastatus"
        };

        private CTimer _pollTimer;

        // Rejections arriving this soon after a new token are for requests sent with the previous one
        private const double TokenGraceSeconds = 5;

        private readonly object _authLock = new object();
        private bool _reauthInProgress;
        private DateTime _tokenTime;
        private readonly Dictionary<string, KeyValuePair<string, string>> _lastRequests = new Dictionary<string, KeyValuePair<string, string>>();
        private readonly List<string> _pendingRetries = new List<string>();
        private readonly HashSet<string> _retriedRequests = new HashSet<string>();

        private readonly object _scenarioLock = new object();
        private int? _commandedScenarioId;
        private DateTime _commandedScenarioTime;

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
                Debug.LogVerbose(this, "ResponseCode: {0}", _responseCode);
                if (ResponseCodeFeedback != null)
                    ResponseCodeFeedback.FireUpdate();
            }
        }

        public string ResponseContent
        {
            get { return _responseContent; }
            set
            {
                _responseContent = value;
                Debug.LogVerbose(this, "ResponseContent: {0}", _responseContent);
                if (ResponseContentFeedback != null)
                    ResponseContentFeedback.FireUpdate();
            }
        }

        public string ResponseSuccessMessage
        {
            get { return _responseSuccessMessage; }
            set
            {
                _responseSuccessMessage = value;
                Debug.LogVerbose(this, "ResponseSuccessMessage: {0}", _responseSuccessMessage);
                ResponseSuccessMessageFeedback.FireUpdate();
            }
        }

        public string ResponseErrorMessage
        {
            get { return _responseErrorMessage; }
            set
            {
                _responseErrorMessage = value;
                Debug.LogVerbose(this, "ResponseErrorMessage: {0}", _responseErrorMessage);
                ResponseErrorMessageFeedback.FireUpdate();
            }
        }

        public IntFeedback ResponseCodeFeedback { get; private set; }
        public StringFeedback ResponseContentFeedback { get; private set; }
        public StringFeedback ResponseSuccessMessageFeedback { get; private set; }
        public StringFeedback ResponseErrorMessageFeedback { get; private set; }

        private ScenariosSelectableItems ScenariosSelectableItems;

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
                if (_autoSwitchIsOn == value) return;
                _autoSwitchIsOn = value;
                AutoSwitchIsOnFeedback.FireUpdate();
                CameraAutoModeIsOnFeedback.FireUpdate();
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

                if (_cameras.ContainsKey((uint)_cameraAddress))
                {
                    SelectedCamera = _cameras[(uint)_cameraAddress];
                }
                else
                {
                    SelectedCamera = null;
                }
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

                if (_currentScenario == null) return;

                if (ScenariosSelectableItems == null) return;

                var scenario = ScenariosSelectableItems.Items.Values.FirstOrDefault(s => s.Key == _currentScenario.Id.ToString());

                if (scenario != null)
                {
                    ScenariosSelectableItems.CurrentItem = scenario.Key;
                }

                foreach (var item in ScenariosSelectableItems.Items) 
                {
                    var scenarioItem = item.Value as ScenariosSelectableItem;
                    if (scenarioItem != null)
                    {
                        scenarioItem.UpdateSelectedFromFeedback(_currentScenario.Id);
                    }
                }
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


        private List<ApiCamera> ApiCameras { get; set; }
        public List<NameWithIdString> Layouts { get; set; }
        public List<NameWithIdInt> RoomConfigs { get; set; }
        public List<NameWithIdInt> Scenarios { get; set; }

        public BoolFeedback CameraAutoModeIsOnFeedback { get; private set; }

        /// <summary>
        /// Collection of cameras indexed by their address.
        /// </summary>
        private Dictionary<uint, IHasCameraControls> _cameras = new Dictionary<uint, IHasCameraControls>();

        public List<IHasCameraControls> Cameras => _cameras.Values.ToList();

        private IHasCameraControls _selectedCamera;

        public IHasCameraControls SelectedCamera
        {
            get
            {
                return _selectedCamera;
            }
            private set
            {
                if(value == _selectedCamera) return;

                _selectedCamera = value;
                SelectedCameraFeedback.FireUpdate();
                CameraSelected?.Invoke(this, new CameraSelectedEventArgs<IHasCameraControls>(_selectedCamera));
            }
        }

        public StringFeedback SelectedCameraFeedback { get; private set; }

        public event EventHandler CamerasChanged;
        public event EventHandler LayoutsChanged;
        public event EventHandler RoomConfigsChanged;
        public event EventHandler ScenariosChanged;
        public event EventHandler<CameraSelectedEventArgs<IHasCameraControls>> CameraSelected;

        /// <summary>
        /// Plugin device constructor for devices that need IBasicCommunication
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <param name="client"></param>
        /// 
        private readonly OneBeyondAutomateVxConfig _config;


        public OneBeyondAutomateVx(string key, string name, OneBeyondAutomateVxConfig config, IRestfulComms client)
            : base(key, name)
        {
            Debug.LogInformation(this, "Constructing new {0} instance", name);

            try
            {
                _client = client;
                _config = config;

                if (_client == null)
                {
                    Debug.LogError(this, "Failed to construct '{1}' using method {0}",
                        config.Control.Method, name);
                    return;
                }

                //ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
                //ResponseContentFeedback = new StringFeedback(() => ResponseContent);
                ResponseSuccessMessageFeedback = new StringFeedback("responseSuccessMessageFeedback", () => ResponseSuccessMessage);
                ResponseErrorMessageFeedback = new StringFeedback("responseErrorMessageFeedback", () => ResponseErrorMessage);

                LoginSuccessfulFeedback = new BoolFeedback("loginSuccessfulFeedback", () => !string.IsNullOrEmpty(Token));
                AutoSwitchIsOnFeedback = new BoolFeedback("autoSwitchIsOnFeedback", () => AutoSwitchIsOn);
                RecordIsOnFeedback = new BoolFeedback("recordIsOnFeedback", () => RecordIsOn);
                IsoRecordIsOnFeedback = new BoolFeedback("isoRecordIsOnFeedback", () => IsoRecordIsOn);
                StreamIsOnFeedback = new BoolFeedback("streamIsOnFeedback", () => StreamIsOn);
                OutputIsOnFeedback = new BoolFeedback("outputIsOnFeedback", () => OutputIsOn);
                CameraAddressFeedback = new IntFeedback("cameraAddressFeedback", () => CameraAddress);
                CamerasCountFeedback = new IntFeedback("camerasCountFeedback", () => ApiCameras.Count);
                LayoutsCountFeedback = new IntFeedback("layoutsCountFeedback", () => Layouts.Count);
                CurrentLayoutNameFeedback = new StringFeedback("currentLayoutNameFeedback", () => CurrentLayout.Name);
                CurrentLayoutIdFeedback = new IntFeedback("currentLayoutIdFeedback", () => ConvertIdToInt(CurrentLayout.Id));
                RoomConfigsCountFeedback = new IntFeedback("roomConfigsCountFeedback", () => RoomConfigs.Count);
                CurrentRoomConfigNameFeedback = new StringFeedback("currentRoomConfigNameFeedback", () => CurrentRoomConfig.Name);
                CurrentRoomConfigIdFeedback = new IntFeedback("currentRoomConfigIdFeedback", () => CurrentRoomConfig.Id);
                ScenariosCountFeedback = new IntFeedback("scenariosCountFeedback", () => Scenarios.Count);
                CurrentScenarioNameFeedback = new StringFeedback("currentScenarioNameFeedback", () => CurrentScenario != null ? CurrentScenario.Name : string.Empty);
                CurrentScenarioIdFeedback = new IntFeedback("currentScenarioIdFeedback", () => CurrentScenario != null ? CurrentScenario.Id : 0);
                CameraAutoModeIsOnFeedback = new BoolFeedback("cameraAutoModeIsOnFeedback", () => AutoSwitchIsOn);
                SelectedCameraFeedback = new StringFeedback("selectedCameraFeedback", () => _selectedCamera?.Key ?? string.Empty);

                if (ApiCameras == null)
                    ApiCameras = new List<ApiCamera>();
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
                Debug.LogError(this, "OneBeyondAutomateVx Exception Message: {0}", ex.Message);
                Debug.LogError(this, "OneBeyondAutomateVx Stack Trace: {0}", ex.StackTrace);
                if (ex.InnerException != null) Debug.LogError(this, "OneBeyondAutomateVx Inner Exception {0}", ex.InnerException);
            }
        }


        protected override void CreateMobileControlMessengers()
        {
            this.LogInformation("Adding Mobile Control Messengers for 1Beyond");
            var mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();
            if (mc == null)
            {
                this.LogError("Unable to find Mobile Control device");
                return;
            }

            var iHasCameraAutoModeMessenger = new IHasCameraAutoModeMessenger($"{Key}-cameraAutoMode", $"/device/{Key}", this);
            mc.AddDeviceMessenger(iHasCameraAutoModeMessenger);

            //var selectableItems = new Dictionary<string, ISelectableItem>();
            //if (_config?.Cameras != null)
            //    {

            //    foreach (var camera in _config.Cameras.Values)
            //        {
            //        // Add each camera to the selectableItems dictionary
            //        selectableItems[camera.Id.ToString()] = new CameraSelectableItems.CameraSelectableItem(
            //            camera.Id.ToString(), camera.Name, camera.Id, this);
            //        }
            //    }
            //var cameraItems = new CameraSelectableItems($"{Key}-cameraItems", "Camera Items", selectableItems);
            //var cameraSelectMessenger = new ISelectableItemsMessenger<string>($"{Key}-cameraManualSelect", $"/device/{Key}", cameraItems, "selectedCamera");
            //mc.AddDeviceMessenger(cameraSelectMessenger);

            if (Scenarios != null)
            {
                var selectableScenarios = new Dictionary<string, ISelectableItem>();
                foreach (var scenario in Scenarios)
                {
                    selectableScenarios[scenario.Id.ToString()] = new ScenariosSelectableItem(
                        scenario.Id.ToString(), scenario.Name, scenario.Id, this);
                }

                ScenariosSelectableItems = new ScenariosSelectableItems($"{Key}-scenarioItems", "Scenario Items", selectableScenarios);

                ScenariosChanged += (o, a) =>
                {
                    var scenarios = new Dictionary<string, ISelectableItem>();
                    foreach (var scenario in Scenarios)
                    {
                        scenarios[scenario.Id.ToString()] = new ScenariosSelectableItem(
                            scenario.Id.ToString(), scenario.Name, scenario.Id, this);
                    }

                    ScenariosSelectableItems.Items = scenarios;
                };

                var scenarioSelectMessenger = new ISelectableItemsMessenger<string>($"{Key}-scenarioSelect", $"/device/{Key}", ScenariosSelectableItems, "selectedScenario");
                mc.AddDeviceMessenger(scenarioSelectMessenger);
            }

            //if (RoomConfigs != null)
            //{
            //    var selectableConfigs = new Dictionary<string, ISelectableItem>();
            //    foreach (var config in RoomConfigs)
            //    {
            //        selectableConfigs[config.Id.ToString()] = new ConfigurationsSelectableItems.ConfigurationsSelectableItem(
            //            config.Id.ToString(), config.Name, config.Id, this);
            //    }

            //    var configItems = new ConfigurationsSelectableItems($"{Key}-configItems", "Configuration Items", selectableConfigs);
            //    var configSelectMessenger = new ISelectableItemsMessenger<string>($"{Key}-configSelect", $"/device/{Key}", configItems, "selectedConfig");
            //    mc.AddDeviceMessenger(configSelectMessenger);
            //}
        }

        private void OneBeyondAutomateVx_ScenariosChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Initialize EPI
        /// </summary>
        public override void Initialize()
        {
            GetToken();

            SetupCameras();

            StartPollTimer();

            CrestronEnvironment.ProgramStatusEventHandler += (type) =>
            {
                if (type != eProgramStatusEventType.Stopping || _pollTimer == null) return;

                _pollTimer.Stop();
                _pollTimer.Dispose();
            };
        }

        private void StartPollTimer()
        {
            var seconds = _config.PollIntervalSeconds ?? DefaultPollIntervalSeconds;

            if (seconds <= 0)
            {
                this.LogInformation("Scenario status polling disabled (pollIntervalSeconds = {seconds})", seconds);
                return;
            }

            if (seconds < MinPollIntervalSeconds)
            {
                this.LogWarning("pollIntervalSeconds {seconds} is below the minimum, using {minimum}", seconds, MinPollIntervalSeconds);
                seconds = MinPollIntervalSeconds;
            }

            this.LogInformation("Polling scenario status every {seconds} s", seconds);

            var intervalMs = seconds * 1000L;
            _pollTimer = new CTimer((o) => OnPollTimer(), null, intervalMs, intervalMs);
        }

        private void OnPollTimer()
        {
            try
            {
                // no token yet (VX unreachable at startup, or a re-auth that got no answer), keep trying
                if (string.IsNullOrEmpty(Token))
                {
                    this.LogDebug("Poll timer: no token, requesting one");
                    GetToken();
                    return;
                }

                GetScenarioStatus();
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Poll timer exception: {message}", ex.Message);
            }
        }

        private void SetupCameras()
        {
            foreach (var camera in _config.Cameras)
            {
                var cam = DeviceManager.GetDeviceForKey<IHasCameraControls>(camera.DeviceKey);

                if (cam == null)
                {
                    Debug.LogError(this, "Camera with key '{0}' not found in DeviceManager", camera.DeviceKey);
                    continue;
                }

                _cameras[camera.Id] = cam;
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

            Debug.LogDebug("Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.LogInformation("Linking to Bridge Type {0}", GetType().Name);

            // Linked Feedbacks
            if (ResponseCodeFeedback != null)
                ResponseCodeFeedback.LinkInputSig(trilist.UShortInput[0]);
            if (ResponseContentFeedback != null)
                ResponseContentFeedback.LinkInputSig(trilist.StringInput[0]);

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

                Poll();
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
                    Debug.LogInformation(this,
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
                Debug.LogVerbose(this,
                    "OnResponseReceived: Request = {0} > Code = {1} | ContentString = {2}",
                    args.Request, args.Code, args.ContentString);

                var request = args.Request.ToLower();
                var content = args.ContentString;

                if (request != "get-token")
                {
                    if (IsAuthFailure(args.Code, content))
                    {
                        HandleAuthFailure(request, args.Code);
                        return;
                    }

                    lock (_authLock)
                    {
                        _retriedRequests.Remove(request);
                    }
                }

                switch (request)
                {
                    case "get-token":
                        {
                            var response = args.Code == 200 ? ApiResponseParser.ParseTokenResponse(content) : null;
                            if (response != null && response.Status == "OK")
                            {
                                List<string> retries;
                                lock (_authLock)
                                {
                                    _reauthInProgress = false;
                                    _tokenTime = DateTime.Now;
                                    retries = new List<string>(_pendingRetries);
                                    _pendingRetries.Clear();
                                    foreach (var retry in retries)
                                        _retriedRequests.Add(retry);
                                }

                                // a new token runs Poll(), which refreshes all status
                                Token = response.Token;
                                ResponseSuccessMessage = response.Message;

                                RetryRequests(retries);
                                return;
                            }

                            // if not OK, clear the token
                            lock (_authLock)
                            {
                                _reauthInProgress = false;
                                if (_pendingRetries.Count > 0)
                                    this.LogError("Get-Token failed (code {code}), dropping retry of {requests:l}", args.Code, string.Join(", ", _pendingRetries.ToArray()));
                                _pendingRetries.Clear();
                            }

                            this.LogWarning("Get-Token failed: code {code}, content {content}", args.Code, content);
                            ResponseErrorMessage = response != null ? response.Error : content;
                            ClearToken();

                            break;
                        }
                    case "autoswitchstatus":
                        {
                            var response = ApiResponseParser.ParseResultResponse(content);
                            if (response.Status == "OK")
                            {
                                Debug.LogVerbose(this, "OnResponseReceived: 'autoswitchstatus' results {0}", response.Results.ToString());
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
                            var response = ApiResponseParser.ParseCamerasResponse(content);
                            if (response.Status == "OK")
                            {
                                ApiCameras = response.Cameras;
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
                                var previousScenario = CurrentScenario;
                                CurrentScenario = response.Scenario;
                                LogScenarioChange(previousScenario, response.Scenario);
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
                            Debug.LogVerbose(this, "OnResponseReceived: Code = {0}, Content = {1}", ResponseCode, ResponseContent);

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "OnResponseReceived Exception Message for request: {0}", args.Request);
                Debug.LogError(this, "OnResponseReceived Exception Message: {0}", ex.Message);
                Debug.LogError(this, "OnResponseReceived Stack Trace: {0}", ex.StackTrace);
                if (ex.InnerException != null) Debug.LogError(this, "OnResponseReceived Inner Exception {0}", ex.InnerException);
            }
        }

        private void OnCamerasChanged(BasicTriList trilist, OneBeyondAutomateVxBridgeJoinMap joinMap)
        {
            if (ApiCameras == null || ApiCameras.Count == 0)
            {
                Debug.LogVerbose(this, "OnCamerasChanged: Cameras is null or has not entries");
                return;
            }

            foreach (var camera in ApiCameras)
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
                Debug.LogVerbose(this, "OnLayoutsChanged: Layouts is null or has not entries.");
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
                Debug.LogVerbose(this, "OnRoomConfigsChanged: RoomConfigs is null or has not entries.");
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
                Debug.LogVerbose(this, "OnScenariosChanged: Scenarios is null or has not entries.");
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
            _client.ClearToken();
            Token = null;
        }

        /// <summary>
        /// Attempts to get an authorization token
        /// </summary>
        public void GetToken()
        {
            _client.SendRequest("POST", "Get-Token", string.Empty);
        }

        // Sends an API request and remembers it so it can be retried once after re-authenticating.
        // Without a token the request is not sent (the client would send credentials as the token and
        // draw a 401); polled requests are covered by the Poll() that follows Get-Token, commands are queued.
        private void SendRequest(string path, string content)
        {
            var request = path.Substring(path.LastIndexOf('/') + 1).ToLower();
            var requestToken = false;

            lock (_authLock)
            {
                _lastRequests[request] = new KeyValuePair<string, string>(path, content);

                if (string.IsNullOrEmpty(Token))
                {
                    if (!PolledRequests.Contains(request) && !_pendingRetries.Contains(request))
                        _pendingRetries.Add(request);

                    requestToken = !_reauthInProgress;
                    _reauthInProgress = true;
                }
            }

            if (string.IsNullOrEmpty(Token))
            {
                this.LogDebug("No token, holding {request:l} until Get-Token completes", request);
                if (requestToken) GetToken();
                return;
            }

            _client.SendRequest("POST", path, content);
        }

        // The VX rejects a stale token after it restarts. The exact response isn't documented,
        // so treat 401/403, or a non-OK body whose error mentions the token, as an auth failure.
        private bool IsAuthFailure(int code, string content)
        {
            if (code == 401 || code == 403) return true;

            if (code != 200 || string.IsNullOrEmpty(content)) return false;

            try
            {
                var response = ApiResponseParser.ParseRootResponse(content);
                if (response == null || response.Status == "OK") return false;

                var text = string.Format("{0} {1}", response.Error, response.Message).ToLower();
                return text.Contains("token") || text.Contains("auth");
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void HandleAuthFailure(string request, int code)
        {
            bool sentBeforeCurrentToken;
            bool startReauth = false;

            lock (_authLock)
            {
                if (_retriedRequests.Remove(request))
                {
                    this.LogError("{request:l} rejected again after re-authenticating (code {code}), not retrying", request, code);
                    return;
                }

                // Requests already in flight when the token was replaced come back rejected; that doesn't
                // mean the new token is stale, so don't re-authenticate again for them.
                sentBeforeCurrentToken = (DateTime.Now - _tokenTime).TotalSeconds < TokenGraceSeconds;

                if (!sentBeforeCurrentToken)
                {
                    if (!PolledRequests.Contains(request) && !_pendingRetries.Contains(request))
                        _pendingRetries.Add(request);

                    startReauth = !_reauthInProgress;
                    _reauthInProgress = true;
                }
            }

            if (sentBeforeCurrentToken)
            {
                this.LogDebug("{request:l} rejected (code {code}), sent before the current token", request, code);

                // polled requests are refreshed by the Poll() that followed the new token, a command is re-sent once
                if (!PolledRequests.Contains(request))
                {
                    lock (_authLock)
                    {
                        _retriedRequests.Add(request);
                    }
                    RetryRequests(new List<string> { request });
                }
                return;
            }

            if (!startReauth) return;

            this.LogWarning("{request:l} rejected as unauthorized (code {code}), token is stale (VX restarted?). Re-acquiring token", request, code);

            ClearToken();
            GetToken();
        }

        private void RetryRequests(List<string> requests)
        {
            foreach (var request in requests)
            {
                KeyValuePair<string, string> last;
                lock (_authLock)
                {
                    if (!_lastRequests.TryGetValue(request, out last)) continue;
                }

                this.LogInformation("Retrying {request:l} with new token", request);
                SendRequest(last.Key, last.Value);
            }
        }

        // Logs scenario changes, flagging any we didn't command (VX restart, change made on the VX itself)
        private void LogScenarioChange(NameWithIdInt previous, NameWithIdInt current)
        {
            if (current == null) return;

            bool commandedByUs;
            int? commandedId;
            lock (_scenarioLock)
            {
                commandedId = _commandedScenarioId;
                commandedByUs = _commandedScenarioId == current.Id
                    && (DateTime.Now - _commandedScenarioTime).TotalSeconds <= CommandedScenarioWindowSeconds;

                if (_commandedScenarioId == current.Id)
                    _commandedScenarioId = null;
            }

            if (previous == null)
            {
                this.LogInformation("Current scenario: {id} {name:l}", current.Id, current.Name);
                return;
            }

            if (previous.Id == current.Id) return;

            if (commandedByUs)
            {
                this.LogInformation("Scenario changed {oldId} {oldName:l} -> {newId} {newName:l} (commanded by Essentials)",
                    previous.Id, previous.Name, current.Id, current.Name);
                return;
            }

            this.LogWarning("Scenario changed outside Essentials: {oldId} {oldName:l} -> {newId} {newName:l}. Last scenario commanded by Essentials: {commandedId:l}. Changed on the VX itself, or the VX restarted",
                previous.Id, previous.Name, current.Id, current.Name, commandedId.HasValue ? commandedId.Value.ToString() : "none pending");
        }

        /// <summary>
        /// Poll device
        /// </summary>
        public void Poll()
        {

            // Run this in a separate thread to avoid blocking the main thread
            // when the sleep commands are executed.
            CrestronInvoke.BeginInvoke((o) =>
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
            }, null);
        }

        /// <summary>
        /// Get current auto switch status
        /// </summary>
        public void GetAutoSwitchStatus()
        {
            var url = string.Format("{0}/AutoSwitchStatus", ApiPath);
            SendRequest(url, string.Empty);
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

            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get current record status
        /// </summary>
        public void GetRecordStatus()
        {
            var url = string.Format("{0}/RecordStatusResponse", ApiPath);
            SendRequest(url, string.Empty);
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

            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get ISO Record state
        /// </summary>
        public void GetIsoRecordStatus()
        {
            var url = string.Format("{0}/ISORecordStatus", ApiPath);
            SendRequest(url, string.Empty);
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

            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the current stream state
        /// </summary>
        public void GetStreamStatus()
        {
            var url = string.Format("{0}/StreamStatus", ApiPath);
            SendRequest(url, string.Empty);
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

            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the current output state
        /// </summary>
        public void GetOutputStatus()
        {
            var url = string.Format("{0}/OutputStatus", ApiPath);
            SendRequest(url, string.Empty);
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

            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the configured layouts
        /// </summary>
        public void GetLayouts()
        {
            var url = string.Format("{0}/GetLayouts", ApiPath);
            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the current layout
        /// </summary>
        public void GetLayoutStatus()
        {
            var url = string.Format("{0}/LayoutStatus", ApiPath);
            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Sets the layout of the device to the specified layout ID.
        /// </summary>
        /// <param name="layout">The layout ID (1-26) to set.</param>
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
            SendRequest(url, content);
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
            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the available room configurations
        /// </summary>
        public void GetRoomConfigs()
        {
            var url = string.Format("{0}/GetRoomConfigs", ApiPath);
            SendRequest(url, string.Empty);
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
            SendRequest(url, content);
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
            SendRequest(url, content);
        }

        /// <summary>
        /// Set all cameras to the home position
        /// </summary>
        public void GoHome()
        {
            var url = string.Format("{0}/GoHome", ApiPath);
            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the available cameras
        /// </summary>
        public void GetCameras()
        {
            var url = string.Format("{0}/GetCameras", ApiPath);
            SendRequest(url, string.Empty);
        }

        /// <summary>
        /// Get the camera status
        /// </summary>
        public void GetCameraStatus()
        {
            var url = string.Format("{0}/CameraStatus", ApiPath);
            SendRequest(url, string.Empty);
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
            SendRequest(url, content);
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
            SendRequest(url, content);
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
            SendRequest(url, content);
        }

        /// <summary>
        /// Import camera presets
        /// </summary>
        public void ImportCameraPresets()
        {
            var url = string.Format("{0}/ImportCameraPresets", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Export camera presets
        /// </summary>
        public void ExportCameraPresets()
        {
            var url = string.Format("{0}/ExportCameraPresets", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
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
            SendRequest(url, content);
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
            SendRequest(url, content);
        }

        /// <summary>
        /// Get available recording space available
        /// </summary>
        public void GetRecordingSpaceAvailable()
        {
            var url = string.Format("{0}/RecodingSpaceAvail", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Set the system to the sleep state
        /// </summary>
        public void SetSleep()
        {
            var url = string.Format("{0}/Sleep", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Wake the system from sleep state
        /// </summary>
        public void SetWake()
        {
            var url = string.Format("{0}/Wake", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Restart the system
        /// </summary>
        public void Restart()
        {
            var url = string.Format("{0}/Restart", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Close the wirecast
        /// </summary>
        public void SetCloseWirecast()
        {
            var url = string.Format("{0}/CloseWirecast", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Get available scenarios
        /// </summary>
        public void GetScenarios()
        {
            var url = string.Format("{0}/GetScenarios", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Get current scenario status
        /// </summary>
        public void GetScenarioStatus()
        {
            var url = string.Format("{0}/ScenarioStatus", ApiPath);
            var content = string.Empty;
            SendRequest(url, content);
        }

        /// <summary>
        /// Set the scenario
        /// </summary>
        /// <param name="scenarioId"></param>
        public void SetScenario(uint scenarioId)
        {
            lock (_scenarioLock)
            {
                _commandedScenarioId = (int)scenarioId;
                _commandedScenarioTime = DateTime.Now;
            }

            var url = string.Format("{0}/GoToScenario", ApiPath);
            var jo = new
            {
                id = scenarioId
            };
            var content = JsonConvert.SerializeObject(jo);
            SendRequest(url, content);
        }

        public void CameraAutoModeOn()
        {
            SetAutoSwitch(true);
        }

        public void CameraAutoModeOff()
        {
            SetAutoSwitch(false);
        }

        public void CameraAutoModeToggle()
        {
            SetAutoSwitch(!_autoSwitchIsOn);
        }

        public void PowerOn() => SetWake();

        public void PowerOff() => SetSleep();

        public void PowerToggle()
        {
            throw new NotImplementedException();
        }

        public void SelectCamera(string key)
        {
            var camera = _config.Cameras.FirstOrDefault((c) => c.DeviceKey == key);

            if (camera == null)
                this.LogError("SelectCamera: Unable to find camera with key {key}", key);

            this.LogDebug("SelectCamera: Setting camera to {camera}", camera?.Id);
            SetCamera(camera.Id);
            
        }
    }
}