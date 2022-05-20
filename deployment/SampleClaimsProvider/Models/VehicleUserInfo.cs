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

    public class VehicleUserInfo
    {
        private Dictionary<string, Collection<string>> claims;

        public Dictionary<string, Collection<string>> Claims { get => this.claims ??= new Dictionary<string, Collection<string>>(StringComparer.OrdinalIgnoreCase); set => this.claims = value; }

        public DateTimeOffset ExpiryTime { get; set; }

        public string UserId { get; set; }

        public string VehicleId { get; set; }
    }
}
