#!/usr/bin/env bash
# Makes the key pair that lets the app check that API replies were not changed on the way (HTTP has no other protection).
#   scripts/gen-signing-key.sh
# Put the PRIVATE value in .env as SIGNING_KEY (server only, never in Git).
# Put the PUBLIC value in the app: the ApiPublicKey setting in Hue_travel.exe.config, or %ProgramData%\CoDoGIS\public.key,
# or the HUE_API_PUBLIC_KEY environment variable. Anyone may see the public value; losing the private one means making a new pair.
set -euo pipefail
command -v openssl >/dev/null || { echo "openssl is needed" >&2; exit 1; }

tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT
openssl ecparam -name prime256v1 -genkey -noout -out "$tmp"
echo "SIGNING_KEY (private, for .env):"
openssl pkcs8 -topk8 -nocrypt -in "$tmp" -outform DER | base64 -w0
echo
echo
echo "PUBLIC KEY (for the app):"
openssl ec -in "$tmp" -pubout -outform DER 2>/dev/null | base64 -w0
echo
