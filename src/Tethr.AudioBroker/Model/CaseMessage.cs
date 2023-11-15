using System;

namespace Tethr.AudioBroker.Model
{
    /// <summary>
    /// The text message details.
    /// </summary>
    public class CaseMessage
    {
        /// <summary>
        /// The datetime of the message in UTC.
        /// </summary>
        public DateTime UtcTimestamp { get; set; }

        /// <summary>
        /// Reference ID of the contact who sent the message.
        /// </summary>
        public string SenderReferenceId { get; set; }
		
        /// <summary>
        /// The communication channel.
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// The message body or content of the interaction.
        /// </summary>
        public string Content { get; set; }
    }
}