param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("QQ", "163", "Gmail")]
    [string]$Provider,
    [Parameter(Mandatory = $true)]
    [string]$Recipient,
    [Parameter(Mandatory = $true)]
    [string]$SshKeyPath,
    [string]$Server = "ubuntu@62.234.93.20"
)

$ErrorActionPreference = "Stop"
$providers = @{
    "QQ" = @{ Host = "smtp.qq.com"; Port = 465; Security = "ssl" }
    "163" = @{ Host = "smtp.163.com"; Port = 465; Security = "ssl" }
    "Gmail" = @{ Host = "smtp.gmail.com"; Port = 465; Security = "ssl" }
}

$sender = Read-Host "Sender email address"
$securePassword = Read-Host "SMTP authorization code or app password" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$plainText = $null
$payloadBytes = $null
$sshProcess = $null

try {
    $plainText = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $providerConfig = $providers[$Provider]
    $config = @{
        smtpHost = $providerConfig.Host
        smtpPort = $providerConfig.Port
        security = $providerConfig.Security
        username = $sender
        password = $plainText
        fromAddress = $sender
        recipients = @($Recipient)
    }
    $json = $config | ConvertTo-Json -Compress
    $payloadBytes = [Text.Encoding]::UTF8.GetBytes($json + "`n")

    $sshStartInfo = New-Object Diagnostics.ProcessStartInfo
    $sshStartInfo.FileName = "ssh.exe"
    $sshStartInfo.Arguments = "-i `"$SshKeyPath`" -o StrictHostKeyChecking=accept-new $Server `"sudo /opt/napcat/configure-email-alert.sh`""
    $sshStartInfo.UseShellExecute = $false
    $sshStartInfo.CreateNoWindow = $true
    $sshStartInfo.RedirectStandardInput = $true
    $sshStartInfo.RedirectStandardOutput = $true
    $sshStartInfo.RedirectStandardError = $true

    $sshProcess = New-Object Diagnostics.Process
    $sshProcess.StartInfo = $sshStartInfo
    [void]$sshProcess.Start()
    $sshProcess.StandardInput.BaseStream.Write($payloadBytes, 0, $payloadBytes.Length)
    $sshProcess.StandardInput.BaseStream.Flush()
    $sshProcess.StandardInput.BaseStream.Close()
    $sshOutput = $sshProcess.StandardOutput.ReadToEnd()
    $sshError = $sshProcess.StandardError.ReadToEnd()
    $sshProcess.WaitForExit()
    $sshExitCode = $sshProcess.ExitCode
    $sshProcess.Dispose()
    $sshProcess = $null

    if ($sshOutput) { Write-Host ($sshOutput.Trim()) }
    if ($sshExitCode -ne 0) {
        if ($sshError) { Write-Error ($sshError.Trim()) }
        throw "Server rejected the email alert configuration."
    }
}
finally {
    if ($payloadBytes) { [Array]::Clear($payloadBytes, 0, $payloadBytes.Length) }
    if ($sshProcess) { $sshProcess.Dispose() }
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    $plainText = $null
}

Write-Host "NapCat email alerts configured; a test email was sent."
