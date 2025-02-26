using System;
using Newtonsoft.Json;

namespace OneBeyondAutomateVxEpi.ApiObjects
{
	public static class ApiResponseParser
	{
		public static TokenResponse ParseTokenResponse(string content)
		{
			return JsonConvert.DeserializeObject<TokenResponse>(content);
		}

		public static RootResponse ParseRootResponse(string content)
		{
			return JsonConvert.DeserializeObject<RootResponse>(content);
		}

		public static ResultResponse ParseResultResponse(string content)
		{
			return JsonConvert.DeserializeObject<ResultResponse>(content);
		}

		public static CameraAddressResponse ParseCameraAddressResponse(string content)
		{
			return JsonConvert.DeserializeObject<CameraAddressResponse>(content);
		}

		public static RecordStatusResponse ParseRecordStatusResponse(string content)
		{
			return JsonConvert.DeserializeObject<RecordStatusResponse>(content);
		}

		public static LayoutsResponse ParseLayoutsResponse(string content)
		{
			return JsonConvert.DeserializeObject<LayoutsResponse>(content);
		}

		public static RoomConfigsResponse ParseRoomConfigsResponse(string content)
		{
			return JsonConvert.DeserializeObject<RoomConfigsResponse>(content);
		}

		public static ScenariosResponse ParseScenariosResponse(string content)
		{
			return JsonConvert.DeserializeObject<ScenariosResponse>(content);
		}
	}
}