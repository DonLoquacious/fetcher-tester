#!/bin/bash

set -o allexport
source ../.env.scripts
set +o allexport

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

if [ -z "$FROM_NUMBER" ]; then
    echo "FROM_NUMBER hasn't been set"
    exit 1
fi

if [ -z "$TO_NUMBER" ]; then
    echo "TO_NUMBER hasn't been set"
    exit 1
fi

SERVICE_HOST=$(echo -n "$SERVICE_HOST" | sed 's/\r//g')
ACCOUNT_SID=$(echo -n "$ACCOUNT_SID" | sed 's/\r//g')
AUTH_TOKEN=$(echo -n "$AUTH_TOKEN" | sed 's/\r//g')
FROM_NUMBER=$(echo -n "$FROM_NUMBER" | sed 's/\r//g')
TO_NUMBER=$(echo -n "$TO_NUMBER" | sed 's/\r//g')

url="https://$SERVICE_HOST/api/laml/2010-04-01/Accounts/"
url+="$ACCOUNT_SID"
url+="/Faxes.json"

echo "URL: $url"

echo "HOST: $SERVICE_HOST"
echo "ACCOUNT: $ACCOUNT_SID"
echo "AUTH: $AUTH_TOKEN"
echo "FROM: $FROM_NUMBER"
echo "TO: $TO_NUMBER"

curl -v "$url" \
-u "$ACCOUNT_SID:$AUTH_TOKEN" -i \
-H "Content-Type: application/x-www-form-urlencoded" \
--data-urlencode "MediaUrl=https://beyond-immersion.com/media/pdf-endpoint" \
--data-urlencode "From=$FROM_NUMBER" \
--data-urlencode "To=$TO_NUMBER"
