using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace OneBeyondAutomateVxEpi
{
    public partial class OneBeyondAutomateVx
    {
        // Constructors, methods and some fields located in ******************************
        // separate file: 1BeyondAutomateVXDevice.cs        ******************************

        public BoolFeedback LoginSuccessfulFb;

        event EventHandler<ErrorEventArgs> ErrorMessageReceived;

        public class ErrorEventArgs : EventArgs
        {
            public string ErrorMessage;

            public ErrorEventArgs(string err)
            {
                ErrorMessage = err;
            }
        }

        private void OnErrorMessageReceived(string error)
        {
            var handler = ErrorMessageReceived;
            if (handler != null)
            {
                Debug.Console(AutomateVxDebug.Notice, this, "Error: {0}", error);
                handler(this, new ErrorEventArgs(error));
            }
        }


        event EventHandler<SuccessEventArgs> SuccessMessageReceived;

        public class SuccessEventArgs : EventArgs
        {
            public string SuccessMessage;

            public SuccessEventArgs(string msg)
            {
                SuccessMessage = msg;
            }
        }

        private void OnSuccessMessageReceived(string msg)
        {
            var handler = SuccessMessageReceived;
            if (handler != null)
            {
                Debug.Console(AutomateVxDebug.Trace, this, "Success: {0}", msg);
                handler(this, new SuccessEventArgs(msg));
            }
        }

        #region AutoSwitch
        private bool _autoSwitchIsOn;

        public bool AutoSwitchIsOn
        {
            get { return _autoSwitchIsOn; }
            private set
            {
                if (value == _autoSwitchIsOn) return;
                
                _autoSwitchIsOn = value;
                AutoSwitchIsOnFb.FireUpdate();          
            }
        }

        public BoolFeedback AutoSwitchIsOnFb;
        #endregion

        #region Recording
        private bool _recordIsOn;

        public bool RecordIsOn
        {
            get { return _recordIsOn; }
            private set
            {
                if (value != _recordIsOn)
                {
                    _recordIsOn = value;
                    RecordIsOnFb.FireUpdate();
                }
            }
        }

        public BoolFeedback RecordIsOnFb;
        #endregion

        #region ISO Recording

        private bool _isoRecordIsOn;

        public bool IsoRecordIsOn
        {
            get { return _isoRecordIsOn; }
            private set
            {
                if (value != _isoRecordIsOn)
                {
                    _isoRecordIsOn = value;
                    IsoRecordIsOnFb.FireUpdate();
                }
            }
        }

        public BoolFeedback IsoRecordIsOnFb;
        #endregion

        #region Streaming

        private bool _streamIsOn;

        public bool StreamIsOn
        {
            get { return _streamIsOn; }
            private set
            {
                if (value != _streamIsOn)
                {
                    _streamIsOn = value;
                    StreamIsOnFb.FireUpdate();
                }
            }
        }

        public BoolFeedback StreamIsOnFb;
        #endregion

        #region Output

        private bool _outputIsOn;

        public bool OutputIsOn
        {
            get { return _outputIsOn; }
            private set
            {
                if (value != _outputIsOn)
                {
                    _outputIsOn = value;
                    OutputIsOnFb.FireUpdate();
                }
            }
        }

        public BoolFeedback OutputIsOnFb;


        #endregion

        #region Available Layouts
        event EventHandler LayoutsChanged;

        private List<IdName> _layouts = new List<IdName>();

        public List<IdName> Layouts
        {
            get
            {
				return _layouts;
            }
            private set
            {
                _layouts = value;

                OnLayoutsChanged();
            }
        }

        private void OnLayoutsChanged()
        {
            var handler = LayoutsChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion

        #region Current Layout
        event EventHandler LayoutChanged;

        private ID _layout = new ID();

        public ID Layout
        {
            get
            {
                return _layout;
            }
            private set
            {
                _layout = value;

                OnLayoutChanged();
            }
        }

        private void OnLayoutChanged()
        {
            var handler = LayoutChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        #endregion

        #region Available RoomConfigs
        event EventHandler RoomConfigsChanged;

        private List<IdName> _roomConfigs = new List<IdName>();

        public List<IdName> RoomConfigs
        {
            get
            {
                return _roomConfigs;
            }
            private set
            {
                _roomConfigs = value;

                OnRoomConfigsChanged();
            }
        }

        private void OnRoomConfigsChanged()
        {
            var handler = RoomConfigsChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion


        #region Current RoomConfig
        event EventHandler RoomConfigChanged;

        private IdName _roomConfig = new IdName();

        public IdName RoomConfig
        {
            get
            {
                return _roomConfig;
            }
            private set 
            {
                _roomConfig = value;

                OnRoomConfigChanged();
            }
        }

        private void OnRoomConfigChanged()
        {
            var handler = RoomConfigChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        #endregion

        #region CameraAddress

        private int _cameraAddress;

        public int CameraAddress
        {
            get { return _cameraAddress; }
            private set
            {
                if (value != _cameraAddress)
                {
                    _cameraAddress = value;
                    CameraAddressFB.FireUpdate();
                }
            }
        }

        public IntFeedback CameraAddressFB;

        #endregion

        #region Available Cameras
        event EventHandler CamerasChanged;

        private List<Camera> _cameras = new List<Camera>();

        public List<Camera> Cameras
        {
            get
            {
                return _cameras;
            }
            private set
            {
                _cameras = value;

                OnCamerasChanged();
            }
        }

        private void OnCamerasChanged()
        {
            var handler = CamerasChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion

        #region Available Storage
        event EventHandler StorageSpaceAvailableChanged;

        private List<Drive> _drives = new List<Drive>();

        public List<Drive> Drives
        {
            get
            {
                return _drives;
            }
            private set
            {
                _drives = value;

                OnDrivesChanged();
            }
        }

        private void OnDrivesChanged()
        {
            var handler = StorageSpaceAvailableChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion



        #region Recording Space
        event EventHandler RecordingSpaceAvailableChanged;

        private RecordingSpace _recordingSpace = new RecordingSpace("0", "0");

        public RecordingSpace RecordingSpace
        {
            get 
            { 
                return _recordingSpace;
            }
            private set 
            {
                _recordingSpace = value;
                OnRecordingSpaceChanged();
            }
        }

        private void OnRecordingSpaceChanged()
        {
            var handler = RecordingSpaceAvailableChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion


        #region Available Scenarios
        event EventHandler ScenariosChanged;

        private List<IdName> _scenarios = new List<IdName>();

        public List<IdName> Scenarios
        {
            get
            {
                return _scenarios;
            }
            private set
            {
                _scenarios = value;

                OnScenariosChanged();
            }
        }

        private void OnScenariosChanged()
        {
            var handler = ScenariosChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion

        #region Current Scenario
        event EventHandler ScenarioChanged;

        private IdName _scenario = new IdName();

        public IdName Scenario
        {
            get
            {
                return _scenario;
            }
            private set
            {
                _scenario = value;

                OnScenarioChanged();
            }
        }

        private void OnScenarioChanged()
        {
            var handler = ScenarioChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        #endregion

        event EventHandler FileCopySuccessful;


        private void OnFileCopySuccessful()
        {
            var handler = FileCopySuccessful;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

    }
}