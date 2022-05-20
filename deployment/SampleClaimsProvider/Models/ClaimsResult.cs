// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Newtonsoft.Json;

    public class ClaimsResult
    {
        [JsonProperty("Claims")]
        public Dictionary<string, Collection<string>> Claims { get; set; }

        [JsonProperty("ExpiryTime")]
        public DateTimeOffset ExpiryTime { get; set; }

        [JsonProperty("UserId")]
        public string UserId { get; set; }

        [JsonProperty("VehicleId")]
        public string VehicleId { get; set; }
    }
}
