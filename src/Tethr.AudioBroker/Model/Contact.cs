using Newtonsoft.Json;

namespace Tethr.AudioBroker.Model
{
    public class Contact
    {
        /// <summary>
        /// The audio channel of this contact in the recording.
        /// </summary>
        /// <remarks>
        /// At this time only one Contact is allowed per Channel on a given call.
        /// 
        /// Channel can be 1 or 0 based, but must match the number of channels in the audio.
        /// By default, Tethr will quarantine calls where the channels and number of audio channels
        /// do not match, and will wait for human interaction to fix any miss configurations.
        /// 
        /// (Contact Support if you think you this will happen)
        /// </remarks>
        public int Channel { get; set; }

        /// <summary>
        /// (Optional) The phone number of extension of this contact
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// (Optional) What type of participant is this contact
        /// </summary>
        /// <example> Default types: 
        /// Agent - for an internal participant,
        /// Customer - for a customer,
        /// IVR - for the IVR (Not used often)
        /// </example>
        /// <remarks>
        /// The list of Types can be defend in Tethr if new or different types are wanted.  The only predefined types are Agent and Customer.
        /// At this time only one Contact is allowed per Channel
        /// 
        /// At this time only one Contact is allowed per Type on a given call.
        /// </remarks>
        [JsonProperty(Required = Required.Always)]
        public string Type { get; set; }
    }
}