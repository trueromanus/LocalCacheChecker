using System.Text.Json;

namespace LocalCacheChecker.Helpers {

    internal static class JsonHelpers {

        public static string SerializeToJson<T> ( T model ) => JsonSerializer.Serialize<T> (
                model,
                new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                }
            );

        public static T? DeserializeFromJson<T> ( string json ) => JsonSerializer.Deserialize<T> (
                json,
                options: new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                }
            );

    }

}
