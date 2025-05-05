#!/bin/bash

SERVICE_PORT=80
PROJECT_NAME="my_fetcher_project"
NETWORK_NAME="${PROJECT_NAME}_default"

# Check for an optional argument
if [ -z "$1" ]; then
    TEST_PATH="/run-tests"
else
    TEST_PATH="/run-test/$1"
fi

docker run --rm --network "${NETWORK_NAME}" curlimages/curl:latest \
  curl -v -k --trace /dev/stdout "http://fetcher-tester:${SERVICE_PORT}${TEST_PATH}"

if [ $? -eq 0 ]; then
    echo "Curl command succeeded."
else
    echo "Curl command failed."
fi
