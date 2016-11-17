using System;
using System.Configuration;
using System.IO;
using Tethr.Api;
using Tethr.Api.Model;

//////////////////////////////////////////////////////////////////////////////
// The credentials for connecting to the Tethr environment
// are stored in the App.config file (UploadRecording.exe.config after build)
// under these keys:
//  <appSettings>
//    <add key = "apiUri" value="Uri goes here" />
//    <add key = "apiUser" value="API user name goes here" />
//    <add key = "apiPassword" value="API password goes here" />
//  </appSettings>
//
// Invoke this demo app by passing the path to a json file (which is preformatted for the HTTPS request)
// or to an XML file containing the Numonix metadata. A wav file is assumed to be "next to" the 
// metadata file and have the same name.
//
// For the purpose of this demo program, the CallDirection field must exist in the xml file.



namespace UploadRecordingSample
{
    class Program
    {
        private static OauthApiConnection _apiConnection;
        static void Main(string[] args)
        {
            var apiUri = new Uri(ConfigurationManager.AppSettings["apiUri"]);
            var apiUser = ConfigurationManager.AppSettings["apiUser"];
            var apiPassword = ConfigurationManager.AppSettings["apiPassword"];

            // The OauthApiConnection object should be reused on subsequent sends so that
            // the oauth bearer token can be reused and refreshed only when it expires
            _apiConnection = new OauthApiConnection();
            _apiConnection.Initialize(apiUri, apiUser, apiPassword, null);
            var tethrConnection = new TethrConnection(_apiConnection);

            var fileName = args.Length < 2 ? "SampleRecording.json" : args[1] ?? "";

            var jsonStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var wavStream = new FileStream(Path.ChangeExtension(fileName, ".wav"), FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            var recording = jsonStream.JsonDeserialize<RecordingInfo>();

            // The sessionID must be unique per recording
            // Here we are appending the time so we can test with the same file multiple times.
            recording.SessionId += DateTime.UtcNow.ToString("o");

            var result = tethrConnection.SendRecording(recording, wavStream);

            Console.WriteLine("Sent recording to Tethr as {0}", result.Result.CallId);
        }
    }
}
