# =============================================================================
# pre_install_safety.ps1
#
# Run this BEFORE installing the Virtual Audio Driver.
# Must be run as Administrator (right-click -> Run with PowerShell as Admin).
# =============================================================================

Write-Host "=== MultiOutput Audio - Pre-Install Safety Script ===" -ForegroundColor Cyan
Write-Host ""

# -----------------------------------------------------------------------
# 1. Make sure System Restore is enabled on the system drive
# -----------------------------------------------------------------------
Write-Host "[1/3] Checking System Restore status on C:..." -ForegroundColor Yellow
try {
    Enable-ComputerRestore -Drive "C:\"
    Write-Host "      System Restore is enabled." -ForegroundColor Green
} catch {
    Write-Host "      Could not enable System Restore: $($_.Exception.Message)" -ForegroundColor Red
}

# -----------------------------------------------------------------------
# 2. Create a restore point
# -----------------------------------------------------------------------
Write-Host ""
Write-Host "[2/3] Creating restore point 'BeforeVirtualAudioDriver'..." -ForegroundColor Yellow
try {
    Checkpoint-Computer -Description "BeforeVirtualAudioDriver" -RestorePointType "DEVICE_DRIVER_INSTALL"
    Write-Host "      Restore point created." -ForegroundColor Green
} catch {
    Write-Host "      Failed to create restore point: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "      (Windows limits restore points to one per 24h by default - if one" -ForegroundColor DarkYellow
    Write-Host "       already exists from today, that's fine, it can still be used.)" -ForegroundColor DarkYellow
}

# -----------------------------------------------------------------------
# 3. Snapshot current audio devices to a text file for reference
# -----------------------------------------------------------------------
Write-Host ""
Write-Host "[3/3] Saving current audio device list..." -ForegroundColor Yellow

$outFile = "$env:USERPROFILE\Desktop\audio_devices_before.txt"

"=== Audio devices BEFORE driver install ===" | Out-File $outFile
"Captured: $(Get-Date)" | Out-File $outFile -Append
"" | Out-File $outFile -Append

Get-PnpDevice -Class "AudioEndpoint","Media" |
    Where-Object { $_.Status -ne $null } |
    Select-Object FriendlyName, Status, InstanceId |
    Format-Table -AutoSize |
    Out-File $outFile -Append

Write-Host "      Saved to: $outFile" -ForegroundColor Green
Write-Host ""
Write-Host "=== Done. Safe to install the Virtual Audio Driver now. ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If anything goes wrong afterwards, you have two options:" -ForegroundColor White
Write-Host "  A) Run rollback_virtual_audio_driver.ps1 (removes the driver only)" -ForegroundColor White
Write-Host "  B) Use System Restore -> pick 'BeforeVirtualAudioDriver' (full revert)" -ForegroundColor White