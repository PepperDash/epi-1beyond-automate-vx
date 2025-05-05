using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Devices.Common.Cameras;
using System;
using System.Collections.Generic;

namespace OneBeyondAutomateVxEpi
    {
    public class IHasCameraAutoModeMessenger : MessengerBase
        {   
        private readonly IHasCameraAutoMode _cameramodedevice;

        public IHasCameraAutoModeMessenger(string key, string messagePath, IHasCameraAutoMode device) : base(key, messagePath, device as Device)
            {
                   _cameramodedevice = device;
            }

        protected override void RegisterActions()
            {
            base.RegisterActions();
          
            AddAction("/fullStatus", (id, content) => SendFullStatus());
            AddAction($"/cameraAutoModeOn", (id, context) => _cameramodedevice.CameraAutoModeOn());
            AddAction($"/cameraAutoModeOff", (id, context) => _cameramodedevice.CameraAutoModeOff());
            AddAction($"/cameraAutoModeToggle", (id, context) => _cameramodedevice.CameraAutoModeToggle());

            _cameramodedevice.CameraAutoModeIsOnFeedback.OutputChange += (o, a) => SendFullStatus();

            }

        private void SendFullStatus()
            {
            var state = new IHasCameraAutoModeMessage
                {
                CameraAutoModeStatus = _cameramodedevice.CameraAutoModeIsOnFeedback.BoolValue
                };
            PostStatusMessage(state);
            }
        }

    public class IHasCameraAutoModeMessage : DeviceStateMessageBase
        {
        [JsonProperty("cameraAutoModeStatus", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? CameraAutoModeStatus { get; set; }

        }


    }