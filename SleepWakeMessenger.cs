using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common;
using System;

namespace OneBeyondAutomateVxEpi
    {
    public class IHasPowerControlMessenger : MessengerBase
        {
        private readonly IHasPowerControl _powerControlDevice;

        public IHasPowerControlMessenger(string key, string messagePath, IHasPowerControl device)
            : base(key, messagePath, device as Device)
            {
            _powerControlDevice = device;
            }

        protected override void RegisterActions()
            {
            base.RegisterActions();

            AddAction("/setWake", (id, context) => _powerControlDevice.PowerOn());
            AddAction("/setSleep", (id, context) => _powerControlDevice.PowerOff());
            AddAction("/fullStatus", (id, context) => SendFullStatus());
            }

        public void SendFullStatus()
            {
            if (_powerControlDevice is OneBeyondAutomateVx concrete)
                {
                var state = new IHasPowerControlMessage
                    {
                    PowerIsOnStatus = concrete.OutputIsOn
                    };
                PostStatusMessage(state);
                }
            else
                {
                Debug.LogError(this, "Cannot send status. Device is not OneBeyondAutomateVx.");
                }
            }
        }

    public class IHasPowerControlMessage : DeviceStateMessageBase
        {
        [JsonProperty("powerIsOnStatus", NullValueHandling = NullValueHandling.Ignore)]
        public bool? PowerIsOnStatus { get; set; }
        }

    }