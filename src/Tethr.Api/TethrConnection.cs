using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tethr.Api.Model;

namespace Tethr.Api
{
    public interface ITethrConnection
    {
        Task<ArchiveCallResponse> SendRecordingAsync(RecordingInfo info, Stream waveStream);
        Task<SessionStatus> GetRecordingStatusAsync(string sessionId);
        Task<SessionStatuses> GetRecordingStatusAsync(IEnumerable<string> sessionIds);
        Task<IEnumerable<RecordingSettingSummary>> GetRecordingSettingsSummariesAsync();
    }

    public class TethrConnection : ITethrConnection
    {
        private readonly IOauthApiConnection _apiConnection;

        public TethrConnection(IOauthApiConnection apiConnection)
        {
            _apiConnection = apiConnection;
        }

        public async Task<ArchiveCallResponse> SendRecordingAsync(RecordingInfo info, Stream waveStream)
        {
            var result = await
                _apiConnection.PostMutliPartAsync<ArchiveCallResponse>("/callCapture/v1/archive", waveStream, info);

            return result;
        }

        public async Task<SessionStatus> GetRecordingStatusAsync(string sessionId)
        {
            var result = await
                _apiConnection.GetAsync<SessionStatus>($"/callCapture/v1/status/{sessionId}");

            return result;
        }

        public async Task<SessionStatuses> GetRecordingStatusAsync(IEnumerable<string> sessionIds)
        {
            var result = await
                _apiConnection.PostAsync<SessionStatuses>("/callCapture/v1/status", new { CallSessionIds = sessionIds });

            return result;
        }

        public async Task<IEnumerable<RecordingSettingSummary>> GetRecordingSettingsSummariesAsync()
        {
            var result = await
                _apiConnection.GetAsync<IEnumerable<RecordingSettingSummary>>("/sources/v1/recordingSettings");

            return result;
        }
    }
}