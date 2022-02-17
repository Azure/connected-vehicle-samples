// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ClaimsRequest
    {
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("serviceId")]
        public string ServiceId { get; set; }

        [JsonPropertyName("claims")]
        public List<StringClaim> Claims { get; set; }
    }
}
