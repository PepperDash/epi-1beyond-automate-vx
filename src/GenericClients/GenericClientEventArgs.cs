using System;

namespace OneBeyondAutomateVxEpi.GenericClients
{
	/// <summary>
	/// Cleint event args
	/// </summary>
	public class GenericClientResponseEventArgs : EventArgs
	{
		/// <summary>
		/// Client request
		/// </summary>
		public string Request { get; set; }

		/// <summary>
		/// Client response code
		/// </summary>
		public int Code { get; set; }

		/// <summary>
		/// Client response content string
		/// </summary>
		public string ContentString { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public GenericClientResponseEventArgs()
		{
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="request"></param>
		/// <param name="code"></param>
		/// <param name="contentString"></param>
		public GenericClientResponseEventArgs(string request, int code, string contentString)
		{
			Request = string.IsNullOrEmpty(request) ? "" : request;
			Code = code < 0 ? 0 : code;
			ContentString = string.IsNullOrEmpty(contentString) ? "" : contentString;
		}
	}

	/// <summary>
	/// Cleint event args
	/// </summary>
	public class GenericClientStringResponseEventArgs : EventArgs
	{
		/// <summary>
		/// Request
		/// </summary>
		public string Request { get; set; }

		/// <summary>
		/// Client response content string
		/// </summary>
		public string ContentString { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public GenericClientStringResponseEventArgs()
		{
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="request"></param>
		/// <param name="contentString"></param>
		public GenericClientStringResponseEventArgs(string request, string contentString)
		{
			Request = string.IsNullOrEmpty(request) ? "" : request;
			ContentString = string.IsNullOrEmpty(contentString) ? "" : contentString;
		}
	}
}