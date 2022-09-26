// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#ifndef UPLOAD_PROCESSOR_H
#define UPLOAD_PROCESSOR_H

#include <iostream>
#include <mqtt_client.h>
#include <mqtt_constants.h>
#include <mutex>
#include <optional>
#include <queue>
#include <string>
#include <thread>
#include <threading_utils.h>

#include "../../handlers/include/blob_upload_handler.h"
#include "../../handlers/include/blob_uri_handler.h"
#include "delete_processor.h"
#include "file_upload_request_message.h"
#include "internal_message.h"
#include "internal_message_types.h"
#include "upload_process_message.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    /**
     * @brief ComparePriority for upload process message priority queue
     */
    struct ComparePriority
    {
        bool operator()(UploadProcessMessage const &p1, UploadProcessMessage const &p2)
        {
            return p1.UploadRequestPayload.Priority > p2.UploadRequestPayload.Priority;
        }
    };

    class UploadProcessor
    {
      public:
        /**
         * @brief Construct UploadProcessor object
         *
         * @param mqttClient The mqtt client to publish messages
         * @param blobUriHandler Blob upload uri handler
         * @param deleteProcessor Processor to delete file
         */
        UploadProcessor(
            const std::shared_ptr<mqttclient::MqttClient> &mqttClient,
            const std::shared_ptr<BlobUriHandler> &blobUriHandler,
            const std::shared_ptr<DeleteProcessor> &deleteProcessor);

        /**
         * @brief Virtual destructor
         */
        virtual ~UploadProcessor() = default;

        /**
         * @brief Enqueue file upload request message
         *
         * @param message the message sent from host for file upload
         * @param correlationId The correlation id
         */
        void EnqueueProcess(const std::string &message, const CorrelationId &correlationId);

        /**
         * @brief Set data container path
         *
         * @param hostDataContainerPath container data path
         */
        void SetHostDataContainerPath(const std::string &hostDataContainerPath);

        /**
         * @brief Start upload process thread
         *
         * @param cancellationToken cancellation token
         */
        void Start(const CancellationToken::Ptr cancellationToken);

      protected:
        std::priority_queue<UploadProcessMessage, std::vector<UploadProcessMessage>, ComparePriority> messageQueue_;

      private:
        /**
         * @brief Dequeue process message from the priority queue
         *
         * @return Process message if messageQueue_ is not empty, or std::nullopt otherwise
         */
        std::optional<UploadProcessMessage> DequeueProcessMessage();

        /**
         * @brief Request upload blob uri to DeviceToCloud (TelemetryModule)
         *
         * @param blobPath Upload blob path
         * @param correlationId The correlation id
         */
        void RequestBlobUri(const std::string &blobPath, const CorrelationId &correlationId);

        /**
         * @brief Start uploading files by request upload payload
         *
         * @param processMessage Upload processing state message
         */
        void UploadFiles(UploadProcessMessage &processMessage);

        /**
         * @brief Validate file upload state from UploadProcessMessage
         *
         * @param processMessage Upload processing state message
         */
        void ValidateUploadState(UploadProcessMessage &processMessage);

        /**
         * @brief Send final upload status notification to DeviceToCloud (TelemetryModule).
         *
         * @param notification Upload notification message to DeviceToCloud (TelemetryModule)
         * @param correlationId The correlation id
         */
        void SendNotification(
            const vehicle::datacontracts::FileUploadNotification &notification,
            const CorrelationId &correlationId);

        /**
         * @brief Publish message to MQTT broker
         *
         * @param internalMessage the internal message to send MQTT broker
         * @param topic   the mqtt topic path
         * @param correlationId The correlation id
         */
        void PublishMessage(
            const vehicle::datacontracts::InternalMessage &internalMessage,
            const std::string &topic,
            const CorrelationId &correlationId);

        std::shared_ptr<mqttclient::MqttClient> mqttClient_;
        std::shared_ptr<BlobUriHandler> blobUriHandler_;
        std::shared_ptr<DeleteProcessor> deleteProcessor_;

        BlobUploadHandler blobUploadHandler_;
        std::string dataContainerPath_;
        std::mutex messageMutex_;

        const int ProcessorThreadSleepInSeconds = 1;
    };
} // namespace microsoft::azure::connectedcar::fileuploadmodule
#endif // UPLOAD_PROCESSOR_H
