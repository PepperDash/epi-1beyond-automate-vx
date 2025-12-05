using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using OneBeyondAutomateVxEpi.ApiObjects;
using OneBeyondAutomateVxEpi.Communications;
using OneBeyondAutomateVxEpi.GenericClients;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.Cameras;
using Renci.SshNet.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ApiCamera = OneBeyondAutomateVxEpi.ApiObjects.Camera;

namespace OneBeyondAutomateVxEpi
{
    public class OneBeyondAutomateVx : EssentialsBridgeableDevice, IHasCamerasWithControls, IHasCameraAutoMode, IHasPowerControl
    {
        private const string ApiPath = "/api";

        #region IRestfulComms

        // private readonly IRestfulComms _client;

        private readonly OneBeyondClient _oneBeyondClient;

        private int _responseCode;
        private string _responseContent;
        private string _responseSuccessMessage;
        private string _responseErrorMessage;

        public bool CameraRebootEnabled => _config.EnableCameraReboot;

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

                var scenario = ScenariosSelectableItems.Items.Values.FirstOrDefault(s => s.Key == _currentScenario.Id.ToString());

                ScenariosSelectableItems.CurrentItem = scenario.Key;

                foreach (var item in ScenariosSelectableItems.Items)
                {
                    var scenarioItem = item.Value as ScenariosSelectableItem;
                    scenarioItem.UpdateSelectedFromFeedback(_currentScenario.Id);
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
                if (value == _selectedCamera) return;

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
        private CTimer _cameraRebootTimer;


        public OneBeyondAutomateVx(string key, string name, OneBeyondAutomateVxConfig config)
            : base(key, name)
        {
            Debug.LogInformation(this, "Constructing new {0} instance", name);

            try
            {
                _config = config;

                _oneBeyondClient = new OneBeyondClient(Key + "-httpClient", config.Control);

                //ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
                //ResponseContentFeedback = new StringFeedback(() => ResponseContent);
                ResponseSuccessMessageFeedback = new StringFeedback("ResponseSuccessMessageFeedback", () => ResponseSuccessMessage);
                ResponseErrorMessageFeedback = new StringFeedback("ResponseErrorMessageFeedback", () => ResponseErrorMessage);

                LoginSuccessfulFeedback = new BoolFeedback("LoginSuccessfulFeedback", () => !string.IsNullOrEmpty(Token));
                AutoSwitchIsOnFeedback = new BoolFeedback("AutoSwitchIsOnFeedback", () => AutoSwitchIsOn);
                RecordIsOnFeedback = new BoolFeedback("RecordIsOnFeedback", () => RecordIsOn);
                IsoRecordIsOnFeedback = new BoolFeedback("IsoRecordIsOnFeedback", () => IsoRecordIsOn);
                StreamIsOnFeedback = new BoolFeedback("StreamIsOnFeedback", () => StreamIsOn);
                OutputIsOnFeedback = new BoolFeedback("OutputIsOnFeedback", () => OutputIsOn);
                CameraAddressFeedback = new IntFeedback("CameraAddressFeedback", () => CameraAddress);
                CamerasCountFeedback = new IntFeedback("CamerasCountFeedback", () => ApiCameras.Count);
                LayoutsCountFeedback = new IntFeedback("LaoutsCountFeedback", () => Layouts.Count);
                CurrentLayoutNameFeedback = new StringFeedback("CurrentLayoutNameFeedback", () => CurrentLayout.Name);
                CurrentLayoutIdFeedback = new IntFeedback("CurrentLayoutIdFeedback", () => ConvertIdToInt(CurrentLayout.Id));
                RoomConfigsCountFeedback = new IntFeedback("RoomConfigsCountFeedback", () => RoomConfigs.Count);
                CurrentRoomConfigNameFeedback = new StringFeedback("CurrentRoomConfigNameFeedback", () => CurrentRoomConfig.Name);
                CurrentRoomConfigIdFeedback = new IntFeedback("CurrentRoomConfigIdFeedback", () => CurrentRoomConfig.Id);
                ScenariosCountFeedback = new IntFeedback("ScenariosCountFeedback", () => Scenarios.Count);
                CurrentScenarioNameFeedback = new StringFeedback("CurrentScenarioNameFeedback", () => CurrentScenario.Name);
                CurrentScenarioIdFeedback = new IntFeedback("CurrentScenarioIdFeedback", () => CurrentScenario.Id);
                CameraAutoModeIsOnFeedback = new BoolFeedback("CameraAutoModeIsOnFeedback", () => AutoSwitchIsOn);
                SelectedCameraFeedback = new StringFeedback("SelectedCameraFeedback", () => _selectedCamera?.Key ?? string.Empty);

                if (ApiCameras == null)
                    ApiCameras = new List<ApiCamera>();
                if (Layouts == null)
                    Layouts = new List<NameWithIdString>();
                if (RoomConfigs == null)
                    RoomConfigs = new List<NameWithIdInt>();
                if (Scenarios == null)
                    Scenarios = new List<NameWithIdInt>();


                // _client.ResponseReceived += OnResponseReceived;

                SetupCameraRebootSchedule();
            }
            catch (Exception ex)
            {
                this.LogError("OneBeyondAutomateVx Exception Message: {0}", ex.Message);
                this.LogError("OneBeyondAutomateVx Stack Trace: {0}", ex.StackTrace);
                if (ex.InnerException != null) this.LogError("OneBeyondAutomateVx Inner Exception {0}", ex.InnerException);
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

            var cameraRebootMessenger = new CameraRebootMessenger($"{Key}-cameraReboot", $"/device/{Key}", this);
            mc.AddDeviceMessenger(cameraRebootMessenger);

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

        public void RebootCameras()
        {
            if (!_config.EnableCameraReboot) return;

            foreach (ApiCamera camera in ApiCameras)
            {
                Debug.LogInformation(this, "Rebooting camera '{0}'", camera.Id);
                SetCameraPreset((uint)camera.Id, 99); // preset 99 is reboot
            }
        }

        private void SetupCameraRebootSchedule()
        {
            if (!_config.EnableCameraReboot)
            {
                Debug.LogInformation(this, "Camera reboot scheduling disabled");
                return;
            }

            if (_config.CameraRebootHour < 0 || _config.CameraRebootHour > 23 ||
                _config.CameraRebootMinute < 0 || _config.CameraRebootMinute > 59)
            {
                _config.CameraRebootHour = 4;
                _config.CameraRebootMinute = 30;
                Debug.LogInformation(this, "Camera reboot time not set or invalid, using default time: {0}:{1:D2}",
                    _config.CameraRebootHour, _config.CameraRebootMinute);
            }
            else
            {
                Debug.LogInformation(this, "Setting up camera reboot schedule for {0}:{1:D2}",
                    _config.CameraRebootHour, _config.CameraRebootMinute);
            }

            CalculateAndStartRebootTimer();
        }

        private void CalculateAndStartRebootTimer()
        {
            if (_cameraRebootTimer != null)
            {
                _cameraRebootTimer.Stop();
                _cameraRebootTimer.Dispose();
                _cameraRebootTimer = null;
            }

            var now = DateTime.Now;
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, _config.CameraRebootHour, _config.CameraRebootMinute, 0);

            if (scheduledTime <= now)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            var timeUntilReboot = (long)(scheduledTime - now).TotalMilliseconds;

            Debug.LogInformation(this, "Next camera reboot scheduled for: {0} (in {1} ms)",
                scheduledTime.ToString("yyyy-MM-dd HH:mm:ss"), timeUntilReboot);

            _cameraRebootTimer = new CTimer(OnCameraRebootTimerCallback, timeUntilReboot);
            _cameraRebootTimer.Reset();
        }

        private void OnCameraRebootTimerCallback(object obj)
        {
            try
            {
                Debug.LogInformation(this, "Executing scheduled camera reboot");
                RebootCameras();

                CalculateAndStartRebootTimer();
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "Error during scheduled camera reboot: {0}", ex.Message);
                Debug.LogError(this, "Stack trace: {0}", ex.StackTrace);

                CalculateAndStartRebootTimer();
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
            Token = null;
        }

        /// <summary>
        /// Attempts to get an authorization token
        /// </summary>
        public void GetToken()
        {
            // _client.SendRequest("POST", "Get-Token", string.Empty);
            var response = _oneBeyondClient.SendRequest<TokenResponse>(HttpMethod.Post, "Get-Token");

            Token = response.Token;
            ResponseSuccessMessage = response.Message;

            if (response.Status != "OK")
            {
                ClearToken();
            }
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
            var response = _oneBeyondClient.SendRequest<ResultResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                Debug.LogVerbose(this, "OnResponseReceived: 'autoswitchstatus' results {0}", response.Results.ToString());
                AutoSwitchIsOn = (response.Results == true);
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url);
            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
            if (response.Status == "OK")
            {
                GetAutoSwitchStatus();
                ResponseSuccessMessage = response.Message;
                return;
            }
        }

        /// <summary>
        /// Get current record status
        /// </summary>
        public void GetRecordStatus()
        {
            var url = string.Format("{0}/RecordStatusResponse", ApiPath);

            var response = _oneBeyondClient.SendRequest<RecordStatusResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                OutputIsOn = response.Results;
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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


            _oneBeyondClient.SendRequest<object>(HttpMethod.Post, url);
        }

        /// <summary>
        /// Get ISO Record state
        /// </summary>
        public void GetIsoRecordStatus()
        {
            var url = string.Format("{0}/ISORecordStatus", ApiPath);

            var response = _oneBeyondClient.SendRequest<ResultResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                IsoRecordIsOn = response.Results;
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Get the current stream state
        /// </summary>
        public void GetStreamStatus()
        {
            var url = string.Format("{0}/StreamStatus", ApiPath);

            var response = _oneBeyondClient.SendRequest<ResultResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                StreamIsOn = response.Results;
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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


            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url);

            if (response.Status == "OK")
            {
                GetStreamStatus();
                return;
            }

            ResponseErrorMessage = response.Error;
        }

        /// <summary>
        /// Get the current output state
        /// </summary>
        public void GetOutputStatus()
        {
            var url = string.Format("{0}/OutputStatus", ApiPath);

            var response = _oneBeyondClient.SendRequest<ResultResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                OutputIsOn = response.Results;
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url);

            if (response.Status == "OK")
            {
                GetOutputStatus();
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
        }

        /// <summary>
        /// Get the configured layouts
        /// </summary>
        public void GetLayouts()
        {
            var url = string.Format("{0}/GetLayouts", ApiPath);

            var response = _oneBeyondClient.SendRequest<LayoutsResponse>(HttpMethod.Post, url, string.Empty);

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

        }

        /// <summary>
        /// Get the current layout
        /// </summary>
        public void GetLayoutStatus()
        {
            var url = string.Format("{0}/LayoutStatus", ApiPath);

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                CurrentLayout = response.Layout;
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status == "OK")
            {
                GetLayoutStatus();
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                CurrentRoomConfig = response.RoomConfig;
                ResponseSuccessMessage = response.Message;
            }

            ResponseErrorMessage = response.Error;
        }

