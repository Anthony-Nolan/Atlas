using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Schema;

namespace Atlas.Utils.Test.CoreUtils.Json
{
    public static class JsonUtils
    {
        public static JSchema LoadSchemaFromResource(Assembly assembly, string path)
        {
            return JSchema.Parse(GetFile(assembly, path));
        }

        public static StringContent LoadJsonContent(Assembly assembly, string path)
        {
            return new StringContent(GetFile(assembly, path), Encoding.UTF8, "application/json");
        }

        private static string GetFile(Assembly assembly, string path)
        {
            var assemblyName = assembly.GetName().Name;
            var resourceName = path.Replace('\\', '.').Replace('/', '.');
            var fullManifestName = $"{assemblyName}.{resourceName}";

            using (var stream = assembly.GetManifestResourceStream(fullManifestName))
            {
                if (stream == null)
                {
                    throw new ArgumentException($"File {path} not found. Please make sure it's marked as an embedded resource.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
