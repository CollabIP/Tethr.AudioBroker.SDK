using System;

namespace Tethr.AudioBroker.Model
{
	public class CallShare
	{
		/// <summary>
		/// The email of the guest user that will be accessing the call.
		/// </summary>
		public string Email { get; set; }
		
		/// <summary>
		/// The Tethr call id of the call to generate a link for.
		/// </summary>
		public string CallId { get; set; }
	}
	
	public class CallShareResponse
	{
		/// <summary>
		/// The time of expiration of the call link.
		/// </summary>
		public DateTime Expiration { get; set; }
		
		/// <summary>
		/// The call link that gives the guest user with the specified email access to the call.
		/// </summary>
		public string CallUrl { get; set; }
	}
}