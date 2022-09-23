// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#include <assert.h>
#include <logging.h>
#include <thread>
#include <threading_utils.h>

#include "include/blob_uri_handler.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    void BlobUriHandler::AddBlobUri(
        const std::string &fileName,
        const std::string &uri,
        const CorrelationId &correlationId)
    {
        OptimizeCache();

        if (blobUriCache_.find(fileName) == blobUriCache_.end())
        {
            BlobUriCacheItem item;
            item.FileName = fileName;
            item.BlobUri = uri;
            item.CreatedTime = std::chrono::steady_clock::now();
            blobUriCache_[fileName] = item;

            LogTrace(correlationId, "Add a new blob uri entry, %s.", fileName.c_str());
        }
        else
        {
            blobUriCache_[fileName].BlobUri = uri;
            blobUriCache_[fileName].CreatedTime = std::chrono::steady_clock::now();

            LogTrace(correlationId, "Update existing uri entry, %s.", fileName.c_str());
        }
    }

    std::string BlobUriHandler::WaitForBlobUri(
        const std::string &fileName,
        int timeoutInSec,
        const CorrelationId &correlationId)
    {
        std::chrono::time_point<std::chrono::steady_clock> current = std::chrono::steady_clock::now();
        int durationInSeconds = 0;

        do
        {
            // Wait for blob uri return.
            std::this_thread::sleep_for(std::chrono::seconds(BlobCacheLookupSleep));
            durationInSeconds =
                (std::chrono::duration_cast<std::chrono::seconds>(std::chrono::steady_clock::now() - current)).count();

            std::string blobUri = FindBlobUri(fileName, correlationId);
            if (!blobUri.empty())
            {
                return blobUri;
            }

        } while (timeoutInSec > durationInSeconds);

        LogWarn(correlationId, "BlobUploadRequest did not receive upload token for, %s.", fileName.c_str());

        return std::string();
    }

    std::string BlobUriHandler::FindBlobUri(const std::string &fileName, const CorrelationId &correlationId)
    {
        if (blobUriCache_.find(fileName) != blobUriCache_.end())
        {
            // Find the item from blobUriCache_, and remove it from the cache
            // to keep the optimal size of cache. There is no chance to reuse
            // the same item after being picked up.
            std::string blobUri = blobUriCache_[fileName].BlobUri;
            blobUriCache_.erase(fileName);

            return blobUri;
        }

        LogTrace(correlationId, "%s is not found...", fileName.c_str());

        return std::string();
    }

    void BlobUriHandler::OptimizeCache()
    {
        // Erase the oldest item from blobUriCache_.
        if (blobUriCache_.size() > MaxCacheSize)
        {
            std::map<std::string, BlobUriCacheItem>::iterator it = blobUriCache_.begin();
            std::string key = it->second.FileName;
            std::chrono::steady_clock::time_point timestamp = it->second.CreatedTime;

            while (it != blobUriCache_.end())
            {
                assert(it != blobUriCache_.end());
                if (timestamp > it->second.CreatedTime)
                {
                    key = it->second.FileName;
                    timestamp = it->second.CreatedTime;
                }
                ++it;
            }

            blobUriCache_.erase(key);

            LogTrace("Remove old item, %s, from the queue.", key.c_str());
        }
    }
} // namespace microsoft::azure::connectedcar::fileuploadmodule