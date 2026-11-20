<#
.SYNOPSIS
    Tags the test runs a job of the Integration Tests stage has published, one per test category, with the area of the category,
    for the Tags filter of the Tests tab of the build.

.DESCRIPTION
    The job lists the runs in PublishedRunsFile, a line per run: the time it was published, its title and its tag, separated by tabs.
    The agent creates a run some time after the publish command, so the runs are read again until each of them is there;
    a run of an earlier attempt of the job has the same title, so only runs created since the publish count.
    The publish command cannot tag a run, so the tags are added through the REST API with the token of the job (SYSTEM_ACCESSTOKEN).
    A run left without its tag fails the step.
#>
param(
    [Parameter(Mandatory)] [string] $CollectionUri,
    [Parameter(Mandatory)] [string] $Project,
    [Parameter(Mandatory)] [int] $BuildId,
    [Parameter(Mandatory)] [string] $PublishedRunsFile,
    [int] $WaitMinutes = 5
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $PublishedRunsFile)) {
    Write-Host 'No test runs published'
    return
}
# Title -> the time it was published, and its tag.
$publishedAt = @{}
$tagOf = @{}
foreach ($line in Get-Content $PublishedRunsFile) {
    $time, $title, $tag = $line -split "`t", 3
    $publishedAt[$title] = [datetime]::Parse($time, $null, 'RoundtripKind').ToUniversalTime()
    $tagOf[$title] = $tag
}

$api = "$($CollectionUri.TrimEnd('/'))/$([uri]::EscapeDataString($Project))/_apis/test/runs"
$headers = @{ Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" }

function Get-BuildRuns {
    $runs = @()
    do {
        $page = @((Invoke-RestMethod "$($api)?buildUri=vstfs:///Build/Build/$BuildId&includeRunDetails=true&`$top=100&`$skip=$($runs.Count)&api-version=7.1" -Headers $headers).value)
        $runs += $page
    } while ($page.Count -eq 100)
    $runs
}

# Only a run itself has its creation time, not the list of runs. Run id -> created.
$createdOf = @{}
# The clocks of the agent and the server differ a little.
$skew = [timespan]::FromMinutes(1)
$deadline = [datetime]::UtcNow.AddMinutes($WaitMinutes)
while ($true) {
    $runs = @(Get-BuildRuns | Where-Object { $publishedAt.ContainsKey($_.name) } | Where-Object {
        if (-not $createdOf.ContainsKey($_.id)) {
            $createdOf[$_.id] = ([datetime] (Invoke-RestMethod "$api/$($_.id)?api-version=7.1" -Headers $headers).createdDate).ToUniversalTime()
        }
        $createdOf[$_.id] -ge $publishedAt[$_.name] - $skew
    })
    $missing = @($publishedAt.Keys | Where-Object { $runs.name -notcontains $_ } | Sort-Object)
    if (-not $missing.Count -or [datetime]::UtcNow -ge $deadline) {
        break
    }
    Write-Host "Waiting for $($missing.Count) test run(s): $($missing -join ', ')"
    Start-Sleep -Seconds 15
}

foreach ($run in $runs) {
    $tag = $tagOf[$run.name]
    $body = @{ tags = @(@{ name = $tag }) } | ConvertTo-Json -Depth 3
    Invoke-RestMethod "$api/$($run.id)?api-version=7.1" -Method Patch -Headers $headers -ContentType 'application/json' -Body $body | Out-Null
    Write-Host "$($run.name): $tag"
}

if ($missing.Count) {
    throw "Not found in $WaitMinutes min, left without tags: $($missing -join ', ')"
}
