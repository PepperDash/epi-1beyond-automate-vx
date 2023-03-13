using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;


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

        [JsonProperty("results", NullValueHandling = NullValueHandling.Ignore)]
        public bool Results { get; set; }

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
        public List<Layout> Layouts { get; set; }

        [JsonProperty("layout", NullValueHandling = NullValueHandling.Ignore)]
        public Layout Layout { get; set; }

        [JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

    }

    public class Camera : Id
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ip")]
        public string IpAddress { get; set; }
    }

    public class Layout : Id
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Id
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public Id(string id)
        {
            Id = id;
        }
    }
}