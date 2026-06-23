# SkyLearn API — EF Core migrations (remote SQL Server, not localhost)
# Run from repo root or Backend folder:
#   .\Backend\run-migrations.ps1
#   .\Backend\run-migrations.ps1 -MigrationName "MyMigration"

param(
    [string]$MigrationName = ""
)

$ErrorActionPreference = "Stop"

# Ensure dotnet-ef is on PATH
$dotnetTools = Join-Path $env:USERPROFILE ".dotnet\tools"
if (Test-Path $dotnetTools) {
    $env:PATH = "$dotnetTools;" + $env:PATH
}

if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef --version 9.0.3
    $env:PATH = "$dotnetTools;" + $env:PATH
}

$projectDir = Join-Path $PSScriptRoot "SkyLearnApi"
if (-not (Test-Path (Join-Path $projectDir "SkyLearnApi.csproj"))) {
    Write-Error "SkyLearnApi project not found at $projectDir"
}

Set-Location $projectDir

Write-Host "Using database from appsettings.json (remote server, not localhost)" -ForegroundColor Cyan
Write-Host "Project: $projectDir" -ForegroundColor Cyan

if ($MigrationName) {
    Write-Host "Adding migration: $MigrationName" -ForegroundColor Green
    dotnet ef migrations add $MigrationName
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Applying migrations to database..." -ForegroundColor Green
dotnet ef database update
exit $LASTEXITCODE
