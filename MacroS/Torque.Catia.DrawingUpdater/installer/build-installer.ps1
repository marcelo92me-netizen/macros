$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\Torque.Catia.DrawingUpdater.csproj'
$stage = Join-Path $PSScriptRoot ('stage_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
$payload = Join-Path $stage 'payload'
$package = Join-Path $stage 'package'
$output = Join-Path $PSScriptRoot 'TorqueDrawingUpdaterSetup.exe'
New-Item -ItemType Directory -Path $package -Force | Out-Null
dotnet publish $project -c Release -o $payload
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou.' }
Copy-Item (Join-Path $PSScriptRoot 'uninstall.ps1') $payload
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath (Join-Path $package 'payload.zip') -CompressionLevel Optimal
Copy-Item (Join-Path $PSScriptRoot 'install.ps1') $package
$sourceFolder = $package.TrimEnd('\') + '\'
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$output
FriendlyName=Torque Drawing Updater
AppLaunched=powershell.exe -NoProfile -ExecutionPolicy Bypass -File install.ps1
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="install.ps1"
FILE1="payload.zip"
[SourceFiles]
SourceFiles0=$sourceFolder
[SourceFiles0]
%FILE0%=
%FILE1%=
"@
$sedPath = Join-Path $stage 'installer.sed'
Set-Content -LiteralPath $sedPath -Value $sed -Encoding Default
& (Join-Path $env:WINDIR 'System32\iexpress.exe') /N /Q $sedPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $output)) { throw 'IExpress não conseguiu gerar o Setup.exe.' }
Write-Output $output
