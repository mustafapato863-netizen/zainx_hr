$proc = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Workforce.Host.Api/Workforce.Host.Api.csproj --urls http://127.0.0.1:5041" -PassThru -WindowStyle Hidden
Write-Host "Started API with PID: $($proc.Id)"
Start-Sleep -Seconds 7

try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:5041/openapi/v1.json" -UseBasicParsing
    $response.Content | Out-File -FilePath "web/tooling/openapi/workforce.openapi.json" -Encoding utf8
    Write-Host "Successfully exported OpenAPI spec (Size: $($response.Content.Length) bytes)."
}
catch {
    Write-Host "Error fetching OpenAPI spec: $_"
}
finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Stopped API process."
}
