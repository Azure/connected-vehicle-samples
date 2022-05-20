// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Extensions.Logging;

    public class ClaimsProviderStore : IClaimsProviderStore
    {
        private const string AuthTokenName = "authToken";
        private const string AuthTokenLabel = "AuthTokenLabel";
        private const string PlaceHolderLabel = "ALL";
        private const string McvpPathPrefix = "//mcvp/";

        private readonly DocumentClient documentClient;
        private readonly Database database;
        private readonly DocumentCollection collection;
        private readonly ILogger<ClaimsProviderStore> log;

        public ClaimsProviderStore(ILogger<ClaimsProviderStore> log, DocumentClient documentClient)
        {
            this.log = log;
            this.documentClient = documentClient;
            this.database = this.CreateOrGetDatabase(Constants.DatabaseName).Result;
            this.collection = this.CreateOrGetCollection(Constants.CollectionName).Result;
        }

        public async Task CreateClaimsAsync(string vehicleId, string userId, string serviceId, IList<StringClaim> claims)
        {
            vehicleId = this.VerifyAddClaimsInput(nameof(vehicleId), vehicleId);
            userId = this.VerifyAddClaimsInput(nameof(userId), userId);
            serviceId = this.VerifyAddClaimsInput(nameof(serviceId), serviceId);

            ClaimsDocument claimsDocument = new ClaimsDocument
            {
                VehicleId = vehicleId,
                UserId = userId,
                EntityId = serviceId,
                Claims = claims.ToList()
            };

            claimsDocument.Id = DocumentIdCreator.CreateClaimDocumentId(claimsDocument.VehicleId, claimsDocument.UserId, claimsDocument.EntityId);
            claimsDocument.PartitionKey = DocumentIdCreator.CreateClaimPartition(claimsDocument.VehicleId, claimsDocument.UserId, claimsDocument.EntityId);
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(this.database.Id, this.collection.Id);

            await this.documentClient.UpsertDocumentAsync(collectionUri, claimsDocument, new RequestOptions { PartitionKey = new PartitionKey(claimsDocument.PartitionKey) });
        }

        public async Task RemoveClaimsAsync(string vehicleId, string userId, string serviceId)
        {
            vehicleId = this.VerifyAddClaimsInput(nameof(vehicleId), vehicleId);
            userId = this.VerifyAddClaimsInput(nameof(userId), userId);
            serviceId = this.VerifyAddClaimsInput(nameof(serviceId), serviceId);

            string documentId = DocumentIdCreator.CreateClaimDocumentId(vehicleId, userId, serviceId);
            string documentPartition = DocumentIdCreator.CreateClaimPartition(vehicleId, userId, serviceId);
            Uri documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.collection.Id, documentId);

            await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(documentPartition) });
        }

        /// <summary>
        ///     Retrieves the claims for a user and vehicle with optional authToken
        /// </summary>
        /// <param name="vehicleId">vehicleId to retrieve the claims for</param>
        /// <param name="userId">userId for which to retrieve the claims for</param>
        /// <param name="authToken">optional authToken provided in request</param>
        /// <param name="paths">optional list of paths to filter claims by</param>
        /// <returns>Claims on the vehicle/user. Null if the given vehicle doesn't exist.</returns>
        public async Task<VehicleUserInfo> RetrieveVehicleUserInfoAsync(string vehicleId, string userId, string authToken, IList<string> paths)
        {
            IDictionary<string, List<StringClaim>> claims = await this.LookupVehicleUserAsync(
                vehicleId,
                userId,
                authToken,
                new Dictionary<string, IList<string>>()
                {
                    { PlaceHolderLabel, paths }
                },
                false);
            if (claims != null)
            {
                if (!string.IsNullOrEmpty(authToken))
                {
                    if (claims.ContainsKey(AuthTokenLabel))
                    {
                        claims[AuthTokenLabel].Add(new StringClaim(AuthTokenName, authToken));
                    }
                    else
                    {
                        claims.Add(AuthTokenLabel, new List<StringClaim> { new StringClaim(AuthTokenName, authToken) });
                    }
                }

                return this.BuildVehicleUserInfo(vehicleId, userId, claims);
            }
            
            return null;
        }

        /// <summary>
        ///     refresh claims for a user and vehicle with existing claimsList
        /// </summary>
        /// <param name="vehicleId">vehicleId to retrieve the claims for</param>
        /// <param name="userId">userId for which to retrieve the claims for</param>
        /// <param name="claimsList">existing claims from existing principal context (if applicable)</param>
        /// <param name="paths">optional list of paths to filter claims by</param>
        /// <returns>Claims on the vehicle/user.  Empty on failure</returns>
        public async Task<VehicleUserInfo> RefreshClaimsInfoAsync(string vehicleId, string userId, List<KeyValuePair<string, string>> claimsList, IList<string> paths)
        {
            // The claimsList may provide additional claims needed to be carried forward into the refreshed token

            // check if an authToken was supplied in initial token request
            // this claims provider saved off a copy of the authToken under the key "authToken"
            string authToken = this.CheckForAuthToken(claimsList);

            // VehicleUserInfo vehicleUserInfo =
            IDictionary<string, List<StringClaim>> claims = await this.LookupVehicleUserAsync(
                vehicleId,
                userId,
                authToken,
                new Dictionary<string, IList<string>>()
                {
                    { PlaceHolderLabel, paths }
                },
                false);
            if (claims != null)
            {
                if (authToken != null)
                {
                    if (claims.ContainsKey(AuthTokenLabel))
                    {
                        claims[AuthTokenLabel].Add(new StringClaim(AuthTokenName, authToken));
                    }
                    else
                    {
                        claims.Add(AuthTokenLabel, new List<StringClaim> { new StringClaim(AuthTokenName, authToken) });
                    }
                }

                return this.BuildVehicleUserInfo(vehicleId, userId, claims);
            }
            else
            {
                return null;
            }
        }

        private async Task<Database> CreateOrGetDatabase(string databaseId)
        {
            Database[] databases = this.documentClient.CreateDatabaseQuery().Where(db => db.Id == databaseId).ToArray();

            if (databases.Any())
            {
                return databases.First();
            }

            return await this.documentClient.CreateDatabaseAsync(new Database { Id = databaseId });
        }

        private async Task<DocumentCollection> CreateOrGetCollection(string collectionID)
        {
            DocumentCollection[] collections = this.documentClient.CreateDocumentCollectionQuery(this.database.CollectionsLink)
                                .Where(col => col.Id == collectionID).ToArray();
            if (collections.Any())
            {
                return collections.First();
            }

            DocumentCollection collection = new DocumentCollection();
            collection.Id = collectionID;
            collection.PartitionKey.Paths.Add("/partitionKey");
            return await this.documentClient.CreateDocumentCollectionAsync(this.database.CollectionsLink, collection);
        }

        private async Task<IDictionary<string, IList<StringClaim>>> LookupClaimsByVehicleAndUser(string vehicleId, string userId, IDictionary<string, IList<string>> labelsAndPaths)
        {
            try
            {
                string queryPartitionKey = DocumentIdCreator.CreateVehicleUserPartition(vehicleId, userId);
                string queryStatement = $"SELECT * FROM root r WHERE r.partitionKey = \"{queryPartitionKey}\"";

                List<StringClaim> claimsList = null;
                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(this.database.Id, this.collection.Id);
                IDocumentQuery<ClaimsDocument> query = this.documentClient.CreateDocumentQuery<ClaimsDocument>(collectionUri)
                    .Where(p => p.PartitionKey.Equals(queryPartitionKey))
                    .AsDocumentQuery();

                while (query.HasMoreResults)
                {
                    foreach (ClaimsDocument claimDocument in await query.ExecuteNextAsync())
                    {
                        // If we found any claims documents then bare minimum we have an empty claims list
                        claimsList ??= new List<StringClaim>();

                        // If there is a entityId associated with this vehicle/user pair - add that entity name
                        if (claimDocument.EntityId != null)
                        {
                            List<StringClaim> entityClaimList = await this.LookupClaimsByEntityId(claimDocument.EntityId);
                            if (entityClaimList?.Count > 0)
                            {
                                claimsList.AddRange(entityClaimList);
                            }
                        }

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
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                this.log.LogError(e, "[{tag}] Error retrieving claims for user '{userId}' vehicle '{vehicleId}'",
                    "a8a9a900-5ce7-406e-a965-472cf09e1ba1", userId, vehicleId);
                return null;
            }
        }

        private IDictionary<string, IList<StringClaim>> FilterClaimsByPaths(List<StringClaim> claims, IDictionary<string, IList<string>> labelsAndPaths)
        {
            Requires.NotNullOrEmpty(labelsAndPaths, nameof(labelsAndPaths));
            Requires.NotNull(claims, nameof(claims));
            IDictionary<string, IList<StringClaim>> filteredLabelClaims = new Dictionary<string, IList<StringClaim>>();
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
                    string pathPrefix = path.Split('*').First();

                    bool matchAbsolute = !path.EndsWith("*", StringComparison.OrdinalIgnoreCase);

                    foreach (StringClaim claim in claims)
                    {
                        if (claim.Name.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        {
                            if (matchAbsolute && claim.Name.Equals(pathPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                filteredClaims.Add(claim);
                            }
                            else if (!matchAbsolute && claim.Name.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                filteredClaims.Add(claim);
                            }
                        }
                        else
                        {
                            if (path.StartsWith(McvpPathPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                string trimmedPathPrefix = pathPrefix.Substring(McvpPathPrefix.Length);

                                if (matchAbsolute && claim.Name.Equals(trimmedPathPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    filteredClaims.Add(claim);
                                }
                                else if (!matchAbsolute && claim.Name.StartsWith(trimmedPathPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    filteredClaims.Add(claim);
                                }
                            }
                        }
                    }
                }

                filteredLabelClaims[labelsAndPath.Key] = filteredClaims.ToList();
            }

            return filteredLabelClaims;
        }

        private async Task<List<StringClaim>> LookupClaimsByEntityId(string entityId)
        {
            string documentId = DocumentIdCreator.CreateEntityClaimDocumentId(entityId);
            string partitionKey = DocumentIdCreator.CreateEntityPartition(entityId);

            Uri documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.collection.Id, documentId);
            DocumentResponse<ClaimsDocument> entityRecordResponse = await this.documentClient.ReadDocumentAsync<ClaimsDocument>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
            ClaimsDocument entityRecord = entityRecordResponse.Document;
            if (entityRecord != null)
            {
                return entityRecord.Claims;
            }
            else
            {
                return null;
            }
        }

        private IDictionary<string, IList<StringClaim>> LookupClaimsByAuthToken(string authToken, IDictionary<string, IList<string>> labelsAndPaths)
        {
            // In a future sample update, will utilize AAD B2C and would need to validate the authToken (properly signed by AAD and not expired)
            // A third party IdP could also be utilized for authToken processing
            IDictionary<string, IList<StringClaim>> claims = new Dictionary<string, IList<StringClaim>>();

            return claims;
        }

        private VehicleUserInfo BuildVehicleUserInfo(string vehicleId, string userId, IDictionary<string, List<StringClaim>> labeledClaims)
        {
            VehicleUserInfo vehicleUser = new VehicleUserInfo();
            vehicleUser.UserId = userId;
            vehicleUser.VehicleId = vehicleId;
            vehicleUser.ExpiryTime = DateTimeOffset.UtcNow.AddMonths(1);
            vehicleUser.Claims = this.ConvertToCollection(labeledClaims);
            return vehicleUser;
        }

        private async Task<IDictionary<string, List<StringClaim>>> LookupVehicleUserAsync(string vehicleId, string userId, string authToken, Dictionary<string, IList<string>> labelsAndPaths, bool includeLabels)
        {
            Requires.Argument(NamingRestrictions.IsValidNamingString(vehicleId), nameof(vehicleId), NamingRestrictions.InvalidNamingStringErrorMessage);

            if (userId != null)
            {
                Requires.Argument(NamingRestrictions.IsValidNamingString(userId), nameof(userId), NamingRestrictions.InvalidNamingStringErrorMessage);
            }

            IDictionary<string, List<StringClaim>> labeledClaims = new Dictionary<string, List<StringClaim>>();
            bool isVehicleUserClaimsPresent = false;

            // If an authToken is provided in the request, verify and determine how the authToken will be used (for example JIT elevation) and what claims to provide.
            if (authToken?.Length > 0)
            {
                IDictionary<string, IList<StringClaim>> labeledAuthClaims = this.LookupClaimsByAuthToken(authToken, labelsAndPaths);
                this.AppendToExistingLabeledClaims(labeledClaims, labeledAuthClaims);
            }

            if (userId?.Length > 0)
            {
                // Aggregate the claims from Claims database for the VehicleId/UserId, VehicleId, UserId
                IDictionary<string, IList<StringClaim>> userVehicleClaims = await this.LookupClaimsByVehicleAndUser(vehicleId, userId, labelsAndPaths);
                if (userVehicleClaims?.Count > 0)
                {
                    isVehicleUserClaimsPresent = true;

                    this.AppendToExistingLabeledClaims(labeledClaims, userVehicleClaims);
                }

                // If there are no claims at this point - there is no association of userId with the vehicleId
                if (labeledClaims.Count == 0)
                {
                    return null;
                }

                // Claims associated with the user
                IDictionary<string, IList<StringClaim>> userClaims = await this.LookupClaimsByVehicleAndUser(null, userId, labelsAndPaths);
                this.AppendToExistingLabeledClaims(labeledClaims, userClaims);
            }

            // Claims associated with the vehicle
            IDictionary<string, IList<StringClaim>> vehicleClaims = await this.LookupClaimsByVehicleAndUser(vehicleId, null, labelsAndPaths);
            if (vehicleClaims?.Count > 0)
            {
                this.AppendToExistingLabeledClaims(labeledClaims, vehicleClaims);
            }
            else
            {
                // if there were no vehicle specific claims (either from Vehicle/User claims or Vehicle only claims), this means the Vehicle is not in the database so fail the request
                if (vehicleClaims == null && !isVehicleUserClaimsPresent)
                {
                    return null;
                }
            }

            return labeledClaims;
        }

        private void AppendToExistingLabeledClaims(IDictionary<string, List<StringClaim>> existingClaimsDictionary, IDictionary<string, IList<StringClaim>> claimsToAppend)
        {
            Requires.NotNull(existingClaimsDictionary, nameof(existingClaimsDictionary));
            if (claimsToAppend == null)
            {
                return;
            }

            foreach (KeyValuePair<string, IList<StringClaim>> labeledAuthClaim in claimsToAppend)
            {
                if (!existingClaimsDictionary.ContainsKey(labeledAuthClaim.Key))
                {
                    existingClaimsDictionary[labeledAuthClaim.Key] = new List<StringClaim>();
                }
                existingClaimsDictionary[labeledAuthClaim.Key].AddRange(labeledAuthClaim.Value);
            }
        }

        private string CheckForAuthToken(List<KeyValuePair<string, string>> claimsList)
        {
            foreach (KeyValuePair<string, string> kvp in claimsList)
            {
                if (kvp.Key.Equals(AuthTokenName))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private Dictionary<string, Collection<string>> ConvertToCollection(IDictionary<string, List<StringClaim>> labeledClaims)
        {
            Dictionary<string, Collection<string>> dictionaryClaims = new Dictionary<string, Collection<string>>();
            List<StringClaim> allClaims = new List<StringClaim>();

            if (labeledClaims != null)
            {
                allClaims.AddRange(labeledClaims.Values.SelectMany(x => x));
            }
            foreach (StringClaim claim in allClaims)
            {
                if (!dictionaryClaims.ContainsKey(claim.Name))
                {
                    Collection<string> value = new Collection<string>();
                    dictionaryClaims.Add(claim.Name, value);
                }
                foreach (ClaimValue cv in claim.Values)
                {
                    dictionaryClaims[claim.Name].Add(cv.Value);
                }
            }
            return dictionaryClaims;
        }

        private string VerifyAddClaimsInput(string paramName, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (!NamingRestrictions.IsValidNamingString(input))
            {
                throw new ArgumentException($"{paramName} [{input}] contains illegal characters");
            }

            return input;
        }
    }
}