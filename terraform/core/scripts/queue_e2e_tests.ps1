<#
.SYNOPSIS
    Queues an E2E test pipeline run against an Atlas environment that has just been deployed.

.DESCRIPTION
    Run by a release job after the Atlas function apps are deployed (see README_Deployment.md). It:
      1. waits MinimumWaitSeconds, then until every health check URL returns "Healthy" (on timeout, it warns and
         carries on: the E2E run's own health checks will then fail and report it);
      2. cancels the E2E pipeline's runs that are queued or in progress: the deploy has restarted the functions under
         them, and two runs against one Atlas delete each other's test donors;
      3. queues a run of the E2E pipeline, requested for RequestedForId so that person gets ADO's "Build completes"
         email, and tags it so the run can be matched to the deploy.

    Uses the job's OAuth token ($env:SYSTEM_ACCESSTOKEN, "Allow scripts to access the OAuth token"), and the
    organisation and project of the release ($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI, $env:SYSTEM_TEAMPROJECT). The identity of that
    token needs View builds, Queue builds, Stop builds and Update build information on the E2E pipeline.

    Works in Windows PowerShell 5.1 and PowerShell 7+.

.PARAMETER PipelineId
    Id of the E2E test pipeline (build definition) to queue.

.PARAMETER PipelineBranch
    Branch of the E2E test repository to run.

.PARAMETER HealthCheckUrls
    Comma-separated URLs that must return 200 with "Healthy" in the body before the run is queued.

.PARAMETER RequestedForId
    Id of the person the run is queued for, e.g. $(Release.Deployment.RequestedForId). Leave empty to queue it for
    the token's identity (then nobody gets the "Build completes" email).

.PARAMETER Tags
    Comma-separated tags to add to the run, e.g. the deployed build number and the release name.

.PARAMETER Commit
    Commit of the deployed build, e.g. $(Build.SourceVersion). Added as the tag "atlas-commit-<first 8 characters>".

.PARAMETER DryRun
    Waits for the health checks and lists the runs, but does not cancel, queue or tag anything.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][int]$PipelineId,
    [string]$PipelineBranch = "refs/heads/main",
    [string]$HealthCheckUrls = "",
    [int]$MinimumWaitSeconds = 120,
    [int]$HealthCheckTimeoutSeconds = 900,
    [int]$HealthCheckIntervalSeconds = 15,
    [string]$RequestedForId = "",
    [string]$Tags = "",
    [string]$Commit = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
# Windows PowerShell 5.1 may default to TLS 1.0
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$apiVersion = "7.1"

function Split-List([string]$list) {
    return @($list -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}

function Get-RequiredEnvironmentVariable([string]$name, [string]$hint) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Environment variable $name is not set. $hint"
    }
    return $value
}

# In a classic release job, SYSTEM_COLLECTIONURI is the Release Management URL (https://vsrm.dev.azure.com/<org>/),
# which doesn't serve the Build API. SYSTEM_TEAMFOUNDATIONCOLLECTIONURI is the organisation URL in builds and releases.
$collectionUri = [Environment]::GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI")
if ([string]::IsNullOrWhiteSpace($collectionUri)) {
    $collectionUri = Get-RequiredEnvironmentVariable "SYSTEM_COLLECTIONURI" "It is set on pipeline agents."
}
$collectionUri = ($collectionUri -replace "://vsrm\.", "://").TrimEnd("/")
$project = Get-RequiredEnvironmentVariable "SYSTEM_TEAMPROJECT" "It is set on pipeline agents."
$accessToken = Get-RequiredEnvironmentVariable "SYSTEM_ACCESSTOKEN" "Tick 'Allow scripts to access the OAuth token' on the agent job."
$projectUri = "$collectionUri/$([Uri]::EscapeDataString($project))"

function Invoke-AdoRest {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Query = "",
        $Body = $null
    )
    $uri = "$projectUri/_apis/$Path`?api-version=$apiVersion"
    if ($Query) { $uri += "&$Query" }

    $request = @{
        Method      = $Method
        Uri         = $uri
        Headers     = @{ Authorization = "Bearer $accessToken" }
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        # A one-element array must stay an array (the tags call), which -InputObject keeps in 5.1
        $request.Body = ConvertTo-Json -InputObject $Body -Depth 10 -Compress
    }

    try {
        return Invoke-RestMethod @request
    }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { "no response" }
        throw "$Method $uri failed ($status): $($_.ErrorDetails.Message) $($_.Exception.Message)"
    }
}

