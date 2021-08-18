// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace My.Extensions
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Vehicle.DataContracts;
    using Microsoft.Azure.Vehicle.Sdk;
    using Microsoft.Extensions.Logging;

    [Priority(1)]
    [ClaimRequirement("Services", "TypeB")]
    public class SimpleTelemetryHandler : TelemetryHandler
    {
        public override string ExtensionName => "SimpleTelemetryHandler";

        public override async Task HandleMessageAsync(
            DeviceTelemetryMessage requestBody,
            RequestDetails requestDetails,
            RequestMessageHeaders headers,
            IExtensionGatewayClient client,
            ILogger log)
        {
            await client.PutDeviceTelemetryAsync(requestDetails.DeviceName, requestBody);
        }
    }
}