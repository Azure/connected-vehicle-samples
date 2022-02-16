﻿// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------


namespace BareBonesClaimsProvider
{
    using System.IO;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Microsoft.Azure.Documents.Client;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System;
    using Microsoft.Extensions.Primitives;

    class RetrieveClaimsFunction
    {
        /// <summary> This function retrieves claims for a specified vehicleId.
        /// <example>
        /// [GET] https://{hostname}/api/claimInfo/vehicles/testVehicle1?paths=path1
        /// </example>
        /// </summary>
        [FunctionName("RetrieveClaimsWithVehicleIdFunction")]
        public async Task<IActionResult> RetrieveClaimsWithVehicleId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "claims/claimInfo/vehicles/{vehicleId}")] HttpRequest req, string vehicleId,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
         => await this.RetrieveClaimsAsync(req, vehicleId, userId: null, client, log);
        

        /// <summary> This function retrieves claims for a specified vehicleId and userId.
        /// <example>
        /// [GET] https://{hostname}/api/claimInfo/vehicles/testVehicle1/users/testUser1?paths=path1
        /// </example>
        /// </summary>
        [FunctionName("RetrieveClaimsWithVehicleAndUserIdFunction")]
        public async Task<IActionResult> RetrieveClaimsWithVehicleAndUserId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "claims/claimInfo/vehicles/{vehicleId}/users/{userId}")] HttpRequest req, string vehicleId, string userId,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
        => await this.RetrieveClaimsAsync(req, vehicleId, userId, client, log);

        [FunctionName("RetrieveClaimsByLabel")]
        public async Task<IActionResult> RetrieveClaimsByLabel(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "claims/claimInfo/vehicles/{vehicleId}/labels")] HttpRequest req, string vehicleId,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
        {
            using StreamReader reader = new StreamReader(req.Body);
            string bodyContent = await reader.ReadToEndAsync();

            Dictionary<string, IList<string>> labelsAndPaths = JsonConvert.DeserializeObject<Dictionary<string, IList<string>>>(bodyContent);
            return await this.RetrieveLabeledClaimsAsync(req, vehicleId, client, labelsAndPaths, log);
        }

        private static string GetAuthTokenIfExists(HttpRequest req)
        {
            return req.Headers.TryGetValue("AuthToken", out StringValues headerValue) ? headerValue.ToString() : string.Empty;
        }

        private async Task<IActionResult> RetrieveLabeledClaimsAsync(
            HttpRequest req,
            string vehicleId,
            DocumentClient client,
            Dictionary<string, IList<string>> labelsAndPaths,
            ILogger log)
        {
            log.LogInformation("[{tag}] Processing RetrieveLabeledClaimsAsync for vehicle with vehicleId '{vehicleId}'", "82b54258-e700-42d1-92de-b45aec151e98", vehicleId);
            
            string authToken = GetAuthTokenIfExists(req);
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
                VehicleUserInfo info = await claimsProviderStore.RetrieveVehicleUserInfoAsync(vehicleId, null, authToken, paths);
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

            string authToken = GetAuthTokenIfExists(req);
            string paths = req.Query["paths"];
            List<string> listPaths = paths?.Split(',').ToList();
            ClaimsProviderStore claimsProviderStore = new ClaimsProviderStore(log, client, Constants.DatabaseName, Constants.CollectionName);
            VehicleUserInfo vehicleUserInfo = await claimsProviderStore.RetrieveVehicleUserInfoAsync(vehicleId, userId, authToken, listPaths);
            log.LogInformation("[{tag}] Vehicle User Info for vehicle '{vehicleId}' and user '{userId}':  {vehicleUserInfo}", "75a1a94f-ef38-4113-97e1-98b7dab30005", vehicleId, userId, JsonConvert.SerializeObject(vehicleUserInfo));

            return new JsonResult(vehicleUserInfo);
        }
    }
}
