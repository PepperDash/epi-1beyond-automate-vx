using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneBeyondAutomateVxEpi.ApiObjects
{
	public class RootResponse
	{
		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
		public string Message { get; set; }

		[JsonProperty("err", NullValueHandling = NullValueHandling.Ignore)]
		public string Error { get; set; }

		[JsonProperty("layout", NullValueHandling = NullValueHandling.Ignore)]
		public NameWithIdString Layout { get; set; }

		[JsonProperty("roomConfig", NullValueHandling = NullValueHandling.Ignore)]
		public NameWithIdInt RoomConfig { get; set; }

		[JsonProperty("scenario", NullValueHandling = NullValueHandling.Ignore)]
		public NameWithIdInt Scenario { get; set; }

		[JsonProperty("cameras", NullValueHandling = NullValueHandling.Ignore)]
		public List<Camera> Cameras { get; set; }

		public RootResponse()
		{
			Cameras = new List<Camera>();
		}
	}

	public class TokenResponse : RootResponse
	{
		[JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
		public string Token { get; set; }
	}

	public class ResultResponse : RootResponse
	{
		[JsonProperty("results")]
		public bool Results { get; set; }
	}

	public class CameraAddressResponse : RootResponse
	{
		[JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
		public string Address { get; set; }

		[JsonProperty("addresses", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> Addresses { get; set; }

		public CameraAddressResponse()
		{
			Addresses = new List<string>();
		}
	}

	public class Camera
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("model")]
		public string Model { get; set; }

		[JsonProperty("ip")]
		public string Ip { get; set; }

		[JsonProperty("asPTZ")]
		public string AsPt { get; set; }
	}

	public class RecordStatusResponse : ResultResponse
	{
		[JsonProperty("record_state", NullValueHandling = NullValueHandling.Ignore)]
		public int RecordState { get; set; }

		[JsonProperty("pause_enabled", NullValueHandling = NullValueHandling.Ignore)]
		public bool PauseEnabled { get; set; }
	}

	public class LayoutsResponse : RootResponse
	{
		[JsonProperty("layouts", NullValueHandling = NullValueHandling.Ignore)]
		public List<NameWithIdString> Layouts { get; set; }

		public LayoutsResponse()
		{
			Layouts = new List<NameWithIdString>();
		}
	}

	public class RoomConfigsResponse : RootResponse
	{
		[JsonProperty("roomConfigs")]
		public List<NameWithIdInt> RoomConfigs { get; set; }

		public RoomConfigsResponse()
		{
			RoomConfigs = new List<NameWithIdInt>();
		}
	}

	public class ScenariosResponse : RootResponse
	{
		[JsonProperty("scenarios")]
		public List<NameWithIdInt> Scenarios { get; set; }

		public ScenariosResponse()
		{
			Scenarios = new List<NameWithIdInt>();
		}
	}

	public class NameWithIdString
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}

	public class NameWithIdInt
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}
}