        /// <summary>
        /// Get the available room configurations
        /// </summary>
        public void GetRoomConfigs()
        {
            var url = string.Format("{0}/GetRoomConfigs", ApiPath);

            var response = _oneBeyondClient.SendRequest<RoomConfigsResponse>(HttpMethod.Post, url, string.Empty);

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
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
            // TODO: Not sure if we need to deal with the response here?
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
            // TODO: Not sure if we need to deal with the response here?
        }

        /// <summary>
        /// Set all cameras to the home position
        /// </summary>
        public void GoHome()
        {
            var url = string.Format("{0}/GoHome", ApiPath);
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Get the available cameras
        /// </summary>
        public void GetCameras()
        {
            var url = string.Format("{0}/GetCameras", ApiPath);

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, string.Empty);

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
        }

        /// <summary>
        /// Get the camera status
        /// </summary>
        public void GetCameraStatus()
        {
            var url = string.Format("{0}/CameraStatus", ApiPath);

            var response = _oneBeyondClient.SendRequest<CameraAddressResponse>(HttpMethod.Post, url, string.Empty);

            if (response.Status == "OK")
            {
                CameraAddress = Convert.ToInt16(response.Address);
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status == "OK")
            {
                GetCameraStatus();
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Error;
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
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

            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Import camera presets
        /// </summary>
        public void ImportCameraPresets()
        {
            var url = string.Format("{0}/ImportCameraPresets", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }

        }

        /// <summary>
        /// Export camera presets
        /// </summary>
        public void ExportCameraPresets()
        {
            var url = string.Format("{0}/ExportCameraPresets", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
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
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
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
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Get available recording space available
        /// </summary>
        public void GetRecordingSpaceAvailable()
        {
            var url = string.Format("{0}/RecodingSpaceAvail", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Set the system to the sleep state
        /// </summary>
        public void SetSleep()
        {
            var url = string.Format("{0}/Sleep", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Wake the system from sleep state
        /// </summary>
        public void SetWake()
        {
            var url = string.Format("{0}/Wake", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Restart the system
        /// </summary>
        public void Restart()
        {
            var url = string.Format("{0}/Restart", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Close the wirecast
        /// </summary>
        public void SetCloseWirecast()
        {
            var url = string.Format("{0}/CloseWirecast", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status != "OK")
            {
                ResponseErrorMessage = response.Error;
                return;
            }
        }

        /// <summary>
        /// Get available scenarios
        /// </summary>
        public void GetScenarios()
        {
            var url = string.Format("{0}/GetScenarios", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<ScenariosResponse>(HttpMethod.Post, url, content);

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

        }

        /// <summary>
        /// Get current scenario status
        /// </summary>
        public void GetScenarioStatus()
        {
            var url = string.Format("{0}/ScenarioStatus", ApiPath);
            var content = string.Empty;
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

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
            var response = _oneBeyondClient.SendRequest<RootResponse>(HttpMethod.Post, url, content);

            if (response.Status == "OK")
            {
                GetScenarioStatus();
                ResponseSuccessMessage = response.Message;
                return;
            }

            ResponseErrorMessage = response.Message;
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