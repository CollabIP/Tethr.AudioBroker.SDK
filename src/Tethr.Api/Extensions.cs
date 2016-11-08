using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Tethr.Api
{
    public static class Extensions
    {
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