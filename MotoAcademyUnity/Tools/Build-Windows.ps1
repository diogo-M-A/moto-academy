param([Parameter(Mandatory=$true)][string]$EditorPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $EditorPath)) { throw 'Unity.exe não encontrado no caminho informado.' }
$logPath = Join-Path $projectRoot 'build-windows.log'
$arguments = @('-batchmode','-quit','-projectPath',('"' + $projectRoot + '"'),'-executeMethod','MotoAcademy.Editor.ProjectSetup.BuildWindows','-logFile',('"' + $logPath + '"'))
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw ('Build falhou (código ' + $process.ExitCode + '). Consulte ' + $logPath) }
Write-Output (Join-Path $projectRoot 'Builds\Windows\MotoAcademy.exe')
