using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tethr.AudioBroker.Model
{
	public class ChatSession
	{
		/// <summary>
		/// (Required) The sessionID is unique identify per recording.
		/// </summary>
		/// <remarks>
		/// Session ID needs to be unique to any and all recordings uploaded to a given instance of Tethr
		/// 
		/// When not used in conjunction with a MasterCallId, Session ID should be something 
		/// that can be used by other systems to connect a call in Tethr to the same call in other systems.
		/// 
		/// When a MasterCallId is provided, the Master call ID can be used link a Tethr call to other systems.
		/// But the SessionID should still be something that the originating call recorder can use to lookup
		/// a given recording with.  This allows for the recorder to validate that Tethr did in fact get a call
		/// as part of a regular integrity or verification process.
		/// </remarks>
		[JsonProperty(Required = Required.Always)]
		public string SessionId { get; set; }

		/// <summary>
		/// The start time of the Chat session, this value should be in UTC.
		/// </summary>
		[JsonProperty(Required = Required.Always)]
		public DateTime UtcStart { get; set; }
		
		/// <summary>
		/// The end time of the Chat session, this value should be in UTC.
		/// </summary>
		[JsonProperty(Required = Required.Always)]
		public DateTime UtcEnd { get; set; }

		public List<ChatContact> Contacts { get; set; }

		/// <summary>
		/// Any extra metadata related to the chat that could be used by Tethr.
		/// </summary>
		/// <remarks>
		/// The metadata is read by Tethr and used classify chats, Examples:
		/// Contact Center the chat was answered in, the disposition code entered by the agent,
		/// the skill group / or product the call was related to, the chat routing information
		/// 
		/// There is really no limit to what can be put in there, as long as it can be converted to JSON.
		/// </remarks>
		public object Metadata { get; set; }
	}
}
