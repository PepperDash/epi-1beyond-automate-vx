using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace OneBeyondAutomateVxEpi
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	public class OneBeyondAutomateVXConfigObject
	{
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		public OneBeyondAutomateVXConfigObject()
		{
		}
	}

}