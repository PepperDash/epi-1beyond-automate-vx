using System.Collections.Generic;
using OneBeyondAutomateVxEpi.GenericClients;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace OneBeyondAutomateVxEpi
{
	/// <summary>
	/// Plugin device factory for device
	/// </summary>
    public class OneBeyoneAutomateVxFactory : EssentialsPluginDeviceFactory<OneBeyondAutomateVx>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
        public OneBeyoneAutomateVxFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.12.8";

            TypeNames = new List<string>() { "oneBeyondAutomateVx" };
        }
        
		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(AutomateVxDebug.Notice, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

            // get the plugin device properties configuration object & check for null 
            var propertiesConfig = dc.Properties.ToObject<OneBeyondAutomateVXConfigObject>();
            if (propertiesConfig == null)
            {
                Debug.Console(AutomateVxDebug.Trace, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
                return null;
            }

			IRestfulComms client;

			switch (propertiesConfig.Control.Method)
			{
				case eControlMethod.Http:
				{
					Debug.Console(AutomateVxDebug.Notice, "[{0}] building {1} client",
						dc.Key, propertiesConfig.Control.Method);

					client = new GenericClientHttp(string.Format("{0}-http", dc.Key), propertiesConfig.Control);

					break;
				}
				case eControlMethod.Https:
				{
					Debug.Console(AutomateVxDebug.Notice, "[{0}] building {1} client",
						dc.Key, propertiesConfig.Control.Method);

					client = new GenericClientHttps(string.Format("{0}-https", dc.Key), propertiesConfig.Control);

					break;
				}
				default:
				{
					Debug.Console(AutomateVxDebug.Trace, "[{0}] control method {1} not supported, check configuration and upate to 'http' (port 3579) or 'https' (port 4443)",
						dc.Key, propertiesConfig.Control.Method);

					client = null;

					break;
				}
			}

			if(client != null ) return new OneBeyondAutomateVx(dc.Key, dc.Name, propertiesConfig, client);

			Debug.Console(AutomateVxDebug.Trace, "[{0}] Factory notice: No control object present for device {1}", dc.Key, dc.Name);
			return null;
        }
    }
}

          