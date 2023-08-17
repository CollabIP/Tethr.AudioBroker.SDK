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
        Task SetExcludedStatusAsync(string sessionId);
        Task SetExcludedStatusAsync(IEnumerable<string> sessionIds);
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
            // Setting the Audio Format to make sure it matches the media type
            // Note that ArchiveController still validates this, when it could actually set it 
            // the way we do here, and rely on Tethr to validate.
            // RecordingInfo.Audio will be obsoleted at some point in favor of only looking at the media type.
            var audioFormat = mediaType.MimeTypeToAudioExtension();
            if (string.IsNullOrEmpty(audioFormat))
            {
                throw new ArgumentException($"Invalid file type {audioFormat}, valid types are {MimeAudioExtensions.SupportedAudioExtensions()}");
            }
            
            info.Audio = new Audio { Format = audioFormat };
            
            var result = await
                _tethrSession.PostMultiPartAsync<ArchiveCallResponse>("/callCapture/v1/archive", info, waveStream, mediaType);

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

        public async Task SetExcludedStatusAsync(string sessionId)
        {
            await SetExcludedStatusAsync(new[] { sessionId });
        }
        
        public async Task SetExcludedStatusAsync(IEnumerable<string> sessionIds)
        {
            await _tethrSession.PostAsync("/callCapture/v1/status/exclude", new { CallSessionIds = sessionIds });
        }
    }
}
