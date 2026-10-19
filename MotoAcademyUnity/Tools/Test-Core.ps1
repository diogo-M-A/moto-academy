$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sources = @(
    (Join-Path $projectRoot 'Assets\MotoAcademy\Scripts\Core\Powertrain.cs'),
    (Join-Path $projectRoot 'Assets\MotoAcademy\Scripts\Core\SessionScore.cs'),
    (Join-Path $projectRoot 'Tests\CoreTests.cs')
)
Add-Type -Path $sources
$results = [CoreTests]::Run()
$results
Write-Output ('Total: {0} verificações aprovadas.' -f $results.Count)
