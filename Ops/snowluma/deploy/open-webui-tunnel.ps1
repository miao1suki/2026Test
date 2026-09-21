param(
    [string]$Server = "ubuntu@62.234.93.20",
    [string]$SshKeyPath
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($SshKeyPath)) {
    $keyCandidates = @(Get-ChildItem -Path "D:\.edge*\key1.pem" -File -ErrorAction SilentlyContinue)
    if ($keyCandidates.Count -ne 1) { throw "Expected exactly one D:\.edge*\key1.pem SSH key; found $($keyCandidates.Count)." }
    $SshKeyPath = $keyCandidates[0].FullName
}
if (-not (Test-Path -LiteralPath $SshKeyPath -PathType Leaf)) { throw "SSH key was not found: $SshKeyPath" }

Write-Host "SnowLuma tunnel is starting. Keep this terminal open."
Write-Host "noVNC: http://127.0.0.1:16081/"
Write-Host "WebUI: http://127.0.0.1:15099/"
& ssh.exe -i $SshKeyPath -o BatchMode=yes -o ExitOnForwardFailure=yes `
    -L 16081:127.0.0.1:6081 -L 15099:127.0.0.1:5099 $Server -N
