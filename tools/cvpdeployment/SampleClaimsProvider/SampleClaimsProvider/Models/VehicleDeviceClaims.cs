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

    public class VehicleDeviceClaims
    {
        [JsonProperty("LabeledClaims")]
        public Dictionary<string, Dictionary<string, Collection<string>>> LabeledClaims { get; set; } = new Dictionary<string, Dictionary<string, Collection<string>>>();

        [JsonProperty("ExpiryTime")]
        public DateTimeOffset ExpiryTime { get; set; }

        [JsonProperty("UserId")]
        public string UserId { get; set; }

        [JsonProperty("VehicleId")]
        public string VehicleId { get; set; }
    }
}
