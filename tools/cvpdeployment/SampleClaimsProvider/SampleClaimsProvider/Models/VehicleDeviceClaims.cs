// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Text.Json.Serialization;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class VehicleDeviceClaims
    {
        [JsonPropertyName("LabeledClaims")]
        public Dictionary<string, Dictionary<string, Collection<string>>> LabeledClaims { get; set; } = new Dictionary<string, Dictionary<string, Collection<string>>>();

        [JsonPropertyName("ExpiryTime")]
        public DateTimeOffset ExpiryTime { get; set; }

        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("VehicleId")]
        public string VehicleId { get; set; }
    }
}
