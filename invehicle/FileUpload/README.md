# File Upload Module

## Introduction
The File Upload Module is a sample pattern through which vehicles can send files directly to cloud storage. Also known as "FatPipe," this sample pattern lets vehicles send files that are too large for MQTT payloads. 

This sample is comprised of edge components (the majority of files, found in this directory), and cloud components in the [devex samples](../../tools/devex/samples/) directory. These cloud components are extensions that are to be called by an MCVP resource.

## Prerequisites
1. VAS (Vehicle Abstraction Service) must be compiled and ready to run.
2. The /var/lib/edgemodules folder must be configured by the prerequisite of VAS module. File Upload Module will use the same folder to receive upload files from other applications. The folder /var/lib/edgemodules folder must exist before both VAS and file upload module start. 



## Build
To build locally, in the file_upload_module/ root directory, run:
```sh
mkdir build
cd build
cmake ..
make -j${nproc}
```



## Run
```sh
./file-upload-module --config=../../module_settings.json
```



## Environment variables for arbitrary topics

The command module & the telemetry module route messages from arbitrary topics to the file upload module by below environment variables.

```
// autoEdgeHub:
export AUTOEDGE_HUB_ARBITRARY_TO_DEVICE_TOPIC_PATTERNS=to_device/1/{VehicleId}/{DeviceName}/#

// CommandModule:
export AUTOEDGE_COMMAND_MODULE_ARBITRARY_TOPIC_ROUTES="[ { \"Source\": \" to_device/1/{VehicleId}/{DeviceName}/fileUpload/FileUploadBlobUri\", \"Sink\": \"local/fileUpload/FileUploadBlobUri\" } ]"

// TelemetryModule:
export AUTOEDGE_TELEMETRY_MODULE_ARBITRARY_TOPIC_ROUTES="[ { \"Source\": \"arbitrarytocloud/fileUpload/fileBlob-UploadRequest\", \"Sink\": \"to_cloud/1/fileUpload/fileBlob-UploadRequest\" }, { \"Source\": \" arbitrarytocloud/fileUpload/fileBlob-CompleteNotification\", \"Sink\": \" to_cloud/1/fileUpload/fileBlob-CompleteNotification\" } ]"


```



* AutoEdge subscribes the cloud topic, to_device_v1/fileUpload/#. The claim, the prefix + topic name, with the value "sub" must exist to subscribe it.

* Topics sending D2C messages, AUTOEDGE_COMMAND_MODULE_ARBITRARY_TOPIC_ROUTES, must have the claim, "pub".
* The topic receiving the C2D message must be the sub topic of the AUTOEDGE_HUB_ARBITRARY_TO_DEVICE_TOPIC_PATTERNS. 

```

"claims": [
        {
            "name": "//mcvp/mqtt/to_cloud/1/fileUpload/fileBlobCompleteNotification",
            "values": [
                {
                    "value": "pub"
                }
            ]
        },
        {
            "name": "//mcvp/mqtt/to_cloud/1/fileUpload/fileBlobUploadRequest",
            "values": [
                {
                    "value": "pub"
                }
            ]
        },
        {
            "name": "//mcvp/mqtt/to_device/1/{VehicleId}/{DeviceName}/#",
            "values": [
                {
                    "value": "sub"
                }
            ]
        },
        {
            "name": "//mcvp/mqtt/to_device/1/{VehicleId}/{DeviceName}/fileUpload/FileUploadBlobUri",
            "values": [
                {
                    "value": "pub"
                }
            ]
        }
    ],

```



## Test

MCVP Cloud Platform and Analytics support blob upload request from vehicle module by extensions. The sample extensions are available in the [devex samples](../../tools/devex/samples/) directory. To run end-to-end file upload, Platform must install Analytics extension to plug Azure Storage Analytics storage into platform service.

To run the test locally instead of deploying the entire MCVP cloud service, we can send and receive messages with CURL and VAS for File Upload Module. The instructions below introduce how to send messages step by step.

### 1. Run VAS to receive messages

File Upload Module receives message signals from VAS (Vehicle Abstraction Service). VAS must run before file upload module starts. 

```
./vehicle-abstraction-module --config=../../module_settings.json
```

### 2. Sending FileUploadRequest Message

Prepare a sample file, test.txt with any text inside, under the data container path in the module_settings.json

```
 "FileUploadModule": {
        "DataContainerPath": "/var/lib/edgemodules/file-drop"
    }
```

Open up a terminal and run the command below to send a file upload request message to VAS. VAS will convert it to internal message and signal to the file upload module. 

```
curl -H "Content-Type:application/json" --data "{\"MessageType\": \"FileUploadRequest\", \"Payload\": \"{\\\"UploadId\\\": \\\"UploadId123\\\", \\\"TimeToLive\\\": \\\"120\\\", \\\"FileList\\\": [\\\"test.txt\\\"], \\\"Priority\\\": 1, \\\"FileRetentionInSec\\\": \\\"180\\\", \\\"Metadata\\\": \\\"testMetaData\\\"}\"}<EOF>" -POST --unix-socket /var/lib/edgemodules/to-val.sock http://localhost/post/all 
```
Confirm a message requesting a blob upload URL, "Successfully sent blob upload request", from terminal window of FileUploadModule.



### 3. Sending Blob Upload URI Message 

On the terminal of #2, run the below command to send blob upload response. 

Create a blob URI with the filename. Replace it with "____###BLOB_URI_WITH_SAS_TOKEN###__" below. 

```
curl -H "Content-Type:application/json" --data "{\"MessageType\": \"FileUploadBlobUri\", \"Payload\": \"{\\\"RequestedFileName\\\": \\\"UploadId123/test.txt\\\", \\\"BlobSasUri\\\": \\\"____###BLOB_URI_WITH_SAS_TOKEN###__"}\"}<EOF>" -POST --unix-socket /var/lib/edgemodules/to-val.sock http://localhost/post/all 

```

When this message gets received, the module should upload file to cloud blob storage. Check the storage account after success message, "Successfully sent notification message", on terminal window of FileUploadModule.

How to create Blob Upload URI: 

Blob Upload URI gets delivered by MCVP Cloud Service. To create your own BlobUploadUri for test message, 

**i**. Create a simple C# application by following the instructions in [Create a service SAS for a container or blob with .NET](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-service-sas-create-dotnet?tabs=dotnet) .

**ii.** Obtain a link from Azure Portal UI.

1. Log in Azure portal and go to the azure storage account overview that you want to upload file.
2. Select a container and upload a blob name, "UploadId123/test.txt".
3. Click the "..." link and select "Generate SAS". Select "Read/Write/Create" permission and click "Generate SAS token and URL".

The format of Blob Upload Uri must be like below.

https://<StorageAccountName>.blob.core.windows.net/<UploadContainer>/UploadId123/test.txt?sv=2018-03-28&sr=b&sig=<SecretKeyCreateByAzurePoral>&se=2020-10-01T23%3A04%3A25Z&sp=rcw

