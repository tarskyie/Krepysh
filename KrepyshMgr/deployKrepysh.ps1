param (
    [string]$path = (Join-Path -Path ([Environment]::GetFolderPath('MyDocuments')) -ChildPath "Krepysh\site"),
    [string]$name = ""
)

Set-Location $path

$BaseName = (Get-Item .).Name
if (!($name -eq "")) {
    $BaseName = $name
}
Compress-Archive -Path ".\*" -DestinationPath ".\$BaseName.zip" -Force

curl.exe -F "file=@$BaseName.zip" https://w29dq7t4-80.euw.devtunnels.ms/upload
Write-Host "file=@$BaseName.zip"
Write-Host "success"
