#!/bin/bash

if [ -z "$1" ]; then
  echo "Usage: $0 <domain>"
  exit 1
fi

DOMAIN="$1"
CERT_DIR="./certs/$DOMAIN"
DAYS_VALID=3650

mkdir -p "$CERT_DIR"

openssl genpkey -algorithm RSA -out "$CERT_DIR/$DOMAIN.key" -pkeyopt rsa_keygen_bits:2048
openssl req -new -x509 -key "$CERT_DIR/$DOMAIN.key" -out "$CERT_DIR/$DOMAIN.crt" -days $DAYS_VALID -subj "/CN=$DOMAIN"

echo "Self-signed certificate generated at $CERT_DIR/$DOMAIN.crt"
echo "Private key generated at $CERT_DIR/$DOMAIN.key"
echo "openssl pkcs12 -export -out $CERT_DIR/certificate.pfx -inkey $CERT_DIR/$DOMAIN.key -in $CERT_DIR/$DOMAIN.crt -passout pass:"
