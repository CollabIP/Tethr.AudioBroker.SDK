using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

namespace Tethr.AudioBroker
{
	public interface ITethrHeartbeat
	{
		Task Send(MonitorEvent monitorEvent);
	}

	public static class TethrHeartbeatExtensions
	{
		public static async Task Send(this ITethrHeartbeat heartbeat, MonitorStatus monitorStatus)
		{
			await heartbeat.Send(new MonitorEvent
			{
				Name = Environment.MachineName,
				Status = monitorStatus,
				TimeStamp = DateTimeOffset.UtcNow
			});
		}
	}

	/// <summary>
	/// Used to send Tethr a heartbeat from a broker.  Allowing you to then monitor if a given broker is able to correctly send data to Tethr at all times.
	/// </summary>
	/// <remarks>
	/// An endpoint can be made available in Tethr to allow you to connect up your own monitoring or alerting system to see that status of a given broker.
	/// This is used to give a high level alert of any possible issue that may prevent a broker from uploading calls to Tethr.
	/// </remarks>
	public class TethrHeartbeat : ITethrHeartbeat
	{
		private readonly ITethrSession _tethrSession;

		public TethrHeartbeat(ITethrSession tethrSession)
		{
			_tethrSession = tethrSession;
		}

		public async Task Send(MonitorEvent monitorEvent)
		{
			await _tethrSession.PostAsync(@"callCapture/v1/monitor", monitorEvent);
		}
	}
}
