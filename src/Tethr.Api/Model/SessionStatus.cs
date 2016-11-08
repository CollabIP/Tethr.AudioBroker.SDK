using System.Collections.Generic;

namespace Tethr.Api.Model
{
    public enum CallStatus
    {
        /// <summary>
        /// The call currently in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// The Call has completed, but we are still processing it.
        /// </summary>
        Concluded,

        /// <summary>
        /// The call has completed, and we are done processing it.
        /// </summary>
        Complete,

        /// <summary>
        /// No call was found for the session.
        /// </summary>
        NotFound,

        Error
    }
    public class SessionStatuses
    {
        public IEnumerable<SessionStatus> CallSessions { get; set; }
    }

    public class SessionStatus
    {
        public CallStatus Status { get; set; }

        public string CallId { get; set; }

        public string SessionId { get; set; }
    }
}