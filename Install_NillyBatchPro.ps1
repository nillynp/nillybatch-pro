# ============================================================================
# NillyBatch Pro — Universal Windows 10/11 Installer (Zero Admin · Portable)
# created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/
# Ultra-Light Batch Image Optimizer, Watermark & Greeklish SEO File Renamer
# ============================================================================
param([switch]$Silent)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $root 'NillyBatchPro.exe'
$icoPath = Join-Path $root 'NillyBatchPro.ico'
$srcPath = Join-Path $root 'desktop\NillyBatchPro.cs'

Write-Host ''
Write-Host '  ============================================================' -ForegroundColor Cyan
Write-Host '   NillyBatch Pro — Ultra-Light Batch Image & Rename Studio' -ForegroundColor White
Write-Host '   created by Nilly graphics (Sotiris Christos)' -ForegroundColor DarkGray
Write-Host '   https://nillygraphics.com/' -ForegroundColor DarkGray
Write-Host '  ============================================================' -ForegroundColor Cyan
Write-Host ''

# 1. Unblock downloaded files if extracted from GitHub Portable ZIP
Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue

# 2. Auto-compile NillyBatchPro.exe on any Windows 10/11 PC if missing or older than source
$needBuild = (-not (Test-Path $exePath)) -or ((Test-Path $srcPath) -and ((Get-Item $srcPath).LastWriteTimeUtc -gt (Get-Item $exePath).LastWriteTimeUtc))
if ($needBuild) {
    $fxDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
    if (-not (Test-Path "$fxDir\csc.exe")) { $fxDir = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319' }
    $csc = Join-Path $fxDir 'csc.exe'
    $wpf = Join-Path $fxDir 'WPF'
    if (-not (Test-Path $csc)) { throw 'Δεν βρέθηκε ο .NET Framework 4.x C# compiler (csc.exe).' }
    Write-Host '  [1/3] Μεταγλώττιση NillyBatchPro.exe (100% Native Windows Studio Host)...' -ForegroundColor Yellow
    $cscArgs = @(
        '/nologo', '/target:winexe', '/optimize+', '/utf8output', "/out:$exePath",
        '/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll',
        "/r:$wpf\PresentationCore.dll", "/r:$wpf\WindowsBase.dll", "/r:$fxDir\System.Xaml.dll"
    )
    if (Test-Path $icoPath) { $cscArgs += "/win32icon:$icoPath" }
    $cscArgs += $srcPath
    & $csc @cscArgs
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exePath)) {
        throw "Αποτυχία μεταγλώττισης NillyBatchPro.exe (exit $LASTEXITCODE)"
    }
    Write-Host ("        OK -> " + [math]::Round((Get-Item $exePath).Length / 1KB, 1) + " KB") -ForegroundColor Green
} else {
    Write-Host '  [1/3] NillyBatchPro.exe έτοιμο.' -ForegroundColor Green
}

# 3. Register Folder Right-Click Context Menu ONLY (Zero Admin · Safe!)
Write-Host '  [2/3] Εγγραφή δεξιού κλικ φακέλων «Batch Process in NillyBatch Pro»...' -ForegroundColor Yellow
$iconRef = if (Test-Path $icoPath) { "$icoPath,0" } else { "$exePath,0" }
$dirCtx = 'HKCU:\Software\Classes\Directory\shell\NillyBatchPro'
New-Item -Path "$dirCtx\command" -Force | Out-Null
Set-ItemProperty -Path $dirCtx -Name '(Default)' -Value 'Batch Process in NillyBatch Pro'
Set-ItemProperty -Path $dirCtx -Name 'Icon' -Value $iconRef
Set-ItemProperty -Path "$dirCtx\command" -Name '(Default)' -Value "`"$exePath`" `"%V`""
Write-Host '        OK -> Έτοιμο το δεξί κλικ σε φακέλους!' -ForegroundColor Green

# 4. Desktop & Start Menu Shortcuts
Write-Host '  [3/3] Δημιουργία συντομεύσεων (Επιφάνεια Εργασίας & Start Menu)...' -ForegroundColor Yellow
$wsh = New-Object -ComObject WScript.Shell
$desktopDir = [Environment]::GetFolderPath('Desktop')
$startMenuDir = Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics'
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null

foreach ($lnkPath in @(
    (Join-Path $desktopDir 'NillyBatch Pro.lnk'),
    (Join-Path $startMenuDir 'NillyBatch Pro.lnk')
)) {
    $sc = $wsh.CreateShortcut($lnkPath)
    $sc.TargetPath = $exePath
    $sc.WorkingDirectory = $root
    $sc.Description = 'NillyBatch Pro — Ultra-Light Batch Image Optimizer & Greeklish SEO Renamer | created by Nilly graphics'
    if (Test-Path $icoPath) { $sc.IconLocation = "$icoPath,0" }
    $sc.Save()
}
Write-Host '        OK -> Έτοιμες οι συντομεύσεις!' -ForegroundColor Green
Write-Host ''
Write-Host '  ΟΛΟΚΛΗΡΩΘΗΚΕ! Άνοιξε το NillyBatch Pro από την Επιφάνεια Εργασίας ή με δεξί κλικ σε φάκελο.' -ForegroundColor Cyan
Write-Host ''

if (-not $Silent) {
    Start-Process -FilePath $exePath
}
