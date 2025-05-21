#!/bin/bash

set -o allexport
source ../.env.scripts
set +o allexport

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <conference-id> <call-id>"
    exit 1
fi

if [ -z "$SERVICE_HOST" ]; then
    echo "SERVICE_HOST hasn't been set"
    exit 1
fi

if [ -z "$ACCOUNT_SID" ]; then
    echo "ACCOUNT_SID hasn't been set"
    exit 1
fi

if [ -z "$AUTH_TOKEN" ]; then
    echo "AUTH_TOKEN hasn't been set"
    exit 1
fi

CONFERENCE_ID="$1"
CALL_SID="$2"

# Perform the curl command
curl -X POST "https://$SERVICE_HOST/2010-04-01/Accounts/$ACCOUNT_SID/Conferences/$CONFERENCE_ID/Participants/$CALL_SID.json" \
--data-urlencode "Hold=true" \
--data-urlencode "HoldUrl=https://beyond-immersion.com/delayed-mp3-endpoint" \
-u $ACCOUNT_SID:$AUTH_TOKEN
