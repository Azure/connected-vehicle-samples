// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Newtonsoft.Json;

    [Route("api/claims/claimInfo")]
    [ApiController]
    public class ClaimsProviderController : ControllerBase
    {
        private readonly IClaimsProviderStore claimsProviderStore;
        private readonly ILogger<ClaimsProviderController> logger;

        public ClaimsProviderController(ILogger<ClaimsProviderController> logger, IConfiguration configuration, IClaimsProviderStore claimsProviderStore)
        {
            this.logger = logger;
            this.claimsProviderStore = claimsProviderStore;
        }

        /// <summary> This API retrieves claims for a specified vehicleId.
        /// <example>
        /// [GET] https://{hostname}/api/claims/claimInfo/vehicles/testVehicle1?paths=path1
        /// </example>
        /// </summary>
        [HttpGet]
        [Route("vehicles/{vehicleId}")]
        public async Task<IActionResult> RetrieveClaimsWithVehicleId(string vehicleId)
        {
            return await this.RetrieveClaimsAsync(this.Request, vehicleId, userId: null);
        }

        /// <summary> This API retrieves claims for a specified vehicleId and userId.
        /// <example>
        /// [GET] https://{hostname}/api/claims/claimInfo/vehicles/testVehicle1/users/testUser1?paths=path1
        /// </example>
        /// </summary>
        [HttpGet]
        [Route("vehicles/{vehicleId}/users/{userId}")]
        public async Task<IActionResult> RetrieveClaimsWithVehicleAndUserId(string vehicleId, string userId)
        {
            return await this.RetrieveClaimsAsync(this.Request, vehicleId, userId);
        }

        /// <summary> This API retrieves claims for a specified vehicleId.
        /// <example>
        /// [GET] https://{hostname}/api/claims/claimInfo/vehicles/testVehicle1?paths=path1
        /// [Body] {"Path1": ["label1","label2"]} 
        /// </example>
        /// </summary>
        [HttpPost]
        [Route("vehicles/{vehicleId}/labels")]
        public async Task<IActionResult> RetrieveClaimsByLabel(string vehicleId)
        {
            using StreamReader reader = new StreamReader(this.Request.Body);
            string bodyContent = await reader.ReadToEndAsync();

            Dictionary<string, IList<string>> labelsAndPaths = JsonConvert.DeserializeObject<Dictionary<string, IList<string>>>(bodyContent);
            return await this.RetrieveLabeledClaimsAsync(this.Request, vehicleId, labelsAndPaths);
        }

        private static string GetAuthTokenIfExists(HttpRequest req)
        {
            return req.Headers.TryGetValue(Headers.AuthToken, out StringValues headerValue) ? headerValue.ToString() : string.Empty;
        }

        private async Task<IActionResult> RetrieveLabeledClaimsAsync(
            HttpRequest req,
            string vehicleId,
            Dictionary<string, IList<string>> labelsAndPaths)
        {
            this.logger.LogInformation("[{tag}] Processing RetrieveLabeledClaimsAsync for vehicle with vehicleId '{vehicleId}'", "82b54258-e700-42d1-92de-b45aec151e98", vehicleId);

            string authToken = GetAuthTokenIfExists(req);

            LabeledClaimsResult claims = new LabeledClaimsResult
            {
                VehicleId = vehicleId,
                UserId = null,
                ExpiryTime = DateTime.UtcNow.AddDays(7),
                LabeledClaims = new Dictionary<string, Dictionary<string, Collection<string>>>()
            };

            foreach ((string label, IList<string> paths) in labelsAndPaths)
            {
                VehicleUserInfo info = await this.claimsProviderStore.RetrieveVehicleUserInfoAsync(vehicleId, null, authToken, paths);
                claims.LabeledClaims[label] = info.Claims;
            }

            return new JsonResult(claims);
        }

        private async Task<IActionResult> RetrieveClaimsAsync(
            HttpRequest req,
            string vehicleId,
            string userId)
        {
            this.logger.LogInformation("[{tag}] Processing RetrieveClaimsAsync for vehicle with vehicleId '{vehicleId}' and userId '{userId}'", "65c3592e-c365-4a7a-856b-275244e65cd7", vehicleId, userId);

            string authToken = GetAuthTokenIfExists(req);
            string paths = req.Query["paths"];
            List<string> listPaths = paths?.Split(',').ToList();
            VehicleUserInfo vehicleUserInfo = await this.claimsProviderStore.RetrieveVehicleUserInfoAsync(vehicleId, userId, authToken, listPaths);
            this.logger.LogInformation("[{tag}] Vehicle User Info for vehicle '{vehicleId}' and user '{userId}':  {vehicleUserInfo}", "75a1a94f-ef38-4113-97e1-98b7dab30005", vehicleId, userId, JsonConvert.SerializeObject(vehicleUserInfo));

            ClaimsResult claimsResult = null;
            if (vehicleUserInfo is not null)
            {
                claimsResult = new ClaimsResult
                {
                    VehicleId = vehicleUserInfo.VehicleId,
                    UserId = vehicleUserInfo.UserId,
                    ExpiryTime = vehicleUserInfo.ExpiryTime,
                    Claims = vehicleUserInfo.Claims
                };
            }

            return new JsonResult(claimsResult);
        }
    }
}

