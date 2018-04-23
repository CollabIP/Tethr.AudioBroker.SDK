using System;

namespace Tethr.AudioBroker.Model
{
	public class ChatMessage
	{
		/// <summary>
		/// The chat message text.
		/// </summary>
		public string Content { get; set; }
		
		/// <summary>
		/// The timestamp the chat message was sent
		/// </summary>
		public DateTime UtcTimestamp { get; set; }
	}
}