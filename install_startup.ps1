$packageName = "ProjectStarkCS"
$exePath = Join-Path $PSScriptRoot "bin\Release\net10.0\ProjectStarkCS.exe"
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupDir "ProjectStark.lnk"

Write-Host "Installing Project Stark to Startup..."
Write-Host "Source Exe: $exePath"
Write-Host "Startup Dir: $startupDir"

if (!(Test-Path $exePath)) {
    Write-Error "Release build not found! Please run 'dotnet build -c Release' first."
    exit
}

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = Split-Path $exePath -Parent
$shortcut.Description = "Project Stark - Jarvis Protocol"
$shortcut.Save()

Write-Host "Success! Project Stark will now run when you log in."
Write-Host "You can delete '$shortcutPath' to remove it from startup."
