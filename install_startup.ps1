$packageName = "ProjectStarkCS"
# Point to the BATCH file now, not the EXE
$batchPath = Join-Path $PSScriptRoot "launch_with_delay.bat"
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupDir "ProjectStark_Delayed.lnk"

Write-Host "Installing Project Stark (Delayed) to Startup..."
Write-Host "Source Batch: $batchPath"
Write-Host "Startup Dir: $startupDir"

if (!(Test-Path $batchPath)) {
    Write-Error "Batch file not found!"
    exit
}

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $batchPath
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.WindowStyle = 7 # 7 = Minimized / 1 = Normal
$shortcut.Description = "Project Stark - Jarvis Protocol (Delayed Start)"
$shortcut.Save()

Write-Host "Success! Project Stark will now run when you log in."
Write-Host "You can delete '$shortcutPath' to remove it from startup."
