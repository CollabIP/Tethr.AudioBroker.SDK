using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Tethr.AudioBroker;
using Tethr.AudioBroker.Model;

namespace Tethr.UploadRecordingSample
{
	/// <summary>
	/// A sample of a timer that can send a heart beat update to Tethr at a set interval.
	/// </summary>
	/// <remarks>
	/// This would be a singleton that is created during application startup.
	/// </remarks>
	public sealed class TethrHeartbeatTimerSample : IDisposable
	{
		private const uint Period = 60000; // send every 60 seconds

		private readonly ITethrHeartbeat _heartbeat;

		private readonly Timer _internalTimer;
		private readonly ILog _log = LogManager.GetLogger(typeof(TethrHeartbeatTimerSample));
		private readonly object _syncRoot = new object();
		private bool _disposed;

		private Func<MonitorStatus> _getStatusCallback;

		public TethrHeartbeatTimerSample(ITethrHeartbeat heartbeat)
		{
			_heartbeat = heartbeat;
			// Using due time and not period in timer so that if our call to Tethr
			// takes longer then then the period, we don't try to send two at a time.
			_internalTimer = new Timer(Callback, null, Period, 0);
		}

		/// <summary>
		/// Setup a call back for checking the status health of the broker.
		/// </summary>
		/// <remarks>
		/// This allows for the broker to check other internal status indicators to see if the system is healthy.
		/// If a call back is not set, a default value of Healthy will be sent to Tethr.
		/// </remarks>
		/// <param name="getStatusCallback">A function that will return the current status of the broker</param>
		public void SetupCallBack(Func<MonitorStatus> getStatusCallback)
		{
			if (_getStatusCallback != null)
				throw new InvalidOperationException("Call back already set.");
			_getStatusCallback = getStatusCallback;
		}

		private void Callback(object state)
		{
			if (_disposed)
				return;

			lock (_syncRoot)
			{
				try
				{
					var status = MonitorStatus.Healthy;
					var cb = _getStatusCallback;
					if (cb != null)
						status = cb();

					// Spin up in a new Task so we have a full Synchronization context
					// If we don't do this, it's possible for this thread to get stuck.
					Task.Run(() => _heartbeat.Send(status)).GetAwaiter().GetResult();
					Online = true;
				}
				catch (Exception e)
				{
					// Because timer call backs are on their own threads,
					// we must capture all exception, or it will result
					// in an unhandled exception and the process will terminate.

					if (Online)
					{
						// State is transitioning, we will log as an error
						_log.Error("Error sending heart beat to Tethr.  " + e.Message, e);
						Online = false;
					}
					else if (_log.IsTraceEnabled)
					{
						// We don't want to flood the logs with errors, so we will just send it as a trace.
						_log.Trace("Error sending heart beat to Tethr.  " + e.Message, e);
					}
				}
				finally
				{
					if (!_disposed)
						_internalTimer.Change(Period, 0);
				}
			}
		}

		/// <summary>
		/// Indicates that our connection to Tethr is online, and we can send call to Tethr.
		/// </summary>
		public bool Online { get; private set; }

		~TethrHeartbeatTimerSample()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				Dispose(true);
			}
		}

		private void Dispose(bool disposing)
		{
			_disposed = true;
			if (disposing)
			{
				WaitHandle wh = new ManualResetEvent(false);
				_internalTimer.Dispose(wh);
				wh.WaitOne();
			}
		}
	}
}
