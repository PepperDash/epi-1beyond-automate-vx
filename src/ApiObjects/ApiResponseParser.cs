using Newtonsoft.Json;

namespace OneBeyondAutomateVxEpi.ApiObjects
{
	public static class ApiResponseParser
	{
		public static T ParseResponse<T>(string content)
		{
			return JsonConvert.DeserializeObject<T>(content);
		}

	}
}