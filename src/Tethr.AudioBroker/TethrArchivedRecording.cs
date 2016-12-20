using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

namespace Tethr.AudioBroker
{
    /// <summary>
    /// Interface for working with call archives where the call recording has already completed and the audio is available for the entire call.
    /// </summary>
    public interface ITethrArchivedRecording
    {
        Task<ArchiveCallResponse> SendRecordingAsync(RecordingInfo info, Stream waveStream, string mediaType);
        Task<SessionStatus> GetRecordingStatusAsync(string sessionId);
        Task<SessionStatuses> GetRecordingStatusAsync(IEnumerable<string> sessionIds);
    }

    public class TethrArchivedRecording : ITethrArchivedRecording
    {
        private readonly ITethrSession _tethrSession;

        public TethrArchivedRecording(ITethrSession tethrSession)
        {
            _tethrSession = tethrSession;
        }

        public async Task<ArchiveCallResponse> SendRecordingAsync(RecordingInfo info, Stream waveStream, string mediaType)
        {
            // Setting the Audio Format to make sure it matches the media type, RecordingInfo.Audio will be obsoleted at some point in favor of only looking at the media type.
            if (string.Equals("audio/x-wav", mediaType, StringComparison.OrdinalIgnoreCase)
                || string.Equals("audio/wave", mediaType, StringComparison.OrdinalIgnoreCase)
                || string.Equals("audio/vnd.wav", mediaType, StringComparison.OrdinalIgnoreCase)
                || string.Equals("audio/x-wave", mediaType, StringComparison.OrdinalIgnoreCase)
                || string.Equals("audio/wav", mediaType, StringComparison.OrdinalIgnoreCase))
            {
                // Setting the audio value as some instance of Tethr may still be looking for this.
                // Will be removed from SDK, once it is fully removed from Tethr servers.
                info.Audio = new Audio { Format = "wav" };
                // Set the media type to the one used by default in Tethr.
                mediaType = "audio/wav";
            }
            else if (string.Equals("audio/mp3", mediaType, StringComparison.OrdinalIgnoreCase))
            {
                info.Audio = new Audio { Format = "mp3" };
            }
            else
            {
                //Check the file type is wav, If they are not attaching a file, we only support Wav files.
                throw new ArgumentException("Only Wav or MP3 files are supported files.");
            }

            var result = await
                _tethrSession.PostMutliPartAsync<ArchiveCallResponse>("/callCapture/v1/archive", info, waveStream, mediaType);

            return result;
        }

        public async Task<SessionStatus> GetRecordingStatusAsync(string sessionId)
        {
            return (await GetRecordingStatusAsync(new[] { sessionId }))?.CallSessions?.FirstOrDefault();
        }

        public async Task<SessionStatuses> GetRecordingStatusAsync(IEnumerable<string> sessionIds)
        {
            var result = await
                _tethrSession.PostAsync<SessionStatuses>("/callCapture/v1/status", new { CallSessionIds = sessionIds });

            return result;
        }
    }
}