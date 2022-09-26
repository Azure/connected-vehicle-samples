// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace Microsoft.Azure.ConnectedCar.SampleExtensions.EncryptedConfig
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ConnectedCar.DataContracts;
    using Microsoft.Azure.ConnectedCar.ExtensionDevelopmentKit.Shared;
    using Microsoft.Azure.ConnectedCar.Instrumentation;
    using Microsoft.Azure.ConnectedCar.Sdk;
    using Microsoft.Azure.ConnectedCar.Sdk.ExtensionBaseTypes;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     This is an example telemetry handler extension that responds to a request from a vehicle to upload files with a write enabled blob SAS uri.
    /// </summary>
    [TelemetryNamePrefix("fileUploadRequest")]
    public class BlobUploadRequestHandler : TelemetryHandler
    {
        private const string DeviceUploadCommandName = "BlobUploadRequestDetailsCommand";

        public override string ExtensionName => "BlobUploadRequestHandler";

        public override async Task HandleMessageAsync(DeviceTelemetryMessage deviceTelemetryMessage, RequestDetails requestDetails, RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            try
            {
                string requestedFileName = deviceTelemetryMessage.Payload as string;
                // Below example save all files under one container, "fileuploads".
                // To collect files into a different container per vehicle,
                // client.GetVehicleBlobSasUriAsync(requestDetails.VehicleId, requestedFileName);

                string fullPath = requestDetails.VehicleId + "/" + requestedFileName;
                string blobSasUri = await client.GetVehicleBlobSasUriAsync("fileuploads", fullPath);


                var payload = new
                {
                    blobSasUri,
                    requestedFileName
                };

                await client.SendDeviceCommandAsync(new DeviceCommandMessage(requestDetails.DeviceName,
                    DeviceUploadCommandName, payload));
            }
            catch (Exception e)
            {
                Instrument.Logger.LogException("F9DC7CB7-7983-4052-944D-A68C8AB81A95", "Exception occured processing blob upload request telemetry", e);
            }
        }
    }
}
