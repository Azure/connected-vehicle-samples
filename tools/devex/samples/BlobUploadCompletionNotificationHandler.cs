// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) 2018 Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace Microsoft.Azure.ConnectedCar.SampleExtensions.EncryptedConfig
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ConnectedCar.DataContracts;
    using Microsoft.Azure.ConnectedCar.ExtensionDevelopmentKit.Shared;
    using Microsoft.Azure.ConnectedCar.Instrumentation;
    using Microsoft.Azure.ConnectedCar.Sdk;
    using Microsoft.Azure.ConnectedCar.Sdk.ExtensionBaseTypes;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     This is an example telemetry handler extension that responds to the completion of uploading of a file to blob
    ///     storage.
    /// </summary>
    [TelemetryNamePrefix("fileBlobUploadCompletion")]
    public class BlobUploadCompletionNotificationHandler : TelemetryHandler
    {
        public override string ExtensionName => "BlobUploadCompletionNotificationHandler";

        public override async Task HandleMessageAsync(DeviceTelemetryMessage requestBody, RequestDetails requestDetails,
            RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            try
            {
                IDictionary<string, object> properties = requestBody.AdditionalProperties.ToDictionary(p => p.Key, p => (object)p.Value);

                dynamic payload = requestBody.Payload;

                Instrument.Logger.LogMessage(EventLevel.Informational, "377C0305-3B43-4595-B59B-CA8526CAE04B", $"Upload Finished id, {payload.UploadId}, for file {payload.UploadFileList}, at {payload.LastUploadTime}");

                bool result = (bool)payload.UploadResult;

                if (result)
                {
                    // Upload Notification Details
                    dynamic notification = new JObject();
                    notification.UploadId = payload.UploadId;
                    notification.UploadResult = payload.UploadResult;
                    notification.UploadFileList = payload.UploadFileList;
                    notification.Metadata = payload.Metadata;
                    notification.LastUploadTime = payload.LastUploadTime;

                    await client.SendToAnalyticsPipeline(properties, new
                    {
                        Payload = notification,
                        MessageId = requestBody.MessageId
                    });
                }
                else
                {
                    Instrument.Logger.LogMessage(EventLevel.Error, "377C0305-3B43-4595-B59B-CA8526CAE04B", $"Failure from  id, {payload.UploadId}, {payload.Metadata}.");
                }
            }
            catch (Exception e)
            {
                Instrument.Logger.LogException("25E2EF76-F189-4215-90C0-47957AD03BDF",
                    "Exception sending blob upload results to analytics", e);
            }
        }
    }
}