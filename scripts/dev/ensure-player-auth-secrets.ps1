#Requires -Version 5.1

function New-LedsAuthenticationSecret {
    param(
        [Parameter(Mandatory = $true)]
        [int]$ByteLength
    )

    $bytes = New-Object byte[] $ByteLength
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }

    return [Convert]::ToBase64String($bytes)
}

function Initialize-LedsPlayerAuthenticationSecrets {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $projectPath = Join-Path $RepositoryRoot "services\player\src\Leds.Player.Api\Leds.Player.Api.csproj"
    $configuredSecrets = @(& dotnet user-secrets list --project $projectPath 2>$null)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read Player API development secrets. Ensure the .NET SDK is installed."
    }

    $secretDefinitions = @(
        @{ Name = "Authentication:MfaProtectionKey"; ByteLength = 32 },
        @{ Name = "Authentication:Jwt:SigningKey"; ByteLength = 48 }
    )

    foreach ($definition in $secretDefinitions) {
        $escapedName = [Regex]::Escape($definition.Name)
        $isConfigured = $configuredSecrets | Where-Object { $_ -match "^$escapedName\s*=" }
        if ($isConfigured) {
            continue
        }

        $value = New-LedsAuthenticationSecret -ByteLength $definition.ByteLength
        & dotnet user-secrets set $definition.Name $value --project $projectPath | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to persist development secret '$($definition.Name)'."
        }
    }
}
