using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PDT.OneBeyondAutomateVx.EPI
{
    public class OneBeyondCamera : CameraBase, IHasCameraPtzControl, IHasCameraPresets, IBridgeAdvanced
    {
        protected OneBeyondAutomateVX ParentDevice { get; private set; }

        /// <summary>
        /// The ID of the camera
        /// </summary>
        public uint CameraId { get; private set; }

        public string IpAddress { get; private set; }

        private bool isPanning;

        private bool isTilting;

        private bool isZooming;

        private bool isMoving
        {
            get
            {
                return isPanning || isTilting || isZooming;
            }
        }


        public OneBeyondCamera(string key, string name, OneBeyondAutomateVX parent, CameraInfo cameraInfo)
            : base(key, name)
        {
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom;

            ParentDevice = parent;

            CameraId = System.UInt16.Parse(cameraInfo.Id);
            IpAddress = cameraInfo.IpAddress;
        }




        #region IHasCameraPtzControl Members

        public void PositionHome()
        {
            ParentDevice.SetCameraPreset(CameraId, (uint)ReservedPresets.HomeShot);
        }

        #endregion

        #region IHasCameraPanControl Members

        public void PanLeft()
        {
            if (!isMoving)
            {
                ParentDevice.StartCameraPanTilt(CameraId, (uint)PanTiltDirections.Left);
                isPanning = true;
            }
        }

        public void PanRight()
        {
            if (!isMoving)
            {
                ParentDevice.StartCameraPanTilt(CameraId, (uint)PanTiltDirections.Right);
                isPanning = true;
            }
        }

        public void PanStop()
        {
            ParentDevice.StopCameraPanTilt(CameraId);
            isPanning = false;
        }

        #endregion

        #region IHasCameraTiltControl Members

        public void TiltUp()
        {
            if (!isMoving)
            {
                ParentDevice.StartCameraPanTilt(CameraId, (uint)PanTiltDirections.Up);
                isTilting = true;
            }
        }

        public void TiltDown()
        {
            if (!isMoving)
            {
                ParentDevice.StartCameraPanTilt(CameraId, (uint)PanTiltDirections.Down);
                isTilting = true;
            }
        }

        public void TiltStop()
        {
            ParentDevice.StopCameraPanTilt(CameraId);
            isTilting = false;
        }

        #endregion

        #region IHasCameraZoomControl Members

        public void ZoomIn()
        {
            if (!isMoving)
            {
                ParentDevice.StartCameraZoom(CameraId, (uint)ZoomDirections.ZoomIn);
                isZooming = true;
            }
        }

        public void ZoomOut()
        {
            if (!isMoving)
            {
                ParentDevice.StartCameraZoom(CameraId, (uint)ZoomDirections.ZoomOut);
                isZooming = true;
            }
        }

        public void ZoomStop()
        {
            ParentDevice.StopCameraZoom(CameraId);
            isZooming = false;
        }

        #endregion

        #region IBridgeAdvanced Members

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            LinkCameraToApi(this, trilist, joinStart, joinMapKey, bridge);
        }

        #endregion

        #region IHasCameraPresets Members

        public void PresetSelect(int preset)
        {
            ParentDevice.SetCameraPreset(CameraId, (uint)preset);
        }

        public void PresetStore(int preset, string description)
        {
            // [ ] TODO: add logic to prevent save to Reserved Presets
            ParentDevice.SaveCameraPreset(CameraId, (uint)preset);
        }

        public List<CameraPreset> Presets
        {
            //get { throw new NotImplementedException(); }
            // getting a camera preset list not an available API command
            get { return null; }
        }

        public event EventHandler<EventArgs> PresetsListHasChanged;

        #endregion
    }


    internal enum ReservedPresets : uint
    {
        HomeShot = 0,
        TrackingShot = 1,
        StartTracking = 80,
        PauseTracking = 81,
        StartGroupTracking = 82,
        PauseGroupTracking = 83,
        StartPauseMultiCameraSwitching = 84,
        PauseMultiCameraSwitchingOutputGroupFraming = 85,
        PauseMultiCameraSwitchingOutputPresenterTracking = 86,
        OSDMenuToggle = 95,
        Reboot = 99,
        PresetZone1 = 101,
        PresetZone2 = 102,
        PresetZone3 = 103,
        PresetZone4 = 104
    }

    internal enum PanTiltDirections : uint
    {
        Up = 0,
        UpRight = 1,
        Right = 2,
        DownRight = 3,
        Down = 4,
        DownLeft = 5,
        Left = 6,
        UpLeft = 7
    }

    internal enum ZoomDirections : uint
    {
        ZoomIn = 0,
        ZoomOut = 1
    }
}
