using Newtonsoft.Json;

namespace Tethr.Api.Model
{
    public class ArchiveCallResponse
    {
        public string CallId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string SessionId { get; set; }

        public string SourceAudioFile { get; set; }
    }
}