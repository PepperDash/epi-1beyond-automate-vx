using System;
using System.Globalization;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using OneBeyondAutomateVxEpi.ApiObjects;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;

namespace OneBeyondAutomateVxEpi.GenericClients
{
	/// <summary>
	/// Http client
	/// </summary>
	public class GenericClientHttps : IRestfulComms
	{
		private static readonly string Separator = new String('-', 50);

		private readonly HttpsClient _client;
		private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);

		public string Host { get; private set; }
		public int Port { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }
		public string AuthorizationBase64 { get; private set; }
		public string Token { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="controlConfig"></param>
		public GenericClientHttps(string key, ControlPropertiesConfig controlConfig)
		{
			if (string.IsNullOrEmpty(key) || controlConfig == null)
			{
				Debug.Console(AutomateVxDebug.Verbose, Debug.ErrorLogLevel.Error,
					"GenericClient key or host is null or empty, failed to create client for {0}", key);
				return;
			}

			Key = string.Format("{0}-client", key).ToLower();

			Port = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
				? controlConfig.TcpSshProperties.Port
				: 4443;

			Host = String.Format("https://{0}:{1}",
					controlConfig.TcpSshProperties.Address.Replace("https://", ""),
					Port);
			
			Username = controlConfig.TcpSshProperties.Username ?? "";
			Password = controlConfig.TcpSshProperties.Password ?? "";
			AuthorizationBase64 = GenericClientHelpers.EncodeBase64(key, Username, Password);

			Debug.Console(AutomateVxDebug.Verbose, this, @"
{0}
>>>>> GenericClientHttps: 
Key = {1}
Host = {2}
Port = {3}
Username = {4}
Password = {5}
AuthBase64 = {6}
Token = {7}
{0}", Separator, Key, Host, Port, Username, Password, AuthorizationBase64, Token);

			_client = new HttpsClient
			{
				//UserName = Username,
				//Password = Password,
				KeepAlive = false,
				HostVerification = false,
				PeerVerification = false
			};

			DeviceManager.AddDevice(this);
		}

		#region IRestfulComms Members

		/// <summary>
		/// Implements IKeyed interface
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// Sends the request with the provided parameters
		/// </summary>
		/// <param name="requestType"></param>
		/// <param name="path"></param>
		/// <param name="content"></param>
		public void SendRequest(string requestType, string path, string content)
		{
			var reqType = (RequestType)Enum.Parse(typeof(RequestType), requestType, true);
			SendRequest(reqType, path, content);
		}

		/// <summary>
		/// Sends the request with the provided parameters
		/// </summary>
		/// <param name="requestType"></param>
		/// <param name="path"></param>
		/// <param name="content"></param>
		public void SendRequest(RequestType requestType, string path, string content)
		{
			var request = new HttpsClientRequest
			{
				RequestType = requestType,
				Url = new UrlParser(String.Format("{0}/{1}", Host, path.TrimStart('/'))),
				ContentString = content
			};

			request.Header.ContentType = "application/json";
			request.Header.SetHeaderValue("Content-Length", content.Length.ToString(CultureInfo.InvariantCulture));

			var authorizationHeaderValue = string.IsNullOrEmpty(Token)
				? AuthorizationBase64
				: Token;

			request.Header.SetHeaderValue("Authorization", authorizationHeaderValue);
			
			Debug.Console(AutomateVxDebug.Verbose, this, @"
{0}
>>>>> SendRequest
url: {1}
content: {2}
requestType: {3}
authHeaderValue: {4}
{0}", Separator, request.Url, request.ContentString, request.RequestType, authorizationHeaderValue);

			if (_client.ProcessBusy)
				_requestQueue.Enqueue(() => RequestDispatch(request));
			else
				RequestDispatch(request);
		}

		// dispatches the recieved request
		private void RequestDispatch(HttpsClientRequest request)
		{
			_client.DispatchAsync(request, (response, error) =>
			{
				if (response == null)
				{
					Debug.Console(AutomateVxDebug.Verbose, this, @"
{0}
>>>>> RequestDispatch
request: {1}
error: {2}
{0}", Separator, request, error);
					return;
				}

				var parts = request.Url.ToString().Split('/');
				var requestPath = parts[parts.Length - 1];

				OnResponseRecieved(new GenericClientResponseEventArgs(requestPath, response.Code, response.ContentString));
			});
		}

		/// <summary>
		/// Client response event
		/// </summary>
		public event EventHandler<GenericClientResponseEventArgs> ResponseReceived;

		// client response event handler
		private void OnResponseRecieved(GenericClientResponseEventArgs args)
		{

			Debug.Console(AutomateVxDebug.Verbose, this, @"
{0}
>>>>> OnResponseReceived: 
args.Reqeuest = {1}
args.Code = {2}
args.ContentString = {3}
{0}", Separator, args.Request, args.Code, args.ContentString);

			CheckRequestQueue();

			switch (args.Code)
			{
				case 200:
				{
					if (args.Request.ToLower().Contains("get-token"))
					{
						var tokenResponse = ApiResponseParser.ParseTokenResponse(args.ContentString);
						if (tokenResponse.Status == "OK")
						{
							Token = tokenResponse.Token;
							ProcessSuccessResponse(args);
							return;
						}
						
						ProcessErrorResponse(args);
						return;
					}

					ProcessSuccessResponse(args);
					break;
				}
				default:
				{
					ProcessErrorResponse(args);
					break;
				}
			}
		}

		private void ProcessSuccessResponse(GenericClientResponseEventArgs args)
		{
			// pass the response to the consuming class
			var handler = ResponseReceived;
			if (handler == null) return;

			handler(this, args);
		}

		private void ProcessErrorResponse(GenericClientResponseEventArgs args)
		{
			// pass the response to the consuming class
			var handler = ResponseReceived;
			if (handler == null) return;

			handler(this, args);
		}

		#endregion

		// Checks request queue and issues next request
		private void CheckRequestQueue()
		{
			Debug.Console(AutomateVxDebug.Verbose, this, "CheckRequestQueue: _requestQueue.Count = {0}", _requestQueue.Count);
			var nextRequest = _requestQueue.TryToDequeue();
			Debug.Console(AutomateVxDebug.Verbose, this, "CheckRequestQueue: _requestQueue.TryToDequeue was {0}",
				(nextRequest == null) ? "unsuccessful" : "successful");
			if (nextRequest != null)
			{
				nextRequest();
			}
		}
	}
}