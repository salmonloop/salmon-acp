#!/bin/bash
set -euo pipefail

display_number="${DISPLAY_NUMBER:-99}"
screen_config="${XVFB_SCREEN:-0 1920x1080x24}"
project_path="SalmonEgg/SalmonEgg/SalmonEgg.csproj"
framework="net10.0-desktop"

if ! command -v Xvfb >/dev/null 2>&1; then
  echo "Xvfb is required but was not found in PATH."
  echo "Install it first, for example: sudo apt-get install -y xvfb"
  exit 1
fi

cleanup() {
  if [[ -n "${xvfb_pid:-}" ]] && kill -0 "${xvfb_pid}" >/dev/null 2>&1; then
    kill "${xvfb_pid}" >/dev/null 2>&1 || true
    wait "${xvfb_pid}" 2>/dev/null || true
  fi
}

trap cleanup EXIT INT TERM

if [[ -n "${DISPLAY:-}" ]]; then
  echo "DISPLAY is already set to ${DISPLAY}; reusing the existing X server."
else
  export DISPLAY=":${display_number}"
  echo "Starting Xvfb on ${DISPLAY} with screen ${screen_config}..."
  Xvfb "${DISPLAY}" -screen ${screen_config} >/tmp/salmon-egg-xvfb.log 2>&1 &
  xvfb_pid=$!
  sleep 1

  if ! kill -0 "${xvfb_pid}" >/dev/null 2>&1; then
    echo "Failed to start Xvfb. See /tmp/salmon-egg-xvfb.log for details."
    exit 1
  fi
fi

echo "Running SalmonEgg Skia Desktop on ${DISPLAY}..."
dotnet run --project "${project_path}" --framework "${framework}"
