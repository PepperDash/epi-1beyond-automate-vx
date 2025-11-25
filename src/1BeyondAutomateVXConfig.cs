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

            [JsonProperty("enableCameraReboot")]            
            public bool EnableCameraReboot { get; set; }

            [JsonProperty("cameraRebootHour")]
            public int CameraRebootHour { get; set; }

            [JsonProperty("cameraRebootMinute")]
            public int CameraRebootMinute { get; set; }
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