using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;

namespace OneBeyondAutomateVxEpi.GenericClients
{
	public static class GenericClientHelpers
	{
		// encodes username and password, returning a Base64 encoded string
		public static string EncodeBase64(string key, string username, string password)
		{
			if (string.IsNullOrEmpty(username))
			{
				return "";
			}

			try
			{
				var base64String =
					Convert.ToBase64String(
						Encoding.GetEncoding("ISO-8859-1")
							.GetBytes(string.Format("{0}:{1}", username, password)));
				return string.Format("{0}", base64String);
			}
			catch (Exception err)
			{
				Debug.Console(AutomateVxDebug.Trace, 
					Debug.ErrorLogLevel.Error, 
					"[{0}] EncodeBase64 Exception:\r{1}", 
					key, err);
				return "";
			}
		}

		public static JToken IsValidJson(string key, string contentString)
		{
			if (string.IsNullOrEmpty(contentString)) return null;

			contentString = contentString.Trim();
			if ((!contentString.StartsWith("{") || !contentString.EndsWith("}")) &&
				(!contentString.StartsWith("[") || !contentString.EndsWith("]"))) return null;

			try
			{
				var jToken = JToken.Parse(contentString);
				Debug.Console(AutomateVxDebug.Notice, "[{0}] IsValidJson: obj {1}", key, jToken == null ? "is null" : "is not null");

				return jToken;
			}
			catch (JsonReaderException jex)
			{
				Debug.Console(AutomateVxDebug.Notice, "[{0}] IsValidJson JsonReaderException.Message: {1}", key, jex.Message);
				Debug.Console(AutomateVxDebug.Verbose, "[{0}] IsValidJson JsonReaderException.StackTrace: {1}", key, jex.StackTrace);
				if (jex.InnerException != null) Debug.Console(AutomateVxDebug.Verbose, "[{0}] IsValidJson JsonReaderException.InnerException: {1}", key, jex.InnerException);

				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(AutomateVxDebug.Notice, "[{0}] IsValidJson Exception.Message: {1}", key, ex.Message);
				Debug.Console(AutomateVxDebug.Verbose, "[{0}] IsValidJson Exception.StackTrace: {1}", key, ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(AutomateVxDebug.Verbose, "[{0}] IsValidJson Exception.InnerException: {1}", key, ex.InnerException);

				return null;
			}
		}
	}
}