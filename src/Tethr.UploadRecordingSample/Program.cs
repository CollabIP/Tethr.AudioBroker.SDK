﻿using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tethr.AudioBroker;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

//////////////////////////////////////////////////////////////////////////////
// The credentials for connecting to the Tethr environment
// are stored in the App.config file (UploadRecording.exe.config after build)
// 
// <add name="Tethr" connectionString="uri=https://YourCompanyNameHere.Audio.Tethr.io/;ApiUser=YourUserNameHere;Password=YourPasswordHere" />
//
// Invoke this demo app by passing the path to a json file (which is preformatted for the HTTPS request)
// A wav file is assumed to be "next to" the metadata file and have the same name.
//////////////////////////////////////////////////////////////////////////////

namespace Tethr.UploadRecordingSample
{
	internal class Program
	{
		// The TethrSession object should be a singleton, and reused on subsequent sends so that
		// the oauth bearer token can be reused and refreshed only when it expires
		private static TethrSession _tethrSession;

		static void Main(string[] args)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			
			// Setup console Logger with default settings
			var factory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
			});
			var logger = factory.CreateLogger<TethrSession>();

			// Set the product info header, used to update the HTTP User-Agent for requests to Tethr.
			var programType = typeof(Program);
			TethrSession.SetProductInfoHeaderValue(programType.Namespace, programType.Assembly.GetName().Version.ToString());

			// Get the connection string so that we can initialize a Tethr session
			var connectionStringSettings = ConfigurationManager.ConnectionStrings["Tethr"];
			if (connectionStringSettings == null)
			{
				throw new InvalidOperationException(
					$"Could not find a connection string for Tethr. Please add a connection string in the <ConnectionStrings> section of the application's configuration file. For example: <add name=\"Tethr\" connectionString=\"uri=https://YourCompanyNameHere.Audio.Tethr.io/;ApiUser=YourUserNameHere;Password=YourPasswordHere\" />");
			}

			// Initialize new options from the connection string
			var sessionOptions = TethrSessionOptions.InitializeFromConnectionString(connectionStringSettings.ConnectionString);
			
			// Create a Tethr Session and store it for use when we make our API calls.
			_tethrSession = new TethrSession(sessionOptions, logger); // {DefaultProxy = new WebProxy("http://127.0.0.1:8888", false) };

			// Send the file, and then await the result.
			var fileName = args.Length < 1 ? "SampleRecording.json" : args[0] ?? "";
			
			Console.WriteLine("Reading : " + fileName);

			var result = Task.Run(() => SendFile(fileName)).GetAwaiter().GetResult();

			Console.WriteLine("Sent recording:");
			Console.WriteLine("\tSession id       : {0}", result.SessionId);
			Console.WriteLine("\tCall Start Time  : {0} (Local Time)", result.StartTime.ToLocalTime().ToLongTimeString());
			Console.WriteLine("\tTethr call id is : {0}", result.CallId);

			// Complete.
			Console.WriteLine("Press enter to exit");
			Console.ReadLine();
		}

		private static async Task<SendFileResult> SendRawFile(string fileName)
		{
			// Create an Instance of the Archive Recording provider used to send archived audio to Tethr
			var tethrConnection = new TethrArchivedRecording(_tethrSession);

			// Figure out the file name of the sample file we are going to send to Tethr and read it in.
			using (var jsonStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				// Open the sample wave file.
				using (var wavStream = new FileStream(Path.ChangeExtension(fileName, ".wav"), FileMode.Open,
					FileAccess.Read, FileShare.Read))
				{
					var reader = new StreamReader(jsonStream);

					// Send the audio and metadata to Tethr.
					var result = await
						_tethrSession.PostMultiPartAsync<ArchiveCallResponse>("/callCapture/v1/archive", reader.ReadToEnd(), wavStream, AudioMediaTypes.Wave);

					return new SendFileResult
					{
						CallId = result.CallId,
					};
				}
			}
		}


		private static async Task<SendFileResult> SendFile(string fileName)
		{
			// Create an Instance of the Archive Recording provider used to send archived audio to Tethr
			var tethrConnection = new TethrArchivedRecording(_tethrSession);

			// Figure out the file name of the sample file we are going to send to Tethr and read it in.
			using (var jsonStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var recording = jsonStream.JsonDeserialize<RecordingInfo>();

				// NOTE: Session ID should be something that can be used by other systems to connect a call in Tethr
				// to the same call in other systems.  The sessionID must be unique per recording.
				//
				// For this example, we are appending the time so we can test with the same file multiple times.
				// but should be a more meaningful value in production.
				recording.SessionId += DateTime.UtcNow.ToString("o");

				// NOTE: it is important that the audio length and the reported call length are the same.
				// If this is not true, Tethr may have harder time accurately detecting what actually happened on a call
				// By default, Tethr will quarantine calls where the reported time and audio length do not match, and wait for
				// human interaction to fix any miss configurations.  (Contact Support if you think you need this changed)
				//
				// for this example, we are Update the call times, so the calls don't always have the same time.
				// and if you change out the audio file in the sample, you will have to re-compute the end time in the sample JSON file.
				var audioLength = recording.EndTime - recording.StartTime;
				recording.StartTime = DateTime.UtcNow;
				recording.EndTime = recording.StartTime + audioLength;

				// Open the sample wave file.
				using (var wavStream = new FileStream(Path.ChangeExtension(fileName, ".wav"), FileMode.Open,
					FileAccess.Read, FileShare.Read))
				{
					// Send the audio and metadata to Tethr.
					var result = await tethrConnection.SendRecordingAsync(recording, wavStream, AudioMediaTypes.Wave);
					return new SendFileResult
					{
						CallId = result.CallId,
						SessionId = recording.SessionId,
						StartTime = recording.StartTime
					};
				}
			}
		}

		public class SendFileResult
		{
			public string CallId { get; set; }
			public string SessionId { get; set; }
			public DateTime StartTime { get; set; }
		}
	}
}
