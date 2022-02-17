namespace SampleClaimsProvider
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a simplified interface for interacting with a cosmos db DocumentClient 
    /// </summary>
    public class ClaimsProviderStore
    {
        private const string PlaceHolderLabel = "ALL";
        private const string McvpPathPrefix = "//mcvp/"; // For claims used by MCVP, for exmaple, specifiying claims on an arbitrary MQTT topic

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
        ///     Retrieves the claims for a user and vehicle with optional authToken
        /// </summary>
        /// <param name="vehicleId">vehicleId to retrieve the claims for</param>
        /// <param name="userId">userId for which to retrieve the claims for</param>
        /// <param name="paths">optional list of paths to filter claims by</param>
        /// <returns>Claims on the vehicle/user. Null if the given vehicle doesn't exist.</returns>
        public async Task<VehicleUserInfo> RetrieveVehicleUserInfoAsync(string vehicleId, string userId, IList<string> paths)
        {
            IDictionary<string, List<StringClaim>> claims = await this.LookupVehicleUserAsync(
                vehicleId,
                userId,
                new Dictionary<string, IList<string>>()
                {
                    { PlaceHolderLabel, paths }
                });
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

        private async Task<IDictionary<string, List<StringClaim>>> LookupVehicleUserAsync(string vehicleId, string userId, Dictionary<string, IList<string>> labelsAndPaths)
        {
            IDictionary<string, List<StringClaim>> userVehicleClaims = null;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                userVehicleClaims = await this.LookupClaimsByVehicleAndUser(vehicleId, userId, labelsAndPaths);
                
                // If we're given a user id and we find no claims, there is no association between the given userId and vehicleId
                if (userVehicleClaims is null)
                {
                    return null; 
                }
            }
            
            IDictionary<string, List<StringClaim>> vehicleClaims = await this.LookupClaimsByVehicleAndUser(vehicleId, null, labelsAndPaths);
            if (vehicleClaims is null && !userVehicleClaims.Any())
            { 
                // if we found no claims documents for the vehicle and we don't have any user+vehicle claims, the vehicle is not in the database
                return null; 
            }

            return vehicleClaims.MergeClaims(userVehicleClaims);
        }

        private async Task<IDictionary<string, List<StringClaim>>> LookupClaimsByVehicleAndUser(string vehicleId, string userId, IDictionary<string, IList<string>> labelsAndPaths)
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
                    return this.FilterClaimsByPaths(claimsList, labelsAndPaths);
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

        private IDictionary<string, List<StringClaim>> FilterClaimsByPaths(List<StringClaim> claims, IDictionary<string, IList<string>> labelsAndPaths)
        {
            IDictionary<string, List<StringClaim>> filteredLabelClaims = new Dictionary<string, List<StringClaim>>();
            foreach (KeyValuePair<string, IList<string>> labelsAndPath in labelsAndPaths)
            {
                HashSet<StringClaim> filteredClaims = new HashSet<StringClaim>();

                IList<string> paths = labelsAndPath.Value;

                if (paths == null || paths.Count == 0)
                {
                    paths = new List<string>() { $"{McvpPathPrefix}*" };
                }

                foreach (string path in paths)
                {
                    filteredClaims.UnionWith(this.FilterClaimsByPath(path, claims));
                }

                filteredLabelClaims[labelsAndPath.Key] = filteredClaims.ToList();
            }

            return filteredLabelClaims;
        }

        private HashSet<StringClaim> FilterClaimsByPath(string path, List<StringClaim> claims)
        { 
            bool matchAbsolute = !path.EndsWith("*");
            string pathPrefix = path.Split('*').First();
            if (pathPrefix.StartsWith(McvpPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                pathPrefix = pathPrefix.Substring(McvpPathPrefix.Length);
            }

            return claims.Where(claim =>
                (matchAbsolute && claim.Name.Equals(pathPrefix, StringComparison.OrdinalIgnoreCase)) ||
                (!matchAbsolute && claim.Name.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
            ).ToHashSet();
        }
    }
}
