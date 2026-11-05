$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'DocumentGenerator.csproj'
$output = Join-Path $PSScriptRoot 'publish\win-x64'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/10.0' }
dotnet restore $project
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Host "Portable build created: $output" -ForegroundColor Green
