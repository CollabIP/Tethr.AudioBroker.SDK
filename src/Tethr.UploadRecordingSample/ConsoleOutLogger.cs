using System;
using System.Text;
using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;

namespace Tethr.UploadRecordingSample
{
	/// <summary>
	/// Simple logger for logging to console.
	/// </summary>
	public class ConsoleOutLogger : AbstractSimpleLogger
	{
		public ConsoleOutLogger(string logName, LogLevel logLevel, bool showlevel, bool showDateTime, bool showLogName,
			string dateTimeFormat) : base(logName, logLevel, showlevel, showDateTime, showLogName, dateTimeFormat)
		{
		}

		protected override void WriteInternal(LogLevel level, object message, Exception exception)
		{
			var sb = new StringBuilder();
			FormatOutput(sb, level, message, exception);
			
			Console.Out.WriteLine(sb.ToString());
		}
	}

	/// <summary>
	/// Factory adapter for <see cref="ConsoleOutLogger"/>.
	/// </summary>
	public class ConsoleOutLoggerFactoryAdapter : AbstractSimpleLoggerFactoryAdapter
	{
		public ConsoleOutLoggerFactoryAdapter()
			: base(null)
		{ }
		
		public ConsoleOutLoggerFactoryAdapter(NameValueCollection properties) : base(properties)
		{
		}

		public ConsoleOutLoggerFactoryAdapter(LogLevel level, bool showDateTime, bool showLogName, bool showLevel,
			string dateTimeFormat) : base(level, showDateTime, showLogName, showLevel, dateTimeFormat)
		{
		}

		protected override ILog CreateLogger(string name, LogLevel level, bool showLevel, bool showDateTime,
			bool showLogName,
			string dateTimeFormat)
		{
			return new ConsoleOutLogger(name, level, showLevel, showDateTime, showLogName, dateTimeFormat);
		}
	}
}