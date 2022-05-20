// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IClaimsProviderStore
    {
        Task CreateClaimsAsync(string vehicleId, string userId, string serviceId, IList<StringClaim> claims);

        Task<VehicleUserInfo> RefreshClaimsInfoAsync(string vehicleId, string userId, List<KeyValuePair<string, string>> claimsList, IList<string> paths);

        Task RemoveClaimsAsync(string vehicleId, string userId, string serviceId);

        Task<VehicleUserInfo> RetrieveVehicleUserInfoAsync(string vehicleId, string userId, string authToken, IList<string> paths);
    }
}