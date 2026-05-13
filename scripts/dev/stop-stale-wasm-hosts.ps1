param(
    [switch]$IncludeCurrentWorktree
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..\..")
$repoRootText = $repoRoot.Path.TrimEnd('\')

$hosts = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "dotnet.exe" -and
        $_.CommandLine -like "*WasmAppHost.dll*"
    }

if (-not $hosts) {
    Write-Host "[wasm] No running WasmAppHost processes found."
    return
}

$stopped = 0
foreach ($hostProcess in $hosts) {
    $commandLine = [string]$hostProcess.CommandLine
    $isCurrentWorktree = $commandLine.IndexOf($repoRootText, [StringComparison]::OrdinalIgnoreCase) -ge 0

    if ($isCurrentWorktree -and -not $IncludeCurrentWorktree) {
        Write-Host "[wasm] Keeping current worktree WasmAppHost PID $($hostProcess.ProcessId)."
        continue
    }

    $scope = if ($isCurrentWorktree) { "current worktree" } else { "stale" }
    Write-Host "[wasm] Stopping $scope WasmAppHost PID $($hostProcess.ProcessId)."
    Stop-Process -Id $hostProcess.ProcessId -Force -ErrorAction Stop
    $stopped++
}

Write-Host "[wasm] Stopped $stopped WasmAppHost process(es)."
