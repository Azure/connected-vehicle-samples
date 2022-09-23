// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#ifndef MODULE_MESSAGE_PROCESSOR_H
#define MODULE_MESSAGE_PROCESSOR_H

#include <iostream>
#include <mqtt_client.h>
#include <string>
#include <thread>
#include <threading_utils.h>

#include "auto_edge_hub_message.pb.h"
#include "delete_processor.h"
#include "upload_processor.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    class ModuleMessageProcessor
    {
      public:
        /**
         * @brief Construct ModuleMessageProcessor object
         *
         * @param mqttClient The mqtt client to receive messages
         */
        explicit ModuleMessageProcessor(const std::shared_ptr<mqttclient::MqttClient> &mqttClient);

        /**
         * @brief Virtual destructor
         */
        virtual ~ModuleMessageProcessor() = default;

        /**
         * @brief Processes a new message from the modules.
         *
         * @param message Message received from the modules
         * @param correlationId The correlation id
         */
        void ProcessMessageAsync(const std::string &message, const CorrelationId &correlationId);

        /**
         * @brief Start processor threads.
         *
         * @param cancellationToken The cancellation token
         * @param hostDataContainerPath The data container path from host.
         */
        void StartProcessorsAsync(
            const CancellationToken::Ptr cancellationToken,
            const std::string &hostDataContainerPath);

      private:
        /**
         * @brief Start delete worker thread.
         *
         * @param deleteProcessor The delete processor to start its worker thread
         * @param cancellationToken The cancellation token
         */
        static void StartDeleteWorker(
            const std::shared_ptr<DeleteProcessor> &deleteProcessor,
            const CancellationToken::Ptr cancellationToken);

        /**
         * @brief Start upload processor worker thread.
         *
         * @param uploadProcessor upload processor to start its worker thread
         * @param cancellationToken cancellation token
         */
        static void StartUploadWorker(
            const std::shared_ptr<UploadProcessor> &uploadProcessor,
            const CancellationToken::Ptr cancellationToken);

        std::shared_ptr<BlobUriHandler> blobUriHandler_;
        std::shared_ptr<UploadProcessor> uploadProcessor_;
        std::shared_ptr<DeleteProcessor> deleteProcessor_;
    };
} // namespace microsoft::azure::connectedcar::fileuploadmodule
#endif // MODULE_MESSAGE_PROCESSOR_H