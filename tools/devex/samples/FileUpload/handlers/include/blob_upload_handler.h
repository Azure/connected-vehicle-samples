// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#ifndef BLOB_UPLOAD_HANDLER_H
#define BLOB_UPLOAD_HANDLER_H

#include <chrono>
#include <iostream>

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    class BlobUploadHandler
    {
      public:
        /**
         * @brief Upload blob request by Upload Processor
         *
         * @param fileName Upload file name
         * @param uri Blob uri string with access token
         *
         * @return True/flase of upload state
         */
        bool UploadBlob(const std::string &fileName, const std::string &uri);
    };
} // namespace microsoft::azure::connectedcar::fileuploadmodule

#endif