using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tethr.AudioBroker.Model;
using Tethr.AudioBroker.Session;

namespace Tethr.AudioBroker
{
    public interface ITethrRecordingSettings
    {
        Task<IEnumerable<RecordingSettingSummary>> GetRecordingSettingsSummariesAsync();
    }

    public class TethrRecordingSettings : ITethrRecordingSettings
    {
        private readonly ITethrSession _tethrSession;

        public TethrRecordingSettings(ITethrSession tethrSession)
        {
            _tethrSession = tethrSession;
        }

        public async Task<IEnumerable<RecordingSettingSummary>> GetRecordingSettingsSummariesAsync()
        {
            var result = await
                _tethrSession.GetAsync<IEnumerable<RecordingSettingSummary>>("/sources/v1/recordingSettings");

            return result;
        }
    }
}
