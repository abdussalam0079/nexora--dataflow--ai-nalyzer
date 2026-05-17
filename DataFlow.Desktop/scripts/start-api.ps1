# Starts the standalone DataFlow API (Python/FastAPI) used by the WinForms app.
# Groq key lives in DataFlow.Api\.env — not in the React project.

$ErrorActionPreference = "Stop"
$apiRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\DataFlow.Api")

if (-not (Test-Path (Join-Path $apiRoot ".env"))) {
    Write-Host "Missing DataFlow.Api\.env — copy .env.example to .env and set GROQ_API_KEY." -ForegroundColor Yellow
    exit 1
}

$venvPython = Join-Path $apiRoot "venv\Scripts\python.exe"
$python = if (Test-Path $venvPython) { $venvPython } else { "python" }

Push-Location $apiRoot
try {
    if (-not (Test-Path $venvPython)) {
        Write-Host "Creating virtual environment in DataFlow.Api\venv ..."
        & python -m venv venv
        & $venvPython -m pip install -q -r requirements.txt 2>$null
        if (-not (Test-Path (Join-Path $apiRoot "requirements.txt"))) {
            & $venvPython -m pip install -q fastapi uvicorn python-dotenv groq pandas sqlalchemy pydantic python-multipart openpyxl
        }
    }
    Write-Host "DataFlow API → http://127.0.0.1:8000  (Ctrl+C to stop)" -ForegroundColor Cyan
    & $python -m uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
}
finally {
    Pop-Location
}
