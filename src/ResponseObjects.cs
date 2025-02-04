using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Essentials.Devices.Common.Cameras;


namespace PDT.OneBeyondAutomateVx.EPI
{
    public class ResponseObjectBase
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
        public string Token { get; set; }

        [JsonProperty("err", NullValueHandling = NullValueHandling.Ignore)]
        public string Error { get; set; }

        [JsonProperty("results")]
        public bool? Results { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty("available_gigabytes", NullValueHandling = NullValueHandling.Ignore)]
        public string AvailableGigabytes { get; set; }

        [JsonProperty("total_gigabytes", NullValueHandling = NullValueHandling.Ignore)]
        public string TotalGigabytes { get; set; }

        [JsonProperty("copy_underway", NullValueHandling = NullValueHandling.Ignore)]
        public bool CopyUnderway { get; set; }

        [JsonProperty("cameras", NullValueHandling = NullValueHandling.Ignore)]
        public List<Camera> Cameras { get; set; }

        [JsonProperty("layouts", NullValueHandling = NullValueHandling.Ignore)]
        public List<IdName> Layouts { get; set; }

        [JsonProperty("layout", NullValueHandling = NullValueHandling.Ignore)]
        public IdName Layout { get; set; }

        [JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

        [JsonProperty("drives")]
        public List<Drive> Drives { get; set; }

        [JsonProperty("scenarios", NullValueHandling = NullValueHandling.Ignore)]
        public List<IdName> Scenarios { get; set; }

        [JsonProperty("scenario", NullValueHandling = NullValueHandling.Ignore)]
        public IdName Scenario { get; set; }



        public ResponseObjectBase()
        {
            Results = null;
        }

    }

    public class Camera : ID
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ip")]
        public string IpAddress { get; set; }

        public Camera() { }
    }

    public class IdName : ID
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        public IdName() { }
    }

    public class ID
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public ID() { }

        public ID(string id)
        {
            Id = id;
        }
    }

    public class CamAddress
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        public CamAddress(string id)
        {
            Address = id;
        }    
    }

    public class CameraPreset
    {
        [JsonProperty("cam")]
        public string CameraId { get; set; }

        [JsonProperty("pre")]
        public string PresetId { get; set; }

        public CameraPreset(string camId, string presetId)
        {
            CameraId = camId;
            PresetId = PresetId;
        }
    }

    public class FilesParams
    {
        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("logDestination")]
        public string LogDestination { get; set; }

        [JsonProperty("deleteSource")]
        public bool DeleteSource { get; set; }

        public FilesParams(string dest, string logDest, bool delete)
        {
            Destination = dest;
            LogDestination = logDest;
            DeleteSource = delete;
        }
    }

    public class DrivesParams
    {
        [JsonProperty("drives")]
        public string Drives { get; set; }

        public DrivesParams(string drives)
        {
            Drives = drives;
        }
    }

    public class Drive
    {
        [JsonProperty("C:\\", NullValueHandling = NullValueHandling.Ignore)]
        public string C { get; set; }

        [JsonProperty("D:\\", NullValueHandling = NullValueHandling.Ignore)]
        public string D { get; set; }
    
        [JsonProperty("L:\\", NullValueHandling = NullValueHandling.Ignore)]
        public string L { get; set; }
    }

    public class RecordingSpace
    {
        [JsonProperty("available_gigabytes", NullValueHandling = NullValueHandling.Ignore)]
        public string AvailableGigabytes { get; set; }

        [JsonProperty("total_gigabytes", NullValueHandling = NullValueHandling.Ignore)]
        public string TotalGigabytes { get; set; }

        public RecordingSpace(string avail, string total)
        {
            AvailableGigabytes = avail;
            TotalGigabytes = total;
        }
    }
}