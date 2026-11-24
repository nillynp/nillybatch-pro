# ============================================================================
# NillyBatch Pro — Clean Uninstaller (Zero Admin · HKCU)
# created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/
# ============================================================================
param([switch]$Silent)

$ErrorActionPreference = 'SilentlyContinue'
Write-Host ''
Write-Host '  Αφαίρεση εγγραφών & συντομεύσεων του NillyBatch Pro...' -ForegroundColor Yellow

Stop-Process -Name 'NillyBatchPro' -Force -ErrorAction SilentlyContinue
Remove-Item -Path 'HKCU:\Software\Classes\Directory\shell\NillyBatchPro' -Recurse -Force

$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'NillyBatch Pro.lnk'
$startLnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics\NillyBatch Pro.lnk'
if (Test-Path $desktopLnk) { Remove-Item $desktopLnk -Force }
if (Test-Path $startLnk) { Remove-Item $startLnk -Force }

Write-Host '  OK -> Αφαιρέθηκαν καθαρά όλες οι εγγραφές & συντομεύσεις του NillyBatch Pro.' -ForegroundColor Green
Write-Host ''
if (-not $Silent) { Read-Host '  Πάτησε Enter για κλείσιμο' }