function Get-HealthProblem([string]$url) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 60
        if ($response.Content -match "Healthy" -and $response.Content -notmatch "Unhealthy") {
            return $null
        }
        return "returned $($response.StatusCode): $($response.Content)"
    }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { "no response" }
        return "failed ($status): $($_.Exception.Message)"
    }
}

function Wait-ForHealthyApps([string[]]$urls) {
    if ($MinimumWaitSeconds -gt 0) {
        Write-Host "Waiting $MinimumWaitSeconds s for the function apps to start after the deploy"
        Start-Sleep -Seconds $MinimumWaitSeconds
    }
    if ($urls.Count -eq 0) {
        return
    }

    $deadline = (Get-Date).AddSeconds($HealthCheckTimeoutSeconds)
    $pending = [System.Collections.Generic.List[string]]::new()
    $urls | ForEach-Object { $pending.Add($_) }
    $problems = @{}

    while ($true) {
        foreach ($url in @($pending)) {
            $problem = Get-HealthProblem $url
            if ($null -eq $problem) {
                Write-Host "Healthy: $url"
                [void]$pending.Remove($url)
            }
            else {
                $problems[$url] = $problem
            }
        }
        if ($pending.Count -eq 0) {
            Write-Host "All $($urls.Count) health checks are healthy"
            return
        }
        if ((Get-Date) -ge $deadline) {
            foreach ($url in $pending) {
                Write-Host "##vso[task.logissue type=warning]Not healthy after $HealthCheckTimeoutSeconds s, queuing the E2E tests anyway: $url $($problems[$url])"
            }
            return
        }
        Write-Host "Waiting for $($pending.Count) health check(s): $($pending -join ', ')"
        Start-Sleep -Seconds $HealthCheckIntervalSeconds
    }
}

function Stop-ActiveRuns {
    $active = Invoke-AdoRest -Method GET -Path "build/builds" -Query "definitions=$PipelineId&statusFilter=inProgress,notStarted,postponed"
    if ($active.count -eq 0) {
        Write-Host "No queued or running E2E runs to cancel"
        return
    }
    foreach ($run in $active.value) {
        $description = "run $($run.buildNumber) (id $($run.id), $($run.status), reason $($run.reason), requested for $($run.requestedFor.displayName))"
        if ($DryRun) {
            Write-Host "[DryRun] Would cancel $description"
            continue
        }
        Write-Host "##vso[task.logissue type=warning]Cancelling E2E ${description}: this deploy restarted the functions under it"
        [void](Invoke-AdoRest -Method PATCH -Path "build/builds/$($run.id)" -Body @{ status = "cancelling" })
    }
}

function Start-Run([string[]]$runTags) {
    $body = @{
        definition   = @{ id = $PipelineId }
        sourceBranch = $PipelineBranch
    }
    if ($RequestedForId) {
        $body.requestedFor = @{ id = $RequestedForId }
    }

    if ($DryRun) {
        Write-Host "[DryRun] Would queue: POST build/builds $(ConvertTo-Json -InputObject $body -Depth 10 -Compress)"
        Write-Host "[DryRun] Would tag it: $(ConvertTo-Json -InputObject $runTags -Compress)"
        return
    }

    $run = Invoke-AdoRest -Method POST -Path "build/builds" -Body $body
    Write-Host "Queued E2E run $($run.buildNumber) (id $($run.id)), requested for $($run.requestedFor.displayName): $($run._links.web.href)"
    if ($RequestedForId -and $run.requestedFor.id -ne $RequestedForId) {
        Write-Host "##vso[task.logissue type=warning]The run is requested for $($run.requestedFor.displayName), not $RequestedForId, so the deployer won't get its 'Build completes' email"
    }

    if ($runTags.Count -gt 0) {
        [void](Invoke-AdoRest -Method POST -Path "build/builds/$($run.id)/tags" -Body $runTags)
        Write-Host "Tagged it: $($runTags -join ', ')"
    }
}

# @() keeps a single tag an array: a function returning a one-element array unrolls it to the element
$runTags = @(Split-List $Tags)
if ($Commit) {
    $runTags += "atlas-commit-$($Commit.Substring(0, [Math]::Min(8, $Commit.Length)))"
}

Wait-ForHealthyApps (Split-List $HealthCheckUrls)
Stop-ActiveRuns
Start-Run $runTags
