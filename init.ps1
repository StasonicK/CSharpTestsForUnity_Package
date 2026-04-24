# CSharpTestToolForUnity - PowerShell profile setup
# Run once: .\init.ps1

$marker  = '# CSharpTestToolForUnity'
$toolDir = $PSScriptRoot
$entry   = @"

# CSharpTestToolForUnity
function test { & '$toolDir\test.ps1' @args }

`$ExecutionContext.InvokeCommand.CommandNotFoundAction = {
    param(`$CommandName, `$CommandLookupEventArgs)
    if (Test-Path (Join-Path `$PWD 'test.ps1')) {
        Write-Host "Unknown command: '`$CommandName'." -ForegroundColor Red
        Write-Host "  Run './test info' to see available commands." -ForegroundColor DarkGray
        `$CommandLookupEventArgs.StopSearch = `$true
    }
}
"@

if (!(Test-Path $PROFILE)) {
    New-Item -Path $PROFILE -ItemType File -Force | Out-Null
}

# Always replace old entry so updates take effect
$raw = Get-Content $PROFILE -Raw -ErrorAction SilentlyContinue
if (-not $raw) { $raw = '' }
if ($raw.Contains($marker)) {
    $idx = $raw.IndexOf($marker)
    $raw = $raw.Substring(0, [Math]::Max(0, $idx - 2))
    Set-Content -Path $PROFILE -Value $raw.TrimEnd() -NoNewline
}

Add-Content -Path $PROFILE -Value $entry
Write-Host '  Done. Profile updated:' -ForegroundColor Green
Write-Host '  Note: if you move this project folder, re-run ./init.ps1 to update the path.' -ForegroundColor DarkGray
Write-Host "  $PROFILE" -ForegroundColor DarkGray
Write-Host '  Restart PowerShell to activate.' -ForegroundColor DarkGray
