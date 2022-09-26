// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#ifndef UPLOAD_PROCESS_MESSAGE_H
#define UPLOAD_PROCESS_MESSAGE_H

#include <chrono>
#include <iostream>
#include <string>
#include <vector>

#include "file_upload_notification.h"
#include "file_upload_request_message.h"
#include "file_upload_result.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    using nlohmann::json;

    /**
     * @brief The structure of UploadProcessMessage
     *
     */
    struct UploadProcessMessage
    {
        vehicle::datacontracts::FileUploadRequestMessage UploadRequestPayload;
        std::string ContainerDataPath;
        std::vector<vehicle::datacontracts::FileUploadResult> UploadFileList;
        bool UploadResult = false;
        std::chrono::system_clock::time_point LastUploadTime;
        int RetriesRemaining = 3;
        std::string CorrelationId;

        /**
         * @brief Create UploadProcessMessage object
         *
         * @param uploadRequest FileUploadRequestMessage
         * @param containerDataPath ContainerDataPath shared with host
         * @param correlationId The correlation id
         */
        void Create(
            vehicle::datacontracts::FileUploadRequestMessage uploadRequest,
            const std::string &containerDataPath,
            const std::string &correlationId)
        {
            UploadRequestPayload = uploadRequest;
            ContainerDataPath = containerDataPath;
            CorrelationId = correlationId;

            for (std::string fileName : uploadRequest.FileList)
            {
                vehicle::datacontracts::FileUploadResult uploadResult;
                uploadResult.FileName = fileName;
                UploadFileList.push_back(uploadResult);
            }
        }

        /**
         * @brief Check if this message is initialized by checking container path.
         *
         * @return True/false of message initialization state.
         */
        bool IsEmptyMessage()
        {
            return ContainerDataPath.empty();
        }

        /**
         * @brief Message expiry check
         *
         * @return True/false of expiry state
         */
        bool HasExpired()
        {
            auto seconds = std::chrono::duration_cast<std::chrono::seconds>(
                UploadRequestPayload.TimeToLiveExpiry - std::chrono::steady_clock::now());
            if (seconds.count() > 0)
            {
                return false;
            }

            return true;
        }

        /**
         * @brief File retention expiry check
         *
         * @return True/false of file retention expiry state
         */
        bool HasFileRetentionExpiry()
        {
            if (UploadRequestPayload.FileRetentionInSec.empty())
            {
                return true;
            }

            auto seconds = std::chrono::duration_cast<std::chrono::seconds>(
                UploadRequestPayload.FileRetentionExpiry - std::chrono::steady_clock::now());
            if (seconds.count() > 0)
            {
                return false;
            }

            return true;
        }

        /**
         * @brief Get the local file path from payload
         *
         * @param fileName Upload file name
         *
         * @return local file path string
         */
        std::string GetLocalPath(const std::string &fileName)
        {
            return ContainerDataPath + "/" + fileName;
        }

        /**
         * @brief Get the destination blob path from payload
         *
         * @param fileName Upload file name
         *
         * @return cloud blob path string
         */
        std::string GetBlobPath(const std::string &fileName)
        {
            return UploadRequestPayload.UploadId + "/" + fileName;
        }

        /**
         * @brief Create the file upload notification from UploadProcessMessage
         *
         * @return notification of file upload state.
         */
        vehicle::datacontracts::FileUploadNotification CreateNotification()
        {
            vehicle::datacontracts::FileUploadNotification notification;
            notification.UploadId = UploadRequestPayload.UploadId;
            notification.UploadResult = UploadResult;
            notification.Metadata = UploadRequestPayload.Metadata;
            for (vehicle::datacontracts::FileUploadResult uploadResult : UploadFileList)
            {
                json json_data = uploadResult;
                notification.UploadFileList.push_back(json_data.dump());
            }

            std::time_t t = std::chrono::system_clock::to_time_t(LastUploadTime);
            notification.LastUploadTime = std::ctime(&t);

            return notification;
        }
    };
} // namespace microsoft::azure::connectedcar::fileuploadmodule
#endif // UPLOAD_PROCESS_MESSAGE_H