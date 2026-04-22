param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $SkipMsixRefresh,

    [switch] $IncludeRealUser
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tests\SalmonEgg.GuiTests.Windows\SalmonEgg.GuiTests.Windows.csproj'

$env:SALMONEGG_GUI = '1'

if (-not $SkipMsixRefresh)
{
    Write-Host "Refreshing MSIX install before session GUI regression..."
    & (Join-Path $repoRoot '.tools\run-winui3-msix.ps1') -Configuration $Configuration
}

$filters = @(
    'FullyQualifiedName~ChatSkeletonSmokeTests.SelectRemoteSessionWithSlowReplay_AutoScrollsToLatestMessageAfterHydration'
    'FullyQualifiedName~ChatSkeletonSmokeTests.HydratedRemoteSession_NavigateToDiscoverAndBack_ReturnsHotWithoutRemoteReload'
    'FullyQualifiedName~ChatSkeletonSmokeTests.HydratedRemoteSession_SwitchToOtherRemoteSessionAndBack_ReturnsHotWithoutRemoteReload'
    'FullyQualifiedName~ChatSkeletonSmokeTests.BackgroundRemoteSession_LiveAgentUpdate_ShowsUnreadAndClearsWhenActivated'
    'FullyQualifiedName~ChatSkeletonSmokeTests.SelectSessionWithMarkdownMessages_DoubleClickCodeBlock_DoesNotCrash'
    'FullyQualifiedName~ChatSkeletonSmokeTests.MarkdownSession_AfterDiscoverRoundTrip_RetainsRenderedCodeAndDoesNotCrash'
    'FullyQualifiedName~ChatSkeletonSmokeTests.MarkdownSession_AfterAcpSettingsRoundTrip_RetainsRenderedCodeAndDoesNotCrash'
)

if ($IncludeRealUser)
{
    $filters += @(
        'FullyQualifiedName~RealUserConfigSmokeTests.SelectRemoteBoundSession_AfterDiscoverRoundTrip_ReturnsWithoutStuckReload'
        'FullyQualifiedName~RealUserConfigSmokeTests.SelectRemoteBoundSession_AfterAcpSettingsRoundTrip_ReturnsWithoutCrash'
    )
}

$filter = [string]::Join('|', $filters)

Write-Host "Running session GUI regression suite..."
if ($IncludeRealUser)
{
    Write-Host "Including real-user ACP round-trip probes."
}

& dotnet test $project -c $Configuration --no-restore -m:1 --filter $filter
