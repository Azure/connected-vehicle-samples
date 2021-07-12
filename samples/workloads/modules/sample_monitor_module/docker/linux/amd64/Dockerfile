# Compact version of Python
FROM python:3.8-slim-buster

# Install necessary libraries for communication with the MQTT Broker and Azure Maps
RUN pip install --upgrade pip
RUN pip install --no-cache-dir requests paho-mqtt

# Copy sources
WORKDIR /scripts
COPY *.py /scripts/

# Start application
CMD python SampleMonitor.py
