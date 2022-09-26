// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#include <curl/curl.h>
#include <fcntl.h>
#include <logging.h>
#include <memory>
#include <stdio.h>
#include <sys/stat.h>

#include "include/blob_upload_handler.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    static size_t ReadCallback(void *ptr, size_t size, size_t numElements, void *data)
    {
        FILE *stream = (FILE *)data;
        size_t retcode = fread(ptr, size, numElements, stream);

        return (curl_off_t)retcode;
    }

    static bool UploadFile(CURL *curl, const std::string &fileName, const std::string &uri)
    {
        CURLcode result;
        struct stat fileInfo;
        stat(fileName.c_str(), &fileInfo);

        FILE *file = fopen(fileName.c_str(), "rb");

        if (!file)
        {
            LogError("Failed to open the file %s\n", fileName.c_str());
            return false;
        }

        curl_easy_setopt(curl, CURLOPT_READFUNCTION, ReadCallback);
        curl_easy_setopt(curl, CURLOPT_UPLOAD, 1L);
        curl_easy_setopt(curl, CURLOPT_PUT, 1L);

        // Set blob-type header.
        struct curl_slist *headers = nullptr;
        headers = curl_slist_append(headers, "x-ms-blob-type: BlockBlob");
        curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);

        // Set upload url & read callback.
        curl_easy_setopt(curl, CURLOPT_URL, uri.c_str());
        curl_easy_setopt(curl, CURLOPT_READDATA, file);
        curl_easy_setopt(curl, CURLOPT_INFILESIZE_LARGE, (curl_off_t)fileInfo.st_size);

        result = curl_easy_perform(curl);
        if (result == CURLE_OK)
        {
            LogInfo("Successfully uploaded " + fileName + ".");
        }
        else
        {
            LogError("curl_easy_perform() failed: %s\n", curl_easy_strerror(result));
        }

        fclose(file);
        return (result == CURLE_OK);
    }

    bool BlobUploadHandler::UploadBlob(const std::string &fileName, const std::string &uri)
    {
        bool result = false;

        CURL *curl = curl_easy_init();
        if (curl)
        {
            result = UploadFile(curl, fileName, uri);
            curl_easy_cleanup(curl);
        }
        else
        {
            LogError("Can't initialize cUrl instance.");
        }

        return result;
    }
} // namespace microsoft::azure::connectedcar::fileuploadmodule