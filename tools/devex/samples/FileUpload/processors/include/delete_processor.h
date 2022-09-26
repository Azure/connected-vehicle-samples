// ---------------------------------------------------------------------------------
//  <copyright company="Microsoft">
//    Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------

#ifndef DELETE_PROCESSOR_H
#define DELETE_PROCESSOR_H

#include <iostream>
#include <queue>
#include <thread>
#include <threading_utils.h>

#include "upload_process_message.h"

namespace microsoft::azure::connectedcar::fileuploadmodule
{
    class DeleteProcessor
    {
      public:
        /**
         * @brief Default constructor
         */
        DeleteProcessor() = default;

        /**
         * @brief Virtual destructor
         */
        virtual ~DeleteProcessor() = default;

        /**
         * @brief Request deleting files in upload request payload
         *
         * @param processMessage Processing status message
         */
        void Delete(UploadProcessMessage processMessage);

        /**
         * @brief Start delete processor thread.
         *
         * @param cancellationToken Process cancellation token
         */
        void Start(const CancellationToken::Ptr cancellationToken);

      protected:
        /**
         * @brief Delete local files in message
         *
         * @param processMessage Processing status message
         */
        virtual void DeleteFilesInMessage(UploadProcessMessage processMessage);

        std::queue<UploadProcessMessage> messageQueue_;

        const int ProcessorThreadSleep = 30;
    };
} // namespace microsoft::azure::connectedcar::fileuploadmodule
#endif // DELETE_PROCESSOR_H