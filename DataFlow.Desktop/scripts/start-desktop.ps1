# Run the WinForms desktop app (calls DataFlow.Api at appsettings.json BaseUrl)

$ErrorActionPreference = "Stop"
$slnRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $slnRoot
dotnet run --project src\DataFlow.UI\DataFlow.UI.csproj
