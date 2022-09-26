// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

#include <fstream>
#include <iostream>
#include <nlohmann/json.hpp>
#include <thread>

#include <logging.h>
#include <mqtt_client.h>
#include <mqtt_client_exception.h>
#include <mqtt_constants.h>
#include <signal_handler.h>
#include <string_operations.h>
#include <threading_utils.h>

#include <boost/program_options.hpp> // The MQTT client library has a bug which requires the mqtt client header to be included before the boost/program_options header.

#include "configuration.h"
#include "configuration_keys.h"
#include "internal_message.h"
#include "module_constants.h"
#include "module_initialization.h"

#include "processors/include/module_message_processor.h"

using namespace microsoft::azure::connectedcar::autoedge;
using namespace microsoft::azure::connectedcar;
using namespace microsoft::azure::connectedcar::constants;
using namespace microsoft::azure::connectedcar::fileuploadmodule;
using namespace microsoft::azure::connectedcar::vehicle::datacontracts;
using namespace microsoft::azure::connectedcar::mqttclient;
using namespace microsoft::azure::connectedcar::datacontracts;
using namespace nlohmann;

namespace
{
    // These members are declared here as they need to be alive throughout the lifetime of the
    // device MqttClient instance.
    bool isConnected = false;
    std::mutex mqttMutex;
    std::condition_variable isConnectedConditionVar;
    MqttConnectionStatusCallback mqttConnectionStatusCallback;
} // namespace

/**
 * @brief Connects the module to the MQTT broker.
 */
bool ConnectToMqttBroker(std::shared_ptr<MqttClient> &mqttClient)
{

    std::string mqttBrokerAddress = Configuration::GetEnvironmentConfigOrDefault(
        ConfigurationKeys::Mqtt::BrokerAddress,
        MqttConstants::DefaultBrokerAddress);
    std::string mqttBrokerPort = Configuration::GetEnvironmentConfigOrDefault(
        ConfigurationKeys::Mqtt::BrokerPort,
        MqttConstants::DefaultBrokerPort);

    mqttClient =
        std::make_shared<MqttClient>(mqttBrokerAddress, mqttBrokerPort, MqttConstants::ClientIds::FileUploadModule);

    mqttConnectionStatusCallback = [](int resultCode, MqttConnectReasonCode reasonCode) {
        if (resultCode != 0)
        {
            LogError("Connection to MQTT broker failed. Result code: %d", resultCode);
            return;
        }

        switch (reasonCode)
        {
        case MqttConnectReasonCode::UNKNOWN:
            LogTrace("MQTT connection status reason code unknown");
            break;
        case MqttConnectReasonCode::SUCCESS: {
            LogInfo("Connected to MQTT broker");
            std::scoped_lock<std::mutex> mqttOnConnectMutexLock(mqttMutex);
            isConnected = true;
            isConnectedConditionVar.notify_one();
            break;
        }
        default:
            LogTrace("Connection to MQTT broker failed. MQTT Reason code: %d", reasonCode);
            break;
        }
    };

    LogInfo("Connect to MQTT broker Callback");

    mqttClient->Connect(mqttConnectionStatusCallback);

    std::unique_lock<std::mutex> mqttConnectedMutexLock(mqttMutex);

    // Wait for connection to MQTT broker before proceeding
    isConnectedConditionVar.wait(mqttConnectedMutexLock, [] { return isConnected; });

    return true;
}

