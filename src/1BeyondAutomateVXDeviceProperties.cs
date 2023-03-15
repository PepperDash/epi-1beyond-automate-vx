using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace PDT.OneBeyondAutomateVx.EPI
{
    public partial class OneBeyondAutomateVX
    {
        // Constructors, methods and some fields located in ******************************
        // separate file: 1BeyondAutomateVXDevice.cs        ******************************

        public BoolFeedback LoginSuccessfulFB;

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
                Debug.Console(1, this, "Error: {0}", error);
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
                Debug.Console(0, this, "Success: {0}", msg);
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
                AutoSwitchIsOnFB.FireUpdate();          
            }
        }

        public BoolFeedback AutoSwitchIsOnFB;
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
                    RecordIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback RecordIsOnFB;
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
                    IsoRecordIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback IsoRecordIsOnFB;
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
                    StreamIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback StreamIsOnFB;
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
                    OutputIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback OutputIsOnFB;


        #endregion

        #region Available Layouts
        event EventHandler LayoutsChanged;

        private List<IdName> _layouts;

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

        private IdName _layout;

        public IdName Layout
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

        private List<IdName> _roomConfigs;

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

        private IdName _roomConfig;

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

        private List<Camera> _cameras;

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

        private List<Drive> _drives;

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

        private RecordingSpace _recordingSpace;

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

        private List<IdName> _scenarios;

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

        private IdName _scenario;

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

    }
}