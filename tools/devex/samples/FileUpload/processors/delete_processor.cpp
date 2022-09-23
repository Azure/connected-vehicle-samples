// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#include <boost/filesystem.hpp>
#include <logging.h>
#include <nlohmann/json.hpp>

#include "include/delete_processor.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    using namespace microsoft::azure::connectedcar::vehicle::datacontracts;
    using namespace nlohmann;

    void DeleteProcessor::Delete(UploadProcessMessage processMessage)
    {
        if (processMessage.HasFileRetentionExpiry())
        {
            LogTrace("Delete " + processMessage.UploadRequestPayload.UploadId + ".");
            DeleteFilesInMessage(processMessage);
        }
        else
        {
            LogTrace("Enqueue " + processMessage.UploadRequestPayload.UploadId + " to delete.");
            messageQueue_.push(processMessage);
        }
    }

    void DeleteProcessor::Start(const CancellationToken::Ptr cancellation_token)
    {
        while (!cancellation_token->IsCancellationRequested())
        {
            if (!messageQueue_.empty())
            {
                UploadProcessMessage processMessage = messageQueue_.front();
                messageQueue_.pop();

                if (processMessage.HasFileRetentionExpiry())
                {
                    DeleteFilesInMessage(processMessage);
                }
                else
                {
                    messageQueue_.push(processMessage);
                }
            }

            std::this_thread::sleep_for(std::chrono::seconds(ProcessorThreadSleep));
        }
    }

    void DeleteProcessor::DeleteFilesInMessage(UploadProcessMessage processMessage)
    {
        for (FileUploadResult fileUpload : processMessage.UploadFileList)
        {
            std::string localFilePath = processMessage.GetLocalPath(fileUpload.FileName);

            try
            {
                if (boost::filesystem::exists(localFilePath))
                {
                    boost::filesystem::remove(localFilePath);
                    LogTrace("Deleted " + localFilePath + ".");
                }
                else
                {
                    LogTrace("Skipped deleting because " + localFilePath + " does not exist.");
                }
            }
            catch (boost::filesystem::filesystem_error &e)
            {
                LogWarn("Exception thrown while deleting file" + std::string(e.what()));
            }
        }
    }
} // namespace microsoft::azure::connectedcar::fileuploadmodule