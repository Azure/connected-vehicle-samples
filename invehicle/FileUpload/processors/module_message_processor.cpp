// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#include <logging.h>
#include <optional>

#include "../handlers/include/blob_uri_handler.h"
#include "blob_upload_uri_response.h"
#include "include/module_message_processor.h"
#include "internal_message.h"
#include "internal_message_types.h"

// The MQTT client library has a bug to be included before the boost/program_options header.
// module_initialization.h contains boost/program_options.
#include <module_initialization.h>

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    using namespace microsoft::azure::connectedcar::constants;
    using namespace microsoft::azure::connectedcar::datacontracts;
    using namespace microsoft::azure::connectedcar::mqttclient;
    using namespace microsoft::azure::connectedcar::mcvp::datacontracts;
    using namespace microsoft::azure::connectedcar::vehicle::datacontracts;
    using namespace nlohmann;

    ModuleMessageProcessor::ModuleMessageProcessor(const std::shared_ptr<MqttClient> &mqttClient)
    {
        blobUriHandler_ = std::make_shared<BlobUriHandler>();
        deleteProcessor_ = std::make_shared<DeleteProcessor>();
        uploadProcessor_ = std::make_shared<UploadProcessor>(mqttClient, blobUriHandler_, deleteProcessor_);
    }

    void ModuleMessageProcessor::StartProcessorsAsync(
        CancellationToken::Ptr cancellationToken,
        const std::string &hostDataContainerPath)
    {
        uploadProcessor_->SetHostDataContainerPath(hostDataContainerPath);

        std::thread deleteWorker = std::thread(StartDeleteWorker, deleteProcessor_, cancellationToken);
        std::thread uploadWorker = std::thread(StartUploadWorker, uploadProcessor_, cancellationToken);

        deleteWorker.join();
        uploadWorker.join();
    }

    void ModuleMessageProcessor::StartDeleteWorker(
        const std::shared_ptr<DeleteProcessor> &deleteProcessorPtr,
        CancellationToken::Ptr cancellationToken)
    {
        deleteProcessorPtr->Start(cancellationToken);
    }

    void ModuleMessageProcessor::StartUploadWorker(
        const std::shared_ptr<UploadProcessor> &uploadProcessorPtr,
        CancellationToken::Ptr cancellationToken)
    {
        uploadProcessorPtr->Start(cancellationToken);
    }

    void ModuleMessageProcessor::ProcessMessageAsync(const std::string &message, const CorrelationId &correlationId)
    {
        try
        {
            // Convert the message to json internal message
            InternalMessage internalMessage = json::parse(message);
            LogTrace("Received a \"" + internalMessage.MessageType + "\" message.");

            if (internalMessage.MessageType == InternalMessageTypes::FileUploadRequest)
            {
                uploadProcessor_->EnqueueProcess(internalMessage.Payload, correlationId);
            }
            else if (internalMessage.MessageType == InternalMessageTypes::ArbitraryToDevice)
            {
                BlobUploadUriResponse blobUriResponse = json::parse(internalMessage.Payload);
                blobUriHandler_->AddBlobUri(
                    blobUriResponse.RequestedFileName,
                    blobUriResponse.BlobSasUri,
                    correlationId);
            }
            else
            {
                LogWarn(
                    correlationId,
                    "Error processing message. Unknown message type: " + internalMessage.MessageType);
            }
        }
        catch (json::exception &e)
        {
            LogWarn(correlationId, "Error in parsing message, %s.", e.what());
        }
    }
} // namespace microsoft::azure::connectedcar::fileuploadmodule