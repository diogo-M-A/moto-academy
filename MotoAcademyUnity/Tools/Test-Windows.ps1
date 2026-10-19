param([string]$ExecutablePath, [switch]$Visible, [switch]$Manual)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $ExecutablePath) { $ExecutablePath = Join-Path $projectRoot 'Builds\Windows\MotoAcademy.exe' }
if (-not (Test-Path -LiteralPath $ExecutablePath)) { throw 'Gere o executável Windows antes de executar este teste.' }
$resultRoot = Join-Path $projectRoot ('TestResults\Windows-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $resultRoot -Force | Out-Null
$logPath = Join-Path $resultRoot 'player.log'
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-academy-smoke-test','-academy-test-output',('"' + $resultRoot + '"'),'-logFile',('"' + $logPath + '"'))
if ($Manual) { $arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"' + $logPath + '"')) }
$windowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $ExecutablePath -ArgumentList $arguments -WindowStyle $windowStyle -PassThru
if (-not $process.WaitForExit(240000)) {
    Stop-Process -Id $process.Id
    throw ('O teste excedeu 4 minutos. Consulte ' + $logPath)
}
if ($Manual) {
    if ($process.ExitCode -ne 0) { throw ('O player encerrou com código ' + $process.ExitCode + '. Consulte ' + $logPath) }
    Write-Output ('Sessão visual encerrada sem falhas. Log: ' + $logPath)
    return
}
$reportPath = Join-Path $resultRoot 'runtime-report.json'
if (-not (Test-Path -LiteralPath $reportPath)) { throw ('O player não produziu relatório. Consulte ' + $logPath) }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report.checks
Write-Output ('Relatório: ' + $reportPath)
if ($process.ExitCode -ne 0 -or $report.failed -gt 0 -or $report.runtimeErrors.Count -gt 0) {
    throw ('A integração encontrou falhas (saída do player: ' + $process.ExitCode + '). Consulte ' + $reportPath)
}
Write-Output ('Player Windows: {0} verificações aprovadas.' -f $report.passed)
