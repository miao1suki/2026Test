param(
    [Parameter(Mandatory = $true)]
    [string]$SshKeyPath,
    [string]$Server = "ubuntu@62.234.93.20"
)

$securePassword = Read-Host "Notification QQ password" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$plainText = $null
$passwordBytes = $null
$hashBytes = $null
$passwordMd5 = $null
$md5 = $null

try {
    $plainText = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $passwordBytes = [Text.Encoding]::UTF8.GetBytes($plainText)
    $md5 = [Security.Cryptography.MD5]::Create()
    $hashBytes = $md5.ComputeHash($passwordBytes)
    $passwordMd5 = [BitConverter]::ToString($hashBytes).Replace("-", "").ToLowerInvariant()

    $sshStartInfo = New-Object Diagnostics.ProcessStartInfo
    $sshStartInfo.FileName = "ssh.exe"
    $sshStartInfo.Arguments = "-i `"$SshKeyPath`" -o StrictHostKeyChecking=accept-new $Server `"sudo /opt/napcat/configure-quick-password.sh`""
    $sshStartInfo.UseShellExecute = $false
    $sshStartInfo.CreateNoWindow = $true
    $sshStartInfo.RedirectStandardInput = $true
    $sshStartInfo.RedirectStandardOutput = $true
    $sshStartInfo.RedirectStandardError = $true
    $sshStartInfo.StandardInputEncoding = [Text.Encoding]::ASCII

    $sshProcess = New-Object Diagnostics.Process
    $sshProcess.StartInfo = $sshStartInfo
    [void]$sshProcess.Start()
    $sshProcess.StandardInput.NewLine = "`n"
    $sshProcess.StandardInput.WriteLine($passwordMd5)
    $sshProcess.StandardInput.Close()
    $sshOutput = $sshProcess.StandardOutput.ReadToEnd()
    $sshError = $sshProcess.StandardError.ReadToEnd()
    $sshProcess.WaitForExit()
    $sshExitCode = $sshProcess.ExitCode
    $sshProcess.Dispose()

    if ($sshOutput) {
        Write-Host ($sshOutput.Trim())
    }
    if ($sshExitCode -ne 0) {
        if ($sshError) {
            Write-Error ($sshError.Trim())
        }
        throw "Server rejected the fallback credential configuration."
    }
}
finally {
    if ($passwordBytes) {
        [Array]::Clear($passwordBytes, 0, $passwordBytes.Length)
    }
    if ($hashBytes) {
        [Array]::Clear($hashBytes, 0, $hashBytes.Length)
    }
    if ($md5) {
        $md5.Dispose()
    }
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    $plainText = $null
    $passwordMd5 = $null
}

Write-Host "NapCat fallback login credential configured without storing the plaintext password."
