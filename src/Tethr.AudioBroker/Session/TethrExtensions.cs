using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Tethr.AudioBroker.Session
{
    public static class TethrExtensions
    {
        internal static HttpWebResponse Get(this HttpWebRequest webRequest)
        {
            webRequest.Method = "GET";
            return (HttpWebResponse)webRequest.GetResponse();
        }

        internal static async Task<HttpWebResponse> PostAsync<T>(this HttpWebRequest request, T data)
        {
            request.ContentType = "application/json; charset=utf-8";
            request.Method = "POST";
            using (var stream = request.GetRequestStream())
            {
                stream.JsonSerialize(new { Test = true });
            }

            return (HttpWebResponse)await request.GetResponseAsync();
        }

        internal static bool IsSuccessStatusCode(this HttpWebResponse response)
        {
            if (response.StatusCode >= HttpStatusCode.OK)
                return response.StatusCode <= (HttpStatusCode)299;
            return false;
        }

        internal static HttpWebResponse EnsureSuccessStatusCode(this HttpWebResponse response)
        {
            if (!response.IsSuccessStatusCode())
            {
                throw new HttpRequestException(string.Format(CultureInfo.InvariantCulture, "Response status code does not indicate success: {0} ({1}).", new object[]
                {
                    (int)response.StatusCode,
                    response.StatusDescription
                }));
            }

            return response;
        }

        public static void JsonSerialize<T>(this Stream stream, T value, JsonSerializerSettings serializerSettings = null)
        {
            var serializer = JsonSerializer.CreateDefault(serializerSettings);
            using (var writer = new StreamWriter(stream))
            using (var jsonTextWriter = new JsonTextWriter(writer))
            {
                serializer.Serialize(jsonTextWriter, value);
            }
        }

        public static T JsonDeserialize<T>(this Stream stream)
        {
            var serializer = JsonSerializer.CreateDefault(null);
            using (var reader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(reader))
            {
                return serializer.Deserialize<T>(jsonTextReader);
            }
        }

        public static IEnumerable<T[]> BatchesOf<T>(this IEnumerable<T> sequence, int batchSize)
        {
            // TODO: look at using Partitioners
            var batch = new List<T>(batchSize);
            foreach (var item in sequence)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch.ToArray();
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                yield return batch.ToArray();
                batch.Clear();
            }
        }
    }
}