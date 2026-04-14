using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Devices.Common.Cameras;
using System;
using System.Collections.Generic;

namespace OneBeyondAutomateVxEpi
{
    public class CameraRebootMessenger : MessengerBase
    {
        private readonly OneBeyondAutomateVx _cameradevice;

        public CameraRebootMessenger(string key, string messagePath, OneBeyondAutomateVx device) : base(key, messagePath, device as Device)
        {
            _cameradevice = device;
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));
            AddAction($"/rebootCameras", (id, context) => _cameradevice.RebootCameras());
        }

        private void SendFullStatus(string id)
        {
            var state = new CameraRebootMessage
            {
                CameraRebootEnabled = _cameradevice.CameraRebootEnabled,
            };
            PostStatusMessage(state, id);
        }
    }

    public class CameraRebootMessage : DeviceStateMessageBase
    {

        [JsonProperty("cameraRebootEnabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CameraRebootEnabled { get; set; }

    }


}