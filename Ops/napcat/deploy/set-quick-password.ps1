param(
    [Parameter(Mandatory = $true)]
    [string]$SshKeyPath,
    [string]$Server = "ubuntu@62.234.93.20"
)

$securePassword = Read-Host "Notification QQ password" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$plainText = $null
$passwordBytes = $null
$passwordMd5 = $null

try {
    $plainText = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $passwordBytes = [Text.Encoding]::UTF8.GetBytes($plainText)
    $passwordMd5 = [Convert]::ToHexString(
        [Security.Cryptography.MD5]::HashData($passwordBytes)
    ).ToLowerInvariant()

    $passwordMd5 | & ssh.exe -i $SshKeyPath -o StrictHostKeyChecking=accept-new `
        $Server "sudo /opt/napcat/configure-quick-password.sh"
    if ($LASTEXITCODE -ne 0) {
        throw "Server rejected the fallback credential configuration."
    }
}
finally {
    if ($passwordBytes) {
        [Array]::Clear($passwordBytes, 0, $passwordBytes.Length)
    }
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    $plainText = $null
    $passwordMd5 = $null
}

Write-Host "NapCat fallback login credential configured without storing the plaintext password."