void SubscribeToMqttBroker(
    std::shared_ptr<ModuleMessageProcessor> &moduleMessageProcessor,
    std::shared_ptr<MqttClient> &mqttClient)
{

    SubscribeHandler handler = [&](unsigned short packetId,
                                   const std::string &topic,
                                   const std::string &payload,
                                   const MqttProperties &mqttProperties) {
        try
        {
            std::optional<std::string> optionalCorrelationId =
                MqttPropertiesBuilder::GetPropertyValueByPropertyId(mqttProperties, MqttPropertyId::CorrelationData);
            CorrelationId correlationId =
                CorrelationId(optionalCorrelationId.has_value() ? optionalCorrelationId.value() : "");

            LogTrace(correlationId, "New message: %s", payload.c_str());
            moduleMessageProcessor->ProcessMessageAsync(payload, correlationId);
        }
        catch (const MqttClientException &e)
        {
            LogError("Error in mqtt client publishing message: %s", e.what());
            return false;
        }
        return true;
    };

    std::unordered_map<std::string, Qos> subscribeTopics = {
        {MqttConstants::Topics::RequestFileUpload, Qos::AT_LEAST_ONCE},
        {MqttConstants::Topics::FileUploadBlobUri, Qos::AT_LEAST_ONCE}};

    for (const auto &[k, v] : subscribeTopics)
    {
        mqttClient->Subscribe(k, handler, v);
    }
}

/**
 * @brief Creates the command line argument descriptions for this service.
 */
boost::program_options::options_description CreateCommandLineDescription()
{
    boost::program_options::options_description desc("Usage");
    desc.add_options()(
        HELP_ARG.c_str(),
        boost::program_options::value<std::string>()->implicit_value(EMPTY_STRING),
        HELP_DESC.c_str())(
        VERBOSE_ARG.c_str(),
        boost::program_options::value<std::string>()
            ->default_value(VERBOSE_LEVEL_INFO)
            ->implicit_value(VERBOSE_LEVEL_DEBUG)
            ->value_name(VERBOSE_ARG_VALUE_NAME),
        VERBOSE_DESC.c_str());
    return desc;
}

int main(int argc, char *argv[])
{
    boost::program_options::variables_map args;
    boost::program_options::options_description optionsDescription = CreateCommandLineDescription();
    bool argsLoaded = ParseCommandLine(argc, argv, optionsDescription, args);
    if (!argsLoaded)
    {
        std::cerr << "Failed to parse command line arguments..." << std::endl;
        return EXIT_FAILURE;
    }

    // Create a cancellation token for this service.
    CancellationTokenSource::Ptr cancellationTokenSource;
    cancellationTokenSource = CancellationTokenSource::Create();
    SignalHandler::AttachToSignals(cancellationTokenSource);

    // Initialize logger.
    std::string logLevel = args[VERBOSE_ARG_VALUE_NAME].as<std::string>();
    bool loggerInitialized = InitializeLogger(logLevel);
    if (!loggerInitialized)
    {
        return EXIT_FAILURE;
    }

    LogInfo("Starting the File Upload Module...");

    // Connect to the MQTT broker.
    std::shared_ptr<MqttClient> mqttClient;
    bool mqttInitialized = ConnectToMqttBroker(mqttClient);
    if (!mqttInitialized)
    {
        Logging::Shutdown();
        return EXIT_FAILURE;
    }

    // Subscribe to the MQTT broker.
    std::shared_ptr<ModuleMessageProcessor> moduleMessageProcessor =
        std::make_unique<ModuleMessageProcessor>(mqttClient);
    SubscribeToMqttBroker(moduleMessageProcessor, mqttClient);

    std::string dataContainerPath = Configuration::GetEnvironmentConfigOrDefault(
        ConfigurationKeys::FileUploadModule::DataContainerPath,
        ModuleConstants::EnvironmentVariableNotSet);

    if (dataContainerPath == ModuleConstants::EnvironmentVariableNotSet)
    {
        LogError(
            "The %s environment variable has not been set. Unable to set data container path.",
            ConfigurationKeys::FileUploadModule::DataContainerPath.c_str());
        Logging::Shutdown();

        throw std::logic_error("Unable to set data container path.");
    }

    LogInfo("File Upload Module has started.");

    // Start module processors.
    moduleMessageProcessor->StartProcessorsAsync(cancellationTokenSource->Token(), dataContainerPath);

    // Wait for cancellation token before closing the threads.
    cancellationTokenSource->Token()->WaitForCancellation();

    LogInfo("File Upload Module has stopped.");
    Logging::Shutdown();

    return 0;
}