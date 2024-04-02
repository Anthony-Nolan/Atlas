using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Common.Utils.Extensions
{
    internal static class JsonSerializerExtensions
    {
        public static T DeserializeFromStream<T>(this JsonSerializer serializer, Stream stream)
        {
            using var reader = new StreamReader(stream);

            return (T)serializer.Deserialize(reader, typeof(T));
        }

        public static void SerializeToStream(this JsonSerializer serializer, object data, Stream stream)
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);

            serializer.Serialize(writer, data);
        }
    }
}
