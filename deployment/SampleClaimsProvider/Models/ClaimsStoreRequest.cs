// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ClaimsStoreRequest
    {
        [JsonProperty("vehicleId")]
        public string? VehicleId { get; set; }

        [JsonProperty("userId")]
        public string? UserId { get; set; }

        [JsonProperty("serviceId")]
        public string? ServiceId { get; set; }

        [JsonProperty("claims")]
        public List<StringClaim>? Claims { get; set; }
    }
}
