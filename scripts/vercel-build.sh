#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet_dir="${repo_root}/.vercel-dotnet"
publish_dir="${repo_root}/publish/vercel-wasm"
dotnet_version="$(grep -m 1 '"version"' "${repo_root}/global.json" | sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/')"
workload_install_args=(wasm-tools --skip-manifest-update --disable-parallel --no-http-cache)

refresh_emscripten_pack_state() {
  emscripten_version="$(find "${dotnet_dir}/metadata/workloads/InstalledPacks/v1/${emscripten_pack}" -mindepth 1 -maxdepth 1 -type d -printf '%f\n' 2>/dev/null | sort -V | tail -n 1)"
  emscripten_sdk_props="${dotnet_dir}/packs/${emscripten_pack}/${emscripten_version}/Sdk/Sdk.props"
}

export DOTNET_ROOT="${dotnet_dir}"
export PATH="${DOTNET_ROOT}:${PATH}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if [[ ! -x "${dotnet_dir}/dotnet" ]] || ! "${dotnet_dir}/dotnet" --list-sdks | grep -q "^${dotnet_version//./\\.}"; then
  mkdir -p "${dotnet_dir}"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${dotnet_dir}/dotnet-install.sh"
  bash "${dotnet_dir}/dotnet-install.sh" --version "${dotnet_version}" --install-dir "${dotnet_dir}" --no-path
fi

if ! dotnet workload list | grep -q "^wasm-tools[[:space:]]"; then
  dotnet workload install "${workload_install_args[@]}"
fi

dotnet_sdk_version="$(dotnet --version)"
IFS=. read -r dotnet_major dotnet_minor dotnet_patch <<< "${dotnet_sdk_version}"
dotnet_feature_band="${dotnet_major}.${dotnet_minor}.$(((dotnet_patch / 100) * 100))"
dotnet_arch="$(dotnet --info | awk -F: '/Architecture:/ { gsub(/^[[:space:]]+/, "", $2); print $2; exit }')"
emscripten_pack="Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.linux-${dotnet_arch}"
refresh_emscripten_pack_state

# Vercel can restore a cache where workload markers exist but extracted pack files do not.
if [[ -z "${emscripten_version}" || ! -f "${emscripten_sdk_props}" ]]; then
  echo "Vercel .NET workload cache is missing ${emscripten_pack}/Sdk.props; repairing wasm-tools."
  dotnet workload repair --disable-parallel --no-http-cache
  refresh_emscripten_pack_state
fi

if [[ -z "${emscripten_version}" || ! -f "${emscripten_sdk_props}" ]]; then
  rm -rf \
    "${dotnet_dir}/packs/${emscripten_pack}" \
    "${dotnet_dir}/metadata/workloads/InstalledPacks/v1/${emscripten_pack}" \
    "${dotnet_dir}/metadata/workloads/${dotnet_feature_band}/InstalledWorkloads/wasm-tools"

  dotnet workload install "${workload_install_args[@]}"
  refresh_emscripten_pack_state
fi

if [[ -z "${emscripten_version}" || ! -f "${emscripten_sdk_props}" ]]; then
  echo "Emscripten SDK workload pack is incomplete after repair: ${emscripten_pack}" >&2
  exit 1
fi

rm -rf "${publish_dir}"

dotnet publish "${repo_root}/SalmonEgg/SalmonEgg/SalmonEgg.csproj" \
  --configuration Release \
  --framework net10.0-browserwasm \
  --output "${publish_dir}" \
  -maxcpucount:1 \
  -p:BuildInParallel=false

find "${publish_dir}" -type d -name .vercel -prune -exec rm -rf {} +
