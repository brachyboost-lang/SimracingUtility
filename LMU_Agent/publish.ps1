# Baut den LMU-Agent-Dienst als self-contained Windows-x64-Binary und legt
# das Ergebnis als ZIP im Download-Ordner der SimracingUtility-Website ab.
#
# Aufruf:  pwsh ./publish.ps1   (aus dem Ordner LMU_Agent)

$ErrorActionPreference = "Stop"

$root        = $PSScriptRoot
$serviceProj = Join-Path $root "src/LMU.Agent.Service/LMU.Agent.Service.csproj"
$publishDir  = Join-Path $root "publish"
$webDownloads = Join-Path $root "../SimracingUtility/wwwroot/downloads"
$zipPath     = Join-Path $webDownloads "LMU.Agent.Service.zip"

Write-Host "Veroeffentliche LMU.Agent.Service (win-x64, self-contained)..."
dotnet publish $serviceProj -c Release -r win-x64 --self-contained true -o $publishDir

if (-not (Test-Path $webDownloads)) {
    New-Item -ItemType Directory -Path $webDownloads | Out-Null
}

Write-Host "Packe nach $zipPath ..."
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath

Write-Host "Fertig. Der Agent steht jetzt auf der Website unter /Download bereit."
