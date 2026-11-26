# =============================================================================
# rollback_virtual_audio_driver.ps1
#
# Run this ONLY IF something goes wrong after installing the Virtual Audio Driver.
# Must be run as Administrator.
#
# What it does:
#   1. Finds the "Virtual Audio Driver" / "Virtual Mic Driver" devices
#   2. Disables and removes the device(s)
#   3. Deletes the underlying driver package from the driver store
#   4. Tells you whether a reboot is needed
#
# If this script itself fails or your audio is still broken afterwards,
# use System Restore -> 'BeforeVirtualAudioDriver' restore point instead.
# This is the guaranteed full revert.
# =============================================================================

Write-Host "=== MultiOutput Audio - Virtual Audio Driver Rollback ===" -ForegroundColor Cyan
Write-Host ""

# -----------------------------------------------------------------------
# 1. Find matching devices
# -----------------------------------------------------------------------
Write-Host "[1/3] Searching for Virtual Audio Driver devices..." -ForegroundColor Yellow

$targets = Get-PnpDevice | Where-Object {
    $_.FriendlyName -like "*Virtual Audio Driver*" -or
    $_.FriendlyName -like "*Virtual Mic Driver*" -or
    $_.FriendlyName -like "*VirtualAudio*"
}

if (-not $targets) {
    Write-Host "      No matching devices found. Driver may already be removed," -ForegroundColor Green
    Write-Host "      or it wasn't installed via Device Manager under that name." -ForegroundColor Green
} else {
    foreach ($dev in $targets) {
        Write-Host "      Found: $($dev.FriendlyName)  [$($dev.InstanceId)]" -ForegroundColor White
    }
}

# -----------------------------------------------------------------------
# 2. Disable + remove each device
# -----------------------------------------------------------------------
Write-Host ""
Write-Host "[2/3] Removing device(s)..." -ForegroundColor Yellow

foreach ($dev in $targets) {
    try {
        Write-Host "      Disabling $($dev.FriendlyName)..." -ForegroundColor White
        Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction Stop

        Write-Host "      Removing $($dev.FriendlyName)..." -ForegroundColor White
        # pnputil /remove-device requires Windows 11 22H2+ or Win10 with recent updates
        pnputil /remove-device "$($dev.InstanceId)" | Out-Null

        Write-Host "      Done." -ForegroundColor Green
    } catch {
        Write-Host "      Could not remove via PowerShell: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "      -> Manually remove via Device Manager:" -ForegroundColor DarkYellow
        Write-Host "         devmgmt.msc -> find '$($dev.FriendlyName)' -> right-click -> Uninstall device" -ForegroundColor DarkYellow
        Write-Host "         -> CHECK 'Delete the driver software for this device' if offered" -ForegroundColor DarkYellow
    }
}

# -----------------------------------------------------------------------
# 3. Find and delete the orphaned driver package (.inf) from driver store
# -----------------------------------------------------------------------
Write-Host ""
Write-Host "[3/3] Searching driver store for leftover driver packages..." -ForegroundColor Yellow

$drivers = pnputil /enum-drivers | Select-String -Pattern "VirtualAudio|Virtual Audio" -Context 3,0

if ($drivers) {
    Write-Host "      Found possible matches in driver store. Published names below -" -ForegroundColor White
    Write-Host "      to delete one, run:  pnputil /delete-driver oemXX.inf /uninstall" -ForegroundColor White
    Write-Host ""
    $drivers | ForEach-Object { Write-Host $_.Context.PreContext[0] -ForegroundColor DarkGray }
} else {
    Write-Host "      No leftover driver packages found (or already cleaned up)." -ForegroundColor Green
}

# -----------------------------------------------------------------------
# Done
# -----------------------------------------------------------------------
Write-Host ""
Write-Host "=== Rollback steps complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. RESTART your computer (driver removal needs a reboot to fully clear)." -ForegroundColor White
Write-Host "  2. After reboot, open Sound Settings and confirm your normal" -ForegroundColor White
Write-Host "     output device (headphones/speakers) is back as default." -ForegroundColor White
Write-Host ""
Write-Host "  If audio is STILL broken after reboot:" -ForegroundColor Red
Write-Host "    - Open 'Create a restore point' (search Start menu)" -ForegroundColor Red
Write-Host "    - Click 'System Restore...'" -ForegroundColor Red
Write-Host "    - Choose 'BeforeVirtualAudioDriver'" -ForegroundColor Red
Write-Host "    - This fully reverts your system to before the driver was installed." -ForegroundColor Red