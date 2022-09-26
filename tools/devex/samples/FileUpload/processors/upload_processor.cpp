// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#include <logging.h>
#include <nlohmann/json.hpp>

#include "include/upload_processor.h"
#include "mqtt_client_exception.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    using namespace microsoft::azure::connectedcar::constants;
    using namespace microsoft::azure::connectedcar::datacontracts;
    using namespace microsoft::azure::connectedcar::mqttclient;
    using namespace microsoft::azure::connectedcar::vehicle::datacontracts;
    using namespace nlohmann;

    UploadProcessor::UploadProcessor(
        const std::shared_ptr<MqttClient> &mqttClient,
        const std::shared_ptr<BlobUriHandler> &blobUriHandler,
        const std::shared_ptr<DeleteProcessor> &deleteProcessor) :
        mqttClient_(mqttClient),
        blobUriHandler_(blobUriHandler), deleteProcessor_(deleteProcessor)
    {
    }

    void UploadProcessor::SetHostDataContainerPath(const std::string &hostDataContainerPath)
    {
        if (!hostDataContainerPath.empty())
        {
            dataContainerPath_ = hostDataContainerPath;
        }
        else
        {
            throw std::runtime_error(
                "HostDataContainerPath is empty. Can't start the file upload module due to missing configuration.");
        }
    }

    void UploadProcessor::EnqueueProcess(const std::string &message, const CorrelationId &correlationId)
    {
        try
        {
            FileUploadRequestMessage uploadRequest = json::parse(message);

            UploadProcessMessage processMessage;
            processMessage.Create(uploadRequest, dataContainerPath_, correlationId.ToString());

            messageMutex_.lock();
            messageQueue_.push(processMessage);
            messageMutex_.unlock();
        }
        catch (json::exception &e)
        {
            LogError(correlationId, "Error enqueue process message, %s.", e.what());
        }
    }

    std::optional<UploadProcessMessage> UploadProcessor::DequeueProcessMessage()
    {
        std::unique_lock<std::mutex> dequeueLock(messageMutex_);

        if (!messageQueue_.empty())
        {
            UploadProcessMessage processMessage = messageQueue_.top();
            messageQueue_.pop();
            return processMessage;
        }

        return std::nullopt;
    }

    void UploadProcessor::Start(const CancellationToken::Ptr cancellation_token)
    {
        while (!cancellation_token->IsCancellationRequested())
        {
            std::optional<UploadProcessMessage> processMessage = DequeueProcessMessage();
            if (processMessage.has_value() && !processMessage->IsEmptyMessage())
            {
                UploadFiles(*processMessage);
            }

            std::this_thread::sleep_for(std::chrono::seconds(ProcessorThreadSleepInSeconds));
        }
    }

    void UploadProcessor::UploadFiles(UploadProcessMessage &processMessage)
    {
        CorrelationId correlationId = CorrelationId(processMessage.CorrelationId);

        processMessage.UploadResult = true;
        for (FileUploadResult fileUpload : processMessage.UploadFileList)
        {
            if (!processMessage.HasExpired() && !fileUpload.UploadResult)
            {
                std::string destinationBlobPath = processMessage.GetBlobPath(fileUpload.FileName);
                RequestBlobUri(destinationBlobPath, correlationId);
                std::string blobUri = blobUriHandler_->WaitForBlobUri(destinationBlobPath, 120, correlationId);

                if (!blobUri.empty())
                {
                    std::string localFilePath = processMessage.GetLocalPath(fileUpload.FileName);
                    fileUpload.UploadResult = blobUploadHandler_.UploadBlob(localFilePath, blobUri);
                }
                else
                {
                    fileUpload.UploadResult = false;
                }

                processMessage.LastUploadTime = std::chrono::system_clock::now();
                processMessage.UploadResult = processMessage.UploadResult & fileUpload.UploadResult;
            }
        }

        ValidateUploadState(processMessage);
    }

    void UploadProcessor::ValidateUploadState(UploadProcessMessage &processMessage)
    {
        CorrelationId correlationId = CorrelationId(processMessage.CorrelationId);

        if (processMessage.UploadResult || processMessage.HasExpired() || processMessage.RetriesRemaining <= 0)
        {
            SendNotification(processMessage.CreateNotification(), correlationId);
            deleteProcessor_->Delete(processMessage);

            if (processMessage.HasExpired() || processMessage.RetriesRemaining <= 0)
            {
                LogTrace(
                    "Upload failed by upload timeout or meeting max retries, message expiration: %b, "
                    "total remaining retries: %i.",
                    processMessage.HasExpired(),
                    processMessage.RetriesRemaining);
            }
        }
        else
        {
            processMessage.RetriesRemaining--;
            messageQueue_.push(processMessage);
            LogTrace("Retry file upload for the message, %s.", processMessage.UploadRequestPayload.UploadId.c_str());
        }
    }

    void UploadProcessor::PublishMessage(
        const InternalMessage &internalMessage,
        const std::string &topic,
        const CorrelationId &correlationId)
    {
        try
        {
            json j = internalMessage;
            std::string serializedInternalMessage = j.dump();

            MqttProperties mqttProperties;
            mqttProperties.emplace_back(std::make_tuple(MqttPropertyId::CorrelationData, correlationId.ToString()));

            mqttClient_->Publish(topic, serializedInternalMessage, Qos::AT_LEAST_ONCE, mqttProperties);
        }
        catch (const MqttClientException &e)
        {
            LogWarn("Mqtt Client Exception: %s", e.what());
        }
    }

    void UploadProcessor::RequestBlobUri(const std::string &blobPath, const CorrelationId &correlationId)
    {
        InternalMessage internalMessage;
        internalMessage.MessageType = InternalMessageTypes::ArbitraryToCloud;
        internalMessage.Payload = blobPath;

        PublishMessage(internalMessage, MqttConstants::Topics::RequestBlobUri, correlationId);

        LogInfo(
            correlationId,
            "Successfully sent blob upload request, %s, %s.",
            blobPath.c_str(),
            MqttConstants::Topics::RequestBlobUri.c_str());
    }

    void UploadProcessor::SendNotification(
        const FileUploadNotification &notification,
        const CorrelationId &correlationId)
    {
        json json_data = notification;

        InternalMessage internalMessage;
        internalMessage.MessageType = InternalMessageTypes::ArbitraryToCloud;
        internalMessage.Payload = json_data.dump();

        PublishMessage(internalMessage, MqttConstants::Topics::FileUploadNotification, correlationId);

        LogInfo(
            correlationId,
            "Successfully sent notification message, %s, %s.",
            notification.UploadId.c_str(),
            MqttConstants::Topics::FileUploadNotification.c_str());
    }
} // namespace microsoft::azure::connectedcar::fileuploadmodule
