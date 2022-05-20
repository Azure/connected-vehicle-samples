// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    /// <summary>
    /// Creates the Cosmos DB document Id values.
    /// </summary>
    public static class DocumentIdCreator
    {
        public static string CreateClaimDocumentId(ClaimsRequest request) => CreateClaimDocumentId(request.VehicleId, request.UserId, request.ServiceId);
        public static string CreateClaimDocumentId(string vehicleId, string userId, string entityId) => $"Claim|{vehicleId}|{userId}|{entityId}";

        public static string CreateEntityClaimDocumentId(string entityId) => CreateClaimDocumentId(null, null, entityId);

        public static string CreateClaimPartition(ClaimsRequest request) => CreateClaimPartition(request.VehicleId, request.UserId, request.ServiceId);
        public static string CreateClaimPartition(string vehicleId, string userId, string entityId)
        {
            if (vehicleId == null && userId == null)
            {
                return CreateEntityPartition(entityId);
            }
            else
            {
                return CreateVehicleUserPartition(vehicleId, userId);
            }
        }

        public static string CreateVehicleUserPartition(string vehicleId, string userId) => $"ClaimP|{vehicleId}|{userId}";

        public static string CreateEntityPartition(string entityId) => $"ClaimP|{entityId}";
    }
}
