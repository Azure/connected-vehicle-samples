// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    
    /// <summary>
    /// The 3 RetreiveClaims functions defined here are all required claims provider routes. Your CVP instance will reach out to these endpoints to retrieve necessary claims information.
    /// The routes must match what is shown below for CVP to be able to interact with your claims provider. 
    /// In addition the response structure must also match what is show below. How you go about retreiving claims from your database is entirely up to you.
    /// </summary>
    public class RetrieveClaimsFunction
    {
        /// <summary> This function retrieves claims for a specified vehicleId.
        /// <example>
        /// [GET] https://{hostname}/api/claimInfo/vehicles/testVehicle1?paths=path1
        /// </example>
        /// </summary>
        [FunctionName("RetrieveClaimsWithVehicleIdFunction")]
        public Task<IActionResult> RetrieveClaimsWithVehicleId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "claims/claimInfo/vehicles/{vehicleId}")] HttpRequest req, string vehicleId,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
        => this.RetrieveClaimsAsync(req, vehicleId, userId: null, client, log);

        /// <summary> This function retrieves claims for a specified vehicleId and userId.
        /// <example>
        /// [GET] https://{hostname}/api/claimInfo/vehicles/testVehicle1/users/testUser1?paths=path1
        /// </example>
        /// </summary>
        [FunctionName("RetrieveClaimsWithVehicleAndUserIdFunction")]
        public Task<IActionResult> RetrieveClaimsWithVehicleAndUserId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "claims/claimInfo/vehicles/{vehicleId}/users/{userId}")] HttpRequest req, string vehicleId, string userId,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
        => this.RetrieveClaimsAsync(req, vehicleId, userId, client, log);

        [FunctionName("RetrieveClaimsByLabel")]
        public async Task<IActionResult> RetrieveClaimsByLabel(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "claims/claimInfo/vehicles/{vehicleId}/labels")] HttpRequest req, string vehicleId,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
        {
            Dictionary<string, IList<string>> labelsAndPaths = JsonSerializer.Deserialize<Dictionary<string, IList<string>>>(req.Body);
            return await this.RetrieveLabeledClaimsAsync(req, vehicleId, client, labelsAndPaths, log);
        }

        private async Task<IActionResult> RetrieveLabeledClaimsAsync(
            HttpRequest req,
            string vehicleId,
            DocumentClient client,
            Dictionary<string, IList<string>> labelsAndPaths,
            ILogger log)
        {
            log.LogInformation("[{tag}] Processing RetrieveLabeledClaimsAsync for vehicle with vehicleId '{vehicleId}'", "82b54258-e700-42d1-92de-b45aec151e98", vehicleId);

            ClaimsProviderStore claimsProviderStore = new ClaimsProviderStore(log, client, Constants.DatabaseName, Constants.CollectionName);

            VehicleDeviceClaims claims = new VehicleDeviceClaims
            {
                VehicleId = vehicleId,
                UserId = null,
                ExpiryTime = DateTime.UtcNow.AddDays(7),
                LabeledClaims = new Dictionary<string, Dictionary<string, Collection<string>>>()
            };

            foreach ((string label, IList<string> paths) in labelsAndPaths)
            {
                VehicleUserInfo info = await claimsProviderStore.RetrieveVehicleUserInfoAsync(vehicleId, null, paths);
                claims.LabeledClaims[label] = info.Claims;
            }

            return new JsonResult(claims);
        }

        private async Task<IActionResult> RetrieveClaimsAsync(
            HttpRequest req,
            string vehicleId,
            string userId,
            DocumentClient client,
            ILogger log)
        {
            log.LogInformation("[{tag}] Processing RetrieveClaimsAsync for vehicle with vehicleId '{vehicleId}' and userId '{userId}'", "65c3592e-c365-4a7a-856b-275244e65cd7", vehicleId, userId);

            string paths = req.Query["paths"];
            List<string> listPaths = paths?.Split(',').ToList();
            ClaimsProviderStore claimsProviderStore = new ClaimsProviderStore(log, client, Constants.DatabaseName, Constants.CollectionName);
            VehicleUserInfo vehicleUserInfo = await claimsProviderStore.RetrieveVehicleUserInfoAsync(vehicleId, userId, listPaths);
            log.LogInformation("[{tag}] Vehicle User Info for vehicle '{vehicleId}' and user '{userId}':  {vehicleUserInfo}", "75a1a94f-ef38-4113-97e1-98b7dab30005", vehicleId, userId, JsonSerializer.Serialize(vehicleUserInfo));

            return new JsonResult(vehicleUserInfo);
        }
    }
}
