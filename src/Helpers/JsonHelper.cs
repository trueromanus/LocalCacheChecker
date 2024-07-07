using LocalCacheChecker.SerializerContext;
using System.Text.Json;

namespace LocalCacheChecker.Helpers {

    internal static class JsonHelpers {

        public static string SerializeToJson<T> ( T model ) => JsonSerializer.Serialize (
            model,
            typeof ( T ),
            ApiModelSerializerContext.Default
        );

        public static T? DeserializeFromJson<T> ( string json ) => (T?)JsonSerializer.Deserialize (
            json,
            typeof ( T ),
            ApiModelSerializerContext.Default
        );

    }

}
