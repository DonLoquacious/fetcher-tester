#!/bin/bash

if [ -z "$1" ]; then
  echo "Usage: $0 <domain_or_ip>"
  exit 1
fi

DOMAIN_OR_IP="$1"
CERT_DIR="./certs/$DOMAIN_OR_IP"
DAYS_VALID=3650

if [[ $DOMAIN_OR_IP =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  IP_ADDRESS=$DOMAIN_OR_IP
  SUBJECT_ALT_NAME="IP:$IP_ADDRESS"
else
  SUBJECT_ALT_NAME="DNS:$DOMAIN_OR_IP"
fi

mkdir -p "$CERT_DIR"

CONFIG_FILE="$CERT_DIR/openssl.cnf"
cat > "$CONFIG_FILE" <<EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = req_ext
x509_extensions = v3_ca

[req_distinguished_name]

[req_ext]
subjectAltName = $SUBJECT_ALT_NAME

[v3_ca]
subjectAltName = $SUBJECT_ALT_NAME
EOF

openssl genpkey -algorithm RSA -out "$CERT_DIR/$DOMAIN_OR_IP.key" -pkeyopt rsa_keygen_bits:2048
openssl req -new -x509 -key "$CERT_DIR/$DOMAIN_OR_IP.key" -out "$CERT_DIR/$DOMAIN_OR_IP.crt" -days $DAYS_VALID -subj "/CN=$DOMAIN_OR_IP" -extensions req_ext -config "$CONFIG_FILE"

echo "Self-signed certificate generated at $CERT_DIR/$DOMAIN_OR_IP.crt"
echo "Private key generated at $CERT_DIR/$DOMAIN_OR_IP.key"
echo "openssl pkcs12 -export -out $CERT_DIR/certificate.pfx -inkey $CERT_DIR/$DOMAIN_OR_IP.key -in $CERT_DIR/$DOMAIN_OR_IP.crt -passout pass:"
