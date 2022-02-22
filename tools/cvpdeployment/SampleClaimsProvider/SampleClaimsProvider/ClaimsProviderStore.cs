namespace SampleClaimsProvider
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Provides a simplified interface for interacting with a cosmos db DocumentClient as a claims provider store.
    /// No behavior defined here is required, you may implement it all as you see fit given that the responses provided to CVP are structured appropriately.
    /// This is merely a sample implementation of a basic claims provider store.
    /// </summary>
    public class ClaimsProviderStore
    {
        private readonly DocumentClient documentClient;
        private readonly string databaseName;
        private readonly string collectionName;
        private readonly ILogger log;

        public ClaimsProviderStore(ILogger log, DocumentClient documentClient, string databaseName, string collectionName)
        {
            this.log = log;
            this.documentClient = documentClient;
            this.databaseName = databaseName;
            this.collectionName = collectionName;
        }

        public async Task RemoveClaimsAsync(ClaimsRequest request)
        {
            string documentId = DocumentIdCreator.CreateClaimDocumentId(request);
            string documentPartition = DocumentIdCreator.CreateClaimPartition(request);
            Uri documentUri = UriFactory.CreateDocumentUri(this.databaseName, this.collectionName, documentId);

            await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(documentPartition) });
        }

        /// <summary>
        ///     Retrieves the claims for a user and vehicle
        /// </summary>
        /// <param name="vehicleId">vehicleId to retrieve the claims for</param>
        /// <param name="userId">userId for which to retrieve the claims for</param>
        /// <param name="paths">optional list of paths to filter claims by</param>
        /// <returns>Claims on the vehicle/user. Null if the given vehicle doesn't exist.</returns>
        public async Task<VehicleUserInfo> RetrieveVehicleUserInfoAsync(string vehicleId, string userId, IList<string> paths)
        {
            List<StringClaim> claims = await this.LookupVehicleUserAsync(vehicleId, userId, paths);
            if (claims is null)
            {
                return null;
            }
            return new VehicleUserInfo {
                VehicleId = vehicleId,
                UserId = userId,
                ExpiryTime = DateTimeOffset.UtcNow.AddMonths(1),
                Claims = claims.ToClaimsCollection()
            };
        }

        private async Task<List<StringClaim>> LookupVehicleUserAsync(string vehicleId, string userId, IList<string> paths)
        {
            List<StringClaim> userVehicleClaims = new List<StringClaim>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                userVehicleClaims = await this.LookupClaimsByVehicleAndUser(vehicleId, userId, paths);
                
                // If we're given a user id and we find no claims, there is no association between the given userId and vehicleId
                if (userVehicleClaims is null)
                {
                    return null; 
                }
            }
            
            List<StringClaim> vehicleClaims = await this.LookupClaimsByVehicleAndUser(vehicleId, null, paths);
            if (vehicleClaims is null)
            { 
                // if we found no claims documents for the vehicle and we don't have any user+vehicle claims, the vehicle is not in the database
                return null; 
            }

            vehicleClaims.AddRange(userVehicleClaims);
            return vehicleClaims;
        }

        private async Task<List<StringClaim>> LookupClaimsByVehicleAndUser(string vehicleId, string userId, IList<string> paths)
        {
            try
            {
                string queryPartitionKey = DocumentIdCreator.CreateVehicleUserPartition(vehicleId, userId);
                string queryStatement = $"SELECT * FROM root r WHERE r.partitionKey = \"{queryPartitionKey}\"";

                List<StringClaim> claimsList = null;
                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName);
                IDocumentQuery<ClaimsDocument> query = this.documentClient.CreateDocumentQuery<ClaimsDocument>(collectionUri)
                    .Where(p => p.PartitionKey.Equals(queryPartitionKey))
                    .AsDocumentQuery();

                while (query.HasMoreResults)
                {
                    foreach (ClaimsDocument claimDocument in await query.ExecuteNextAsync())
                    {
                        // If we found any claims documents then bare minimum we have an empty claims list
                        claimsList ??= new List<StringClaim>();

                        // If there are additional claims, add them into the list
                        if (claimDocument.Claims?.Count > 0)
                        {
                            claimsList.AddRange(claimDocument.Claims);
                        }
                    }
                }

                if (claimsList != null)
                {
                    return this.FilterClaimsByPaths(claimsList, paths);
                }
                return null;
            }
            catch (Exception e)
            {
                this.log.LogError(e, "[{tag}] Error retrieving claims for user '{userId}' vehicle '{vehicleId}'",
                    "a8a9a900-5ce7-406e-a965-472cf09e1ba1", userId, vehicleId);
                return null;
            }
        }

        private List<StringClaim> FilterClaimsByPaths(List<StringClaim> claims, IList<string> paths)
        {
            List<StringClaim> filteredClaims = new List<StringClaim>();

            foreach (string path in paths)
            {
                filteredClaims.AddRange(this.FilterClaimsByPath(path, claims));
            }

            return filteredClaims.ToList();
        }

        private IEnumerable<StringClaim> FilterClaimsByPath(string path, List<StringClaim> claims)
        { 
            bool matchAbsolute = !path.EndsWith("*");
            string pathPrefix = path.Split('*').First();

            return claims.Where(claim =>
                (matchAbsolute && claim.Name.Equals(pathPrefix, StringComparison.OrdinalIgnoreCase)) ||
                (!matchAbsolute && claim.Name.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
            );
        }
    }
}
