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
          
            AddAction("/fullStatus", (id, content) => SendFullStatus(id));
            AddAction($"/cameraAutoModeOn", (id, context) => _cameramodedevice.CameraAutoModeOn());
            AddAction($"/cameraAutoModeOff", (id, context) => _cameramodedevice.CameraAutoModeOff());
            AddAction($"/cameraAutoModeToggle", (id, context) => _cameramodedevice.CameraAutoModeToggle());

            _cameramodedevice.CameraAutoModeIsOnFeedback.OutputChange += (o, a) => SendFullStatus(null);

            }

        private void SendFullStatus(string id)
            {
            var state = new IHasCameraAutoModeMessage
                {
                CameraAutoModeIsOn = _cameramodedevice.CameraAutoModeIsOnFeedback.BoolValue
                };
            PostStatusMessage(state, id);
            }
        }

    public class IHasCameraAutoModeMessage : DeviceStateMessageBase
        {
            [JsonProperty("cameraAutoModeIsOn", NullValueHandling = NullValueHandling.Ignore)]
            public bool? CameraAutoModeIsOn { get; set; }

        }


    }