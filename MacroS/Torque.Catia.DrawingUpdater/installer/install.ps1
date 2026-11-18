$ErrorActionPreference = 'Stop'
$destination = Join-Path $env:LOCALAPPDATA 'Torque\Atualizador de Drawings'
$tempPayload = Join-Path $env:TEMP ('TorqueDrawingUpdater_' + [guid]::NewGuid().ToString('N'))
$runtime = (& dotnet --list-runtimes 2>$null | Out-String)
if ($LASTEXITCODE -ne 0 -or $runtime -notmatch 'Microsoft\.WindowsDesktop\.App 8\.') {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show('Este aplicativo requer o .NET Desktop Runtime 8 x64.', 'Pré-requisito ausente', 'OK', 'Warning') | Out-Null
    exit 1
}
Expand-Archive -LiteralPath (Join-Path $PSScriptRoot 'payload.zip') -DestinationPath $tempPayload -Force
if (-not (Test-Path (Join-Path $tempPayload 'Torque.Catia.DrawingUpdater.exe'))) { throw 'O pacote do aplicativo está incompleto.' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item (Join-Path $tempPayload '*') $destination -Recurse -Force
Remove-Item -LiteralPath $tempPayload -Recurse -Force
$exe = Join-Path $destination 'Torque.Catia.DrawingUpdater.exe'
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Torque - Atualizador de Drawings.lnk'
$shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $destination
$shortcut.IconLocation = "$exe,0"
$shortcut.Save()
$key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TorqueDrawingUpdater'
New-Item -Path $key -Force | Out-Null
Set-ItemProperty $key DisplayName 'Torque - Atualizador de Drawings'
Set-ItemProperty $key DisplayVersion '1.0.0'
Set-ItemProperty $key Publisher 'Torque'
Set-ItemProperty $key InstallLocation $destination
Set-ItemProperty $key DisplayIcon $exe
Set-ItemProperty $key UninstallString "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $destination 'uninstall.ps1')`""
[System.Windows.MessageBox]::Show('Instalação concluída. O atalho está na área de trabalho.', 'Torque - Atualizador de Drawings', 'OK', 'Information') | Out-Null
