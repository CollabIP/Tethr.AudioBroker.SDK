using System;

namespace Tethr.AudioBroker.Model
{
	public class MonitorEvent
	{
		/// <summary>
		/// The current status of the service
		/// </summary>
		public MonitorStatus Status { get; set; }

		/// <summary>
		/// The local time on the service
		/// </summary>
		public DateTimeOffset TimeStamp { get; set; }

		/// <summary>
		/// A system name used to identify an instance of the service
		/// </summary>
		/// <remarks>
		/// Used only to detect if the system name has changed.
		/// There should only be one broker per API User, and this helps detect if a second one reporting.
		/// This would normally be set to something like a computer name, but could be an instance if
		/// you are going to support more then one broker on a given system.
		/// </remarks>
		public string Name { get; set; }

	    /// <summary>
	    /// Software Name and Version
	    /// </summary>
	    /// <remarks>
	    /// This is a string indicating the software and the version number
	    /// Example: 'Numonix Broker 1.0.0.29'
	    /// </remarks>
	    public string SoftwareVersion { get; set; }

    }

    public enum MonitorStatus
	{
		/// <summary>
		/// The status is unknown or not specified at this time.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Everything is working as expected.
		/// </summary>
		Healthly = 1,
		/// <summary>
		/// The system is online, but something is out of normal parameters.
		/// </summary>
		Warning = 10,
		/// <summary>
		/// The system is online, but there are errors that should be looked at.
		/// </summary>
		Error = 20,
		/// <summary>
		/// The system is off-line, and unable to process calls at this time.
		/// </summary>
		Offline = 30
	}
}