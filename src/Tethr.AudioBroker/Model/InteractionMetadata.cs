using System;
using Newtonsoft.Json;

namespace Tethr.AudioBroker.Model
{
	/// <summary>
	/// Metadata to append to interactions. (e.g. Call, Case)
	/// </summary>
	public abstract class InteractionMetadataBase
	{
		/// <summary>
		/// The time stamp of the event this metadata represents.
		/// </summary>
		/// <remarks>
		/// Async Metadata is applied in sequential orders, so if two events contain the same field the value from the later event will be used.
		/// </remarks>
		public DateTime EventTime { get; set; }

		/// <summary>
		/// The free-form JSON object that will be appended to the interaction metadata.
		/// </summary>
		/// <remarks>
		/// The metadata is read by Tethr and used classify calls, Examples:
		/// Contact Center the call was answered in, the disposition code entered by the agent,
		/// the skill group / or product the call was related to, the call routing group from the IVR,
		/// an identifier for phone the agent used to make the call.
		/// 
		/// There is really no limit to what can be put in there, as long as it can be converted to a JToken.
		/// </remarks>
		[JsonProperty(Required = Required.Always)]
		public object Metadata { get; set; }
	}

	/// <summary>
	/// Metadata to append to the interactions with the specified session Id.
	/// </summary>
	/// <example>{"sessionId":"string","eventTime":"2020-09-10T03:16:11.190Z","metadata":{"field__c":"a value"}}</example>
	public class SessionInteractionMetadata : InteractionMetadataBase
	{
		/// <summary>
		/// The session Id for the call or chat this metadata should be appended to.
		/// </summary>
		[JsonProperty(Required = Required.Always)]
		public string SessionId { get; set; }
	}

	/// <summary>
	/// Metadata to append to all interactions with the specified master call Id.
	/// </summary>
	/// <example>
	/// {"masterCallId":"string","eventTime":"2020-09-10T03:16:11.190Z","metadata":{"field__c":"a value"}}
	/// </example>
	public class MasterInteractionMetadata : InteractionMetadataBase
	{
		/// <summary>
		/// The master call Id the metadata should be appended to.
		/// </summary>
		[JsonProperty(Required = Required.Always)]
		public string MasterCallId { get; set; }
	}

	/// <summary>
	/// Metadata to append to the case with the specified case reference Id.
	/// </summary>
	/// <example>{"caseReferenceId":"string","eventTime":"2020-09-10T03:16:11.190Z","metadata":{"field__c":"a value"}}</example>
	public class CaseInteractionMetadata : InteractionMetadataBase
	{
		/// <summary>
		/// The Case reference Id, from the source system, this metadata should be appended to.
		/// </summary>
		[JsonProperty(Required = Required.Always)]
		public string CaseReferenceId { get; set; }
	}
}