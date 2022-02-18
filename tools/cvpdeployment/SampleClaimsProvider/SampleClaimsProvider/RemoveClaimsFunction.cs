﻿// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class RemoveClaimsFunction
    {
        /// <summary> This function deletes a specified claim.
        /// <example>
        /// [DELETE] https://{hostname}/api/claimsstore
        ///     <code>
        ///     Json Body:
        ///     {
        ///         "vehicleId": "testVehicle1",
        ///         "serviceId": "service1"
        ///     }
        ///     </code>
        /// </example>
        /// </summary>
        [FunctionName("RemoveClaimsFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "claimsstore")] HttpRequest req,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("[{tag}] Processing RemoveClaimsFunction", "");

            using StreamReader reader = new StreamReader(req.Body);
            string requestBody = await reader.ReadToEndAsync();
            ClaimsRequest claimsRequest = JsonConvert.DeserializeObject<ClaimsRequest>(requestBody);

            ClaimsProviderStore claimsProviderStore = new ClaimsProviderStore(log, client, Constants.DatabaseName, Constants.CollectionName);
            await claimsProviderStore.RemoveClaimsAsync(claimsRequest);

            const string responseMessage = "Deleted claim";
            log.LogInformation("[{tag}] Deleted claim for: {requestBody}", "", claimsRequest);
            return new OkObjectResult(responseMessage);
        }
    }
}
