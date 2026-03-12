param (
    [string]$path = (Join-Path -Path ([Environment]::GetFolderPath('MyDocuments')) -ChildPath "Krepysh\site"),
    [string]$repo = ""
)

if ($repo -eq "") {
    Write-Error "Repository URL must be provided."
    exit 1
}

Set-Location $path

if (Test-Path ".git") {
    Remove-Item -Recurse -Force ".git"
}

git init
git remote add origin $repo

if (Test-Path ".nojekyll") {
    Remove-Item -Force ".nojekyll"
}

New-Item -ItemType File -Path ".\.nojekyll" -Force | Out-Null

git checkout -b gh-pages 
git add .
git commit -m "Auto-commit"
git push -f origin gh-pages
