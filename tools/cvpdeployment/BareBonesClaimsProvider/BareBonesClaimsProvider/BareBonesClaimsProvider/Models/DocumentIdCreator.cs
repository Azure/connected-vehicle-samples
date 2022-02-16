// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace BareBonesClaimsProvider
{
    /// <summary>
    /// Creates the Cosmos DB document Id values.
    /// </summary>
    public static class DocumentIdCreator
    {
        public static string CreateClaimDocumentId(string vehicleId, string userId, string entityId)
        {
            return $"Claim|{vehicleId}|{userId}|{entityId}";
        }

        public static string CreateEntityClaimDocumentId(string entityId)
        {
            return CreateClaimDocumentId(null, null, entityId);
        }

        public static string CreateGroupDocumentId(string userId, string groupId)
        {
            return $"Group|{userId}|{groupId}";
        }

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

        public static string CreateVehicleUserPartition(string vehicleId, string userId)
        {
            return $"ClaimP|{vehicleId}|{userId}";
        }

        public static string CreateEntityPartition(string entityId)
        {
            return $"ClaimP|{entityId}";
        }

        public static string CreateGroupPartition(string userId)
        {
            return $"GroupP|{userId}";
        }
    }
}
