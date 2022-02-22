// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System.IO;
    
    /// <summary>
    /// The AddClaimsFunction defined here is not required by your CVP instance. CVP will never attempt to call this route.
    /// For this sample, it simply provides a way to add data into the backing Cosmos database. 
    /// You may choose to add data to your backing database in any way you see fit.
    /// </summary>
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

            using StreamReader reader = new StreamReader(req.Body);
            string requestBody = await reader.ReadToEndAsync();
            ClaimsRequest claimsRequest = JsonConvert.DeserializeObject<ClaimsRequest>(requestBody);

            ClaimsDocument claim = claimsRequest.CreateClaimsDocument();

            await claimsInfoItems.AddAsync(claim);

            const string responseMessage = "Added Claims info object.";
            log.LogInformation("[{tag}] Created claims for: {claimsRequest}", "", claimsRequest);
            return new OkObjectResult(responseMessage);
        }
    }
}
