<#
.SYNOPSIS
    Generates strong random values for every secret docker-compose.yml expects,
    formatted as a ready-to-paste .env block.

.DESCRIPTION
    Never overwrites .env -- prints to stdout only. Copy the values you need
    into your real .env (or a separate .env.production kept outside git).
    Re-run and re-paste whenever you rotate secrets; rotating DB/RabbitMQ/Redis
    passwords for an already-running stack requires recreating those
    containers' volumes (or updating the password inside them), not just
    editing .env.

.EXAMPLE
    ./tools/generate-secrets.ps1 | Out-File -Encoding utf8 .env.production.generated
#>

$script:Rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()

function New-RandomBytes([int]$Count) {
    $buffer = New-Object byte[] $Count
    $script:Rng.GetBytes($buffer)
    return $buffer
}

function New-Secret([int]$Bytes = 32) {
    $b64 = [Convert]::ToBase64String((New-RandomBytes $Bytes))
    return ($b64 -replace '[+/=]', '')
}

function New-Password([int]$Length = 24) {
    $chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#%^&*-_'
    $buffer = New-RandomBytes $Length
    -join ($buffer | ForEach-Object { $chars[$_ % $chars.Length] })
}

Write-Output "# Generated $(Get-Date -Format o) -- paste into .env / .env.production, do not commit."
Write-Output ""
Write-Output "ASPNETCORE_ENVIRONMENT=Production"
Write-Output ""
Write-Output "# ---- JWT ----"
Write-Output "JWT_SECRET=$(New-Secret 48)"
Write-Output "JWT_ISSUER=BitirmeProject.IdentityService"
Write-Output "JWT_AUDIENCE=BitirmeProject.Clients"
Write-Output ""
Write-Output "# ---- RabbitMQ ----"
Write-Output "RABBITMQ_USER=admin"
Write-Output "RABBITMQ_PASS=$(New-Password 24)"
Write-Output ""
Write-Output "# ---- Redis ----"
Write-Output "REDIS_PASS=$(New-Password 24)"
Write-Output ""
Write-Output "# ---- Database passwords ----"
Write-Output "IDENTITY_DB_PASS=$(New-Password 24)"
Write-Output "PROJECT_DB_PASS=$(New-Password 24)"
Write-Output "ISSUE_DB_PASS=$(New-Password 24)"
Write-Output "SPRINT_DB_PASS=$(New-Password 24)"
Write-Output "NOTIFICATION_DB_PASS=$(New-Password 24)"
Write-Output "STORAGE_DB_PASS=$(New-Password 24)"
Write-Output "AI_DB_PASS=$(New-Password 24)"
Write-Output ""
Write-Output "# ---- Internal service-to-service API key (shared across services) ----"
Write-Output "INTERNAL_SERVICE_API_KEY=$(New-Secret 32)"
Write-Output ""
Write-Output "# ---- Admin seeder ----"
Write-Output "SEED_ADMIN=true"
Write-Output "ADMIN_PASSWORD=$(New-Password 20)"
Write-Output "ADMIN_EMAIL=admin@example.com"
Write-Output "ADMIN_USERNAME=admin"
Write-Output ""
Write-Output "# ---- Public addresses (set to the real host before deploying) ----"
Write-Output "PUBLIC_API_BASE_URL=http://localhost:5000"
Write-Output "PUBLIC_WEB_ORIGIN=http://localhost:5174"
