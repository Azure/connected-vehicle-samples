// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    [Route("api/claimsstore")]
    [ApiController]
    public class ClaimsStoreController : ControllerBase
    {
        private readonly IClaimsProviderStore claimsProviderStore;
        private readonly ILogger<ClaimsStoreController> logger;

        public ClaimsStoreController(ILogger<ClaimsStoreController> logger, IConfiguration configuration, IClaimsProviderStore claimsProviderStore)
        {
            this.logger = logger;
            this.claimsProviderStore = claimsProviderStore;
        }

        /// <summary> This API adds a specified claim.
        /// <example>
        /// [POST] https://{hostname}/api/claimsstore
        ///     <code>
        ///     Json Body:
        ///     {"vehicleId": "testVehicle1", "serviceId": "service1", "claims": [{"name": "claim1","values": [{"value": "value1"}]}]}
        ///     </code>
        /// </example>
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddClaims([FromBody] ClaimsStoreRequest claimsRequest)
        {
            await this.claimsProviderStore.CreateClaimsAsync(claimsRequest.VehicleId, claimsRequest.UserId, claimsRequest.ServiceId, claimsRequest.Claims);

            const string responseMessage = "Added Claims info object.";
            this.logger.LogInformation("[{tag}] Created claims for: {claimsRequest}", "", claimsRequest);
            return new OkObjectResult(responseMessage);
        }

        /// <summary> This API deletes a specified claim.
        /// <example>
        /// [DELETE] https://{hostname}/api/claimsstore
        ///     <code>
        ///     Json Body:
        ///     {"vehicleId": "testVehicle1", "serviceId": "service1"}
        ///     </code>
        /// </example>
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> RemoveClaims([FromBody] ClaimsStoreRequest claimsRequest)
        {
            this.logger.LogInformation("[{tag}] Processing RemoveClaimsFunction", "");
            await this.claimsProviderStore.RemoveClaimsAsync(claimsRequest.VehicleId, claimsRequest.UserId, claimsRequest.ServiceId);

            const string responseMessage = "Deleted claim";
            this.logger.LogInformation("[{tag}] Deleted claim for: {claimsRequest}", "", claimsRequest);
            return new OkObjectResult(responseMessage);
        }
    }
}
