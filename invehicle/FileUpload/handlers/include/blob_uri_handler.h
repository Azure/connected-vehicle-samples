// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#ifndef BLOB_URI_HANDLER_H
#define BLOB_URI_HANDLER_H

#include <chrono>
#include <iostream>
#include <iterator>
#include <map>
#include <memory>
#include <string.h>

#include "correlation_id.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    /**
     * @brief The common structure of BlobUriCacheItem
     *
     */
    struct BlobUriCacheItem
    {
        std::string FileName;
        std::string BlobUri;
        std::chrono::steady_clock::time_point CreatedTime;
    };

    class BlobUriHandler
    {
      public:
        /**
         * @brief Default constructor
         */
        BlobUriHandler() = default;

        /**
         * @brief Virtual destructor
         */
        virtual ~BlobUriHandler() = default;

        /**
         * @brief Add blob uri from Cloud2Device(command module) to blob uri cache
         *
         * @param fileName upload file name
         * @param uri blob uri with access token
         * @param correlationId The correlation id
         */
        void AddBlobUri(const std::string &fileName, const std::string &uri, const CorrelationId &correlationId);

        /**
         * @brief Wait blob uri call by upload processor
         *
         * @param fileName upload file name
         * @param seconds wait for blob uri
         * @param correlationId The correlation id
         *
         * @return blob uri string
         */
        std::string WaitForBlobUri(const std::string &fileName, int seconds, const CorrelationId &correlationId);

      protected:
        std::map<std::string, BlobUriCacheItem> blobUriCache_;

      private:
        /**
         * @brief Optimize BlobUri Cache. Delete old uri if the size of cache is bigger than MaxCacheSize
         */
        void OptimizeCache();

        /**
         * @brief Find blob uri from blob uri cache
         *
         * @param fileName upload file name
         * @param correlationId The correlation id
         *
         * @return blob uri string
         */
        std::string FindBlobUri(const std::string &fileName, const CorrelationId &correlationId);

        const unsigned int MaxCacheSize = 10;
        const int BlobCacheLookupSleep = 2;
    };
} // namespace microsoft::azure::connectedcar::fileuploadmodule

#endif