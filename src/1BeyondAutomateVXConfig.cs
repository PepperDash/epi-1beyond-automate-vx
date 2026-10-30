using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace OneBeyondAutomateVxEpi
    {
    /// <summary>
    /// Plugin device configuration object
    /// </summary>
    public class OneBeyondAutomateVxConfig
        {
            [JsonProperty("control")]
            public EssentialsControlPropertiesConfig Control { get; set; }

            [JsonProperty("cameras")]
            public List<CameraConfig> Cameras { get; set; }

            /// <summary>
            /// Seconds between scenario status polls. Defaults to 30 when omitted; 0 disables polling.
            /// </summary>
            [JsonProperty("pollIntervalSeconds", NullValueHandling = NullValueHandling.Ignore)]
            public int? PollIntervalSeconds { get; set; }
        }

    /// <summary>
    /// Configuration for individual cameras
    /// </summary>
    public class CameraConfig
        {
            [JsonProperty("id")]
            public uint Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("deviceKey")]
            public string DeviceKey { get; set; }
        }
    }