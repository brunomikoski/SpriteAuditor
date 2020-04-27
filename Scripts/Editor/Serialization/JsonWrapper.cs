using FullSerializer;

namespace BrunoMikoski.AtlasAudior.Serialization
{
    public static class JsonWrapper
    {
        public static string ToJson<T>(T target, bool pretty = true)
        {
            fsSerializer serializer = new fsSerializer();

            fsData data;
            fsResult result = serializer.TrySerialize<T>(target, out data);
            if (!result.Failed)
            {
                fsSerializer.StripDeserializationMetadata(ref data);
                if (pretty)
                    return fsJsonPrinter.PrettyJson(data);
                else
                    return fsJsonPrinter.CompressedJson(data);
            }
            else
            {
                UnityEngine.Debug.LogError(result.FormattedMessages);
                return null;
            }
        }

        public static bool FromJson<T>(string json, ref T instance)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            fsSerializer serializer = new fsSerializer();

            fsData data = fsJsonParser.Parse(json);
            fsResult result = serializer.TryDeserialize(data, ref instance);

            if (result.Failed)
            {
                UnityEngine.Debug.LogError(result.FormattedMessages);
                return false;
            }

            return true;
        }
    }
}