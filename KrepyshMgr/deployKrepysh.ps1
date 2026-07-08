param (
    [string]$path = (Join-Path -Path ([Environment]::GetFolderPath('MyDocuments')) -ChildPath "Krepysh\site"),
    [string]$name = ""
    [string]$address = "https://pbgrpfrm-8000.euw.devtunnels.ms"
)

Set-Location $path

$BaseName = (Get-Item .).Name
if (!($name -eq "")) {
    $BaseName = $name
}
Compress-Archive -Path ".\*" -DestinationPath ".\$BaseName.zip" -Force

curl.exe -F "file=@$BaseName.zip" $address
Write-Host "file=@$BaseName.zip"
Write-Host "success"
