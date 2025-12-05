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
            MinimumEssentialsFrameworkVersion = "2.4.7";

            TypeNames = new List<string> { "oneBeyondAutomateVx" };
        }
        
		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.LogDebug("[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);			

            // get the plugin device properties configuration object & check for null 
            var propertiesConfig = dc.Properties.ToObject<OneBeyondAutomateVxConfig>();
            if (propertiesConfig == null)
            {
                Debug.LogInformation("[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
                return null;
            }

			if(propertiesConfig.Control != null)
			{
				return new OneBeyondAutomateVx(dc.Key, dc.Name, propertiesConfig);
			}

			Debug.LogInformation("[{0}] Factory notice: No control object present for device {1}", dc.Key, dc.Name);
			return null;
        }
    }
}

          