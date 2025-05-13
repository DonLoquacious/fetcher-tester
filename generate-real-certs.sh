#!/bin/bash

function usage {
  echo "Usage: $0 <domain> <email>"
  echo "Ensure you have sudo privileges and Certbot installed"
  exit 1
}

if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root (sudo)" 
   exit 1
fi

if [ -z "$1" ] || [ -z "$2" ]; then
  usage
fi

DOMAIN="$1"
EMAIL="$2"
CERT_DIR="/etc/letsencrypt/live/$DOMAIN"
PFX_FILE="./certs/$DOMAIN/certificate.pfx"

if ! command -v certbot &> /dev/null; then
    echo "Certbot could not be found, please install it first."
    exit 1
fi

function check_certificate_expiry {
  local cert_file="$1"
  local current_date=$(date +%s)
  local expire_date=$(date -d "$(openssl x509 -enddate -noout -in "$cert_file" | cut -d= -f2)" +%s)
  
  if [ "$expire_date" -gt "$current_date" ]; then
    return 0
  else
    return 1
  fi
}

if [ -d "$CERT_DIR" ] && [ -f "$CERT_DIR/fullchain.pem" ] && check_certificate_expiry "$CERT_DIR/fullchain.pem"; then
  echo "Existing valid certificate for $DOMAIN found. Skipping certificate generation."
else
  certbot certonly --standalone --preferred-challenges http --agree-tos --non-interactive --email "$EMAIL" -d "$DOMAIN"
  
  if [ ! -d "$CERT_DIR" ]; then
      echo "Certificate for $DOMAIN failed to obtain."
      exit 1
  fi
fi

if [ -z "$PFX_PASSWORD" ]; then
  echo "Note: PFX_PASSWORD environment variable not set. Using empty password for PFX."
  PFX_PASSWORD=""
fi

openssl pkcs12 -export -out "$PFX_FILE" -inkey "$CERT_DIR/privkey.pem" -in "$CERT_DIR/fullchain.pem" -password pass:$PFX_PASSWORD

if [ $? -ne 0 ]; then
    echo "Failed to convert certificates to PFX format."
    exit 1
fi

echo "Certificate stored at $CERT_DIR/fullchain.pem"
echo "Private key stored at $CERT_DIR/privkey.pem"
echo "PFX file stored at $PFX_FILE"
