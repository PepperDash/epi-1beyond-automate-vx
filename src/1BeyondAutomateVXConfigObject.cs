using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace PDT.OneBeyondAutomateVx.EPI
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being created
	/// </remarks>
	/// <example>
	/// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
	/// </example>
	[ConfigSnippet("\"properties\":{\"control\":{}")]
	public class OneBeyondAutomateVXConfigObject
	{
		/// <summary>
		/// JSON control object
		/// </summary>
		/// <example>
		/// <code>
		/// "control": {
        ///		"method": "http",
		///		"tcpSshProperties": {
		///			"address": "172.22.0.101",
		///			"port": 3579,
		///			"username": "admin",
		///			"password": "password",
		///		}
		///	}
		/// </code>
		/// </example>
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Example dictionary of objects
		/// </summary>
        public OneBeyondAutomateVXConfigObject()
		{
		}
	}

}