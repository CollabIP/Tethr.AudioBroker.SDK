namespace Tethr.AudioBroker.Model
{
    /// <summary>
    /// Response when creating or updating a case.
    /// </summary>
    public class CaseResponse
    {
        /// <summary>
        /// The external case ID that was sent in the request.
        /// </summary>
        public string ReferenceId { get; set; }

        /// <summary>
        /// The ID that uniquely identifies the case as it exists in Tethr.
        /// </summary>
        /// <remarks>
        /// This is the primary ID for the case in Tethr, and wherever possible should be stored by the integrating system for troubleshooting purposes.
        /// </remarks>
        public string CaseId { get; set; }
	
        /// <summary>
        /// The Collection ID this interaction is associated to.
        /// </summary>
        public string CollectionId { get; set; }
    }
}