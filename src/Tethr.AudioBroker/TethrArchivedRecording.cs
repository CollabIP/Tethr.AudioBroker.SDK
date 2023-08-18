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

        private static readonly Dictionary<string, string> MimeTypeMappings = new Dictionary<string, string>()
        {
            { "audio/wav", "wav" }, { "audio/x-wav", "wav" }, { "audio/wave", "wav" }, { "audio/vnd.wav", "wav" },
            { "audio/x-wave", "wav" }, { "audio/mp3", "mp3" }, { "audio/ogg", "opus" }, { "audio/mp4", "mp4" },
            { "audio/m4a", "mp4" }, { "audio/mp4-helium", "mp4helium" }, { "audio/m4a-helium", "mp4helium" },
            { "audio/wma", "wma" }, { "audio/wma-helium", "wmahelium" }
        };
        
        private static string MediaTypeToTethrType(string mimeType)
        {
            if(string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            return MimeTypeMappings.TryGetValue(mimeType.ToLower(), out var returnType) ? returnType : mimeType;
        }

        private readonly ITethrSession _tethrSession;

        public TethrArchivedRecording(ITethrSession tethrSession)
        {
            _tethrSession = tethrSession;
        }

        public async Task<ArchiveCallResponse> SendRecordingAsync(RecordingInfo info, Stream waveStream, string mediaType)
        {
            // check the media types, and convert them to the types that Tethr is expecting.
            // Allow the user to put in other types, that maybe the API supports now but the SDK doesn't
            // yet have the mapping from media type.
            var audioFormat = MediaTypeToTethrType(mediaType);
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
