$proc = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Workforce.Host.Api/Workforce.Host.Api.csproj --urls http://127.0.0.1:5041" -PassThru -WindowStyle Hidden
Write-Host "Started API with PID: $($proc.Id)"

$exported = $false
for ($i = 0; $i -lt 15; $i++) {
    Start-Sleep -Seconds 1
    try {
        $response = Invoke-WebRequest -Uri "http://127.0.0.1:5041/openapi/v1.json" -UseBasicParsing -TimeoutSec 3
        if ($response.StatusCode -eq 200 -and $response.Content.Length -gt 100) {
            $response.Content | Out-File -FilePath "web/tooling/openapi/workforce.openapi.json" -Encoding utf8
            Write-Host "Successfully exported OpenAPI spec (Size: $($response.Content.Length) bytes)."
            Write-Host "Contains recruitment: $($response.Content.Contains('recruitment'))"
            $exported = $true
            break
        }
    }
    catch {
        # Retry until ready
    }
}

if (-not $exported) {
    Write-Host "Failed to fetch OpenAPI spec within timeout."
}

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
Write-Host "Stopped API process."
