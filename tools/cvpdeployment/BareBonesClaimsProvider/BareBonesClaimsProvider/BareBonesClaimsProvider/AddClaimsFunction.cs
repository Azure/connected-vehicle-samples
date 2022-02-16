// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace BareBonesClaimsProvider
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class AddClaimsFunction
    {
        /// <summary> This function adds a specified claim.
        /// <example>
        /// [POST] https://{hostname}/api/claimsstore
        ///     <code>
        ///     Json Body:
        ///     {
        ///         "vehicleId": "testVehicle1",
        ///         "serviceId": "service1",
        ///         "claims": [
        ///         {
        ///             "name": "claim1",
        ///             "values": [
        ///             {
        ///                 "value": "value1"
        ///             }
        ///             ]
        ///         }
        ///         ]
        ///     }
        ///     </code>
        /// </example>
        /// </summary>
        [FunctionName("AddClaimsFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "claimsstore")] HttpRequest req,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                collectionName: Constants.CollectionName,
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnectionString",
                CreateIfNotExists = true,
                PartitionKey = "/partitionKey",
                Id = "id" )]
            IAsyncCollector<ClaimsDocument> claimsInfoItems,
            ILogger log)
        {
            log.LogInformation("[{tag}] Processing Add Claims function.", "65231b53-7ec3-400c-9a5c-4595b2d2e000");

            using (StreamReader reader = new StreamReader(req.Body))
            {
                string requestBody = await reader.ReadToEndAsync();
                ClaimsRequest claimsRequest = JsonConvert.DeserializeObject<ClaimsRequest>(requestBody);

                ClaimsDocument claim = new ClaimsDocument
                {
                    UserId = claimsRequest.UserId,
                    VehicleId = claimsRequest.VehicleId,
                    EntityId = claimsRequest.ServiceId,
                    Claims = claimsRequest.Claims
                };

                claim.Id = DocumentIdCreator.CreateClaimDocumentId(claim.VehicleId, claim.UserId, claim.EntityId);
                claim.PartitionKey = DocumentIdCreator.CreateClaimPartition(claim.VehicleId, claim.UserId, claim.EntityId);

                await claimsInfoItems.AddAsync(claim);

                const string responseMessage = "Added Claims info object.";
                log.LogInformation("[{tag}] Created claims for: {claimsRequest}", "", claimsRequest);
                return new OkObjectResult(responseMessage);
            }
        }
    }
}
