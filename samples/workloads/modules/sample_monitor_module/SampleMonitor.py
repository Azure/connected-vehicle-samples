import paho.mqtt.client as mqtt 
import json
import time

# Host name of the local mosquitto broker
mosquitto_host = "mosquittomodule.azure-iot-edge"

# Port of the local mosquitto broker
mosquitto_port = 1883


# Connect to the mosquitto broker
def connect(clientName):
    client = mqtt.Client(clientName)
    try:
        client.connect(mosquitto_host, mosquitto_port)
    except:
        print ("Failed to connect to {}:{}, trying localhost".format(mosquitto_host, mosquitto_port))
        client.connect("localhost")
    return client    


# Reacts to a published message
def on_message(client, userdata, message):
    print("Received Telemetry Message")
    calculateScore(message)

# Calculates a basic score based on the message
def calculateScore(message):
    print(message)
    # global ecoScoreList
    # client.publish("tripreportmodule/public/tripscoreliveupdate", ecoScore)
  


# Starts the application, connects to the mosquitto module

print ("Starting Sample Monitoring Module... ")
client = connect("sample-monitoring-module") 

# Registers on_message as a callback that will be invoked when telemetry is received
client.on_message=on_message
client.loop_start()

# Subscribes to the telemetry message from the telemetry and command daemon
client.subscribe("vehicleabstractionmodule/public/telemetry")

# Create loop
while True:
    time.sleep(5)

client.loop_stop()