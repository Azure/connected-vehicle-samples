// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public static class Extensions
    {
        public static ClaimsDocument CreateClaimsDocument(this ClaimsRequest request)
            => new ClaimsDocument
            {
                Id = DocumentIdCreator.CreateClaimDocumentId(request.VehicleId, request.UserId, request.ServiceId),
                PartitionKey = DocumentIdCreator.CreateClaimPartition(request.VehicleId, request.UserId, request.ServiceId),
                VehicleId = request.VehicleId,
                UserId = request.UserId,
                EntityId = request.ServiceId,
                Claims = request.Claims,
            };

        public static IDictionary<string, List<StringClaim>> MergeClaims(this IDictionary<string, List<StringClaim>> existingClaims, IDictionary<string, List<StringClaim>> newClaims)
        {
            if (newClaims == null)
            {
                return existingClaims; 
            }

            foreach (KeyValuePair<string, List<StringClaim>> labeledClaim in newClaims)
            {
                if (!existingClaims.ContainsKey(labeledClaim.Key))
                {
                    existingClaims[labeledClaim.Key] = new List<StringClaim>();
                }
                existingClaims[labeledClaim.Key].AddRange(labeledClaim.Value);
            }

            return existingClaims;
        }

        public static Dictionary<string, Collection<string>> ToClaimsCollection(this List<StringClaim> claims)
        { 
            Dictionary<string, Collection<string>> claimsCollection = new Dictionary<string, Collection<string>>();

            foreach (StringClaim claim in claims)
            {
                if (!claimsCollection.ContainsKey(claim.Name))
                {
                    claimsCollection.Add(claim.Name, new Collection<string>());
                }
                foreach (ClaimValue claimValue in claim.Values)
                {
                    claimsCollection[claim.Name].Add(claimValue.Value);
                }
            }

            return claimsCollection;
        }
    }
}
