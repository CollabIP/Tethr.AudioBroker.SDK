using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Tethr.AudioBroker.Model;

namespace Tethr.AudioBroker
{
    /// <summary>
    /// Request for creating a case.
    /// </summary>
    /// <example>
    /// {"referenceId":"case1234","metadata":{"field__c":"a value"},"utcStart":"2017-08-16T19:30:00Z","utcEnd":"2017-08-16T19:35:00Z","messages":[{"senderReferenceId":"C1","utcTimestamp":"2017-08-16T19:30:00Z","content":"Hello, I need some help!","channel":"Web"}],"contacts":[{"referenceId":"C1","firstName":"Jane","lastName":"Doe","email":"jane@doe.org","phoneNumber":"4155551212","type":"customer"}]}
    /// </example>
    public class CaseSession
    {
        /// <summary>
        /// The external case ID from the source system.
        /// </summary>
        public string ReferenceId { get; set; }
        
        /// <summary>
        /// The master ID can be used to group cases together in a collection
        /// to create a composite interaction. Analysis will generally treat
        /// a composite interaction as a larger single interaction 
        /// </summary>
        public string MasterId { get; set; }
        
        /// <summary>
        /// The Collection ID will group cases together, but not treat them
        /// as a single large interaction for the purpose of analysis.
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// The metadata attached to the case.
        /// </summary>
        public JObject Metadata { get; set; }

        /// <summary>
        /// The datetime when the case was opened in UTC.
        /// </summary>
        public DateTime UtcStart { get; set; }
		
        /// <summary>
        /// The datetime when the case was closed in UTC.
        /// </summary>
        public DateTime UtcEnd { get; set; }

        /// <summary>
        /// The messages for the case.
        /// </summary>
        public List<CaseMessage> Messages { get; set; }
		
        /// <summary>
        /// The contacts that received the message.
        /// </summary>
        public List<CaseContact> Contacts { get; set; }
    }
}