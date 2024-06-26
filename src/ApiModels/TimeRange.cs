﻿using System.Text.Json.Serialization;

namespace LocalCacheChecker.ApiModels
{

    public record TimeRange
    {

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Start { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Stop { get; init; }

    }

}