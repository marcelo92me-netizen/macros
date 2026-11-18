$ErrorActionPreference = 'SilentlyContinue'
$installPath = Join-Path $env:LOCALAPPDATA 'Torque\Atualizador de Drawings'
$shortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Torque - Atualizador de Drawings.lnk'
Remove-Item -LiteralPath $shortcut -Force
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TorqueDrawingUpdater' -Recurse -Force
Remove-Item -LiteralPath $installPath -Recurse -Force
