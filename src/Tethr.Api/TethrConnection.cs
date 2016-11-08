using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tethr.Api.Model;

namespace Tethr.Api
{
    public interface ITethrConnection
    {
        Task<ArchiveCallResponse> SendRecording(RecordingInfo info, Stream waveStream);
        Task<SessionStatus> GetRecordingStatus(string sessionId);
        Task<SessionStatuses> GetRecordingStatus(IEnumerable<string> sessionIds);
        Task<IEnumerable<RecordingSettingSummary>> GetRecordingSettingsSummaries();
    }

    public class TethrConnection : ITethrConnection
    {
        private readonly IOauthApiConnection _apiConnection;

        public TethrConnection(IOauthApiConnection apiConnection)
        {
            _apiConnection = apiConnection;
        }

        public async Task<ArchiveCallResponse> SendRecording(RecordingInfo info, Stream waveStream)
        {
            var result = await
                _apiConnection.PostMutliPartAsync<ArchiveCallResponse>("/callCapture/v1/archive", waveStream, info);

            return result;
        }

        public async Task<SessionStatus> GetRecordingStatus(string sessionId)
        {
            var result = await
                _apiConnection.GetAsync<SessionStatus>($"/callCapture/v1/status/{sessionId}");

            return result;
        }

        public async Task<SessionStatuses> GetRecordingStatus(IEnumerable<string> sessionIds)
        {
            var result = await
                _apiConnection.PostAsync<SessionStatuses>("/callCapture/v1/status", new { CallSessionIds = sessionIds });

            return result;
        }

        public async Task<IEnumerable<RecordingSettingSummary>> GetRecordingSettingsSummaries()
        {
            var result = await
                _apiConnection.GetAsync<IEnumerable<RecordingSettingSummary>>("/sources/v1/recordingSettings");

            return result;
        }
    }
}