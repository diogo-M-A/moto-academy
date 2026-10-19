param([string]$EditorPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $EditorPath) {
    $editorRoots = @('C:\Program Files\Unity\Hub\Editor', 'C:\Program Files\Unity Hub\Editor', (Join-Path $env:LOCALAPPDATA 'Programs\Unity'))
    $editors = foreach ($editorRoot in $editorRoots) {
        if (Test-Path -LiteralPath $editorRoot) {
            Get-ChildItem -LiteralPath $editorRoot -Directory | Where-Object Name -Like '6000.3.*' |
                ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' } | Where-Object { Test-Path -LiteralPath $_ }
        }
    }
    $EditorPath = $editors | Sort-Object -Descending | Select-Object -First 1
}
if (-not $EditorPath -or -not (Test-Path -LiteralPath $EditorPath)) {
    throw 'Unity 6.3 não encontrado. Instale pelo Unity Hub ou informe -EditorPath com o caminho de Unity.exe.'
}
Start-Process -FilePath $EditorPath -ArgumentList @('-projectPath', ('"' + $projectRoot + '"')) -WindowStyle Hidden
