namespace Tethr.AudioBroker.Model
{
    /// <summary>
    /// The contact that participates in a message.
    /// </summary>
    public class CaseContact
    {
        /// <summary>
        /// A Reference Id or name for the contact. 
        /// </summary>
        public string ReferenceId { get; set; }

        /// <summary>
        /// First name of the contact.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last name of the contact.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The email address of the contact.
        /// </summary>
        public string Email { get; set; }
		
        /// <summary>
        /// The phone number of the contact.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The type of contact. Default types are "Agent" and "Customer".
        /// </summary>
        public string Type { get; set; }
    }
}