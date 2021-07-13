import paho.mqtt.client as mqtt 
import json
import time
import os
from random import *

# Host name of the local mosquitto broker is read from the environment variable MqttBrokerAddress
mosquitto_host = os.environ.get("MqttBrokerAddress", "localhost")

# Port of the  mosquitto broker.
mosquitto_port = 1883


# Connects to the mosquitto broker
def connect(clientName):
    client = mqtt.Client(clientName)
    try:
        client.connect(mosquitto_host, mosquitto_port)
    except:
        raise ("Failed to connect to {}:{}. Check ENV variable MqttBrokerAddress".format(mosquitto_host, mosquitto_port))        
    return client    


# Reacts to a published message
def on_message(client, userdata, msg):
    print(f"Received Message topic {msg.topic} -> {msg.payload.decode()}")
    calculateScore(msg)

# Calculates a basic score based on the message
def calculateScore(msg):
    # ... code to calculate score goes here...
    score = randint(1, 100)
    # .....

    print(f"Posting updated score {score}")
    
    # Sends the calculated score
    scoreMsg = {"score":score}
    scoreMsgString = json.dumps(scoreMsg)
    scoreTopic = "samplemonitormodule/public/scoreupdate"
    print(f"Publishing updated score {scoreTopic} -> {scoreMsgString}")
    client.publish(scoreTopic, scoreMsgString)
  

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
