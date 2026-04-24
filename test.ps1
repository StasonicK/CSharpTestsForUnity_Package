param([Parameter(ValueFromRemainingArguments=$true)][string[]]$TargetParts = @())
$Target = ($TargetParts -join ' ').Trim()
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$CsprojPath  = Join-Path $ScriptDir "CSharpTestToolForUnity.csproj"
$SearchRoot  = $ScriptDir

function Get-TestClassNames([string]$FilePath) {
    $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
    if (-not $content) { return @() }
    if ($content -notmatch '\[Test(?:Fixture)?\b') { return @() }
    $m = [regex]::Matches($content, '(?:public|internal)\s+class\s+(\w+)')
    return @($m | ForEach-Object { $_.Groups[1].Value })
}
function Build-Filter([string[]]$ClassNames) {
    return ($ClassNames | ForEach-Object { "FullyQualifiedName~$_" }) -join "|"
}
function Invoke-DotnetTest([string]$Filter) {
    # Scope ErrorActionPreference to Continue so dotnet test exit code 1
    # (test failures) does not abort the PowerShell script in PS7+.
    # All tests still run - NUnit reports every result before exiting.
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($Filter) {
            Write-Host "  filter : $Filter" -ForegroundColor DarkGray
            & dotnet test $CsprojPath --settings "$ScriptDir\test.runsettings" --filter $Filter --logger "console;verbosity=minimal"
        } else {
            & dotnet test $CsprojPath --settings "$ScriptDir\test.runsettings" --filter "FullyQualifiedName!~Project.Tests" --logger "console;verbosity=minimal"
        }
    } finally {
        $ErrorActionPreference = $prevEAP
    }
}
function Run-Folder([string]$FolderPath) {
    $files = @(Get-ChildItem -Path $FolderPath -Filter "*.cs" -Recurse)
    if ($files.Count -eq 0) { Write-Host "Warning: no .cs files in: $FolderPath" -ForegroundColor Yellow; return }
    $classes = @($files | ForEach-Object { Get-TestClassNames $_.FullName } | Where-Object { $_ })
    if ($classes.Count -eq 0) { Write-Host "Warning: no test classes in: $FolderPath" -ForegroundColor Yellow; return }
    $rel = ($FolderPath -ireplace [regex]::Escape($SearchRoot), '.').TrimStart('\\/') 
    Write-Host "Running folder [$rel] - $($classes.Count) class(es): $($classes -join ', ')" -ForegroundColor Cyan
    Invoke-DotnetTest (Build-Filter $classes)
}
function Run-File([string]$FilePath) {
    $classes = @(Get-TestClassNames $FilePath | Where-Object { $_ })
    if ($classes.Count -eq 0) { Write-Host "Warning: no test classes in: $(Split-Path -Leaf $FilePath)" -ForegroundColor Yellow; return }
    Write-Host "Running script [$(Split-Path -Leaf $FilePath)] - $($classes.Count) class(es): $($classes -join ', ')" -ForegroundColor Cyan
    Invoke-DotnetTest (Build-Filter $classes)
}


# Resolve actual filesystem path (case-insensitive) then fall back to suffix search
function Find-File([string]$Norm) {
    $cs   = if ($Norm -match '\.cs$') { $Norm } else { "$Norm.cs" }
    $full = Join-Path $SearchRoot $cs
    if (Test-Path $full -PathType Leaf) { return @((Get-Item -LiteralPath $full).FullName) }
    # Fallback: suffix search anywhere under tool folder
    return @(Get-ChildItem -Path $SearchRoot -Filter '*.cs' -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName.EndsWith('\' + $cs, [System.StringComparison]::OrdinalIgnoreCase) } |
        Select-Object -ExpandProperty FullName)
}
function Find-Folder([string]$Norm) {
    $full = Join-Path $SearchRoot $Norm
    if (Test-Path $full -PathType Container) { return @((Get-Item -LiteralPath $full).FullName) }
    # Fallback: suffix search anywhere under tool folder
    return @(Get-ChildItem -Path $SearchRoot -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName.EndsWith('\' + $Norm, [System.StringComparison]::OrdinalIgnoreCase) } |
        Select-Object -ExpandProperty FullName)
}

function Run-WithCoverage([string]$Filter, [string]$CovDir) {
    $htmlDir = Join-Path $CovDir "html"
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($Filter) {
            Write-Host "  filter : $Filter" -ForegroundColor DarkGray
            & dotnet test $CsprojPath --settings "$ScriptDir\test.runsettings" --filter $Filter --collect:"XPlat Code Coverage" --results-directory $CovDir --logger "console;verbosity=minimal"
        } else {
            & dotnet test $CsprojPath --settings "$ScriptDir\test.runsettings" --collect:"XPlat Code Coverage" --results-directory $CovDir --logger "console;verbosity=minimal"
        }
        $xml = @(Get-ChildItem -Path $CovDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1)
        if ($xml.Count -gt 0) {
            reportgenerator -reports:"$($xml[0].FullName)" -targetdir:"$htmlDir" -reporttypes:Html 2>$null | Out-Null
            if (Test-Path "$htmlDir\index.html") {
                Write-Host "  Report : $htmlDir\index.html" -ForegroundColor DarkGray
                Start-Process "$htmlDir\index.html"
            } else {
                Write-Host "  Report generation failed. Is reportgenerator installed?" -ForegroundColor Yellow
                Write-Host "  Install: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor DarkGray
            }
        }
    } finally {
        $ErrorActionPreference = $prevEAP
    }
}

function Show-Info {
    $w = 62
    $line = '-' * $w
    Write-Host $line -ForegroundColor DarkGray
    Write-Host '  test - headless NUnit runner' -ForegroundColor White
    Write-Host '  Run ./init.ps1 once to register ''test'' for all sessions.' -ForegroundColor DarkGray
    Write-Host $line -ForegroundColor DarkGray
    Write-Host ''
    Write-Host '  COMMANDS' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  test all' -ForegroundColor Cyan
    Write-Host '      Run your project tests (excludes built-in package tests).'
    Write-Host '        test all'
    Write-Host ''
    Write-Host '  test examples' -ForegroundColor Cyan
    Write-Host '      Run all built-in package tests (unit + integration + NSubstitute).'
    Write-Host '        test examples'
    Write-Host ''
    Write-Host '  test <path/to/Folder>' -ForegroundColor Cyan
    Write-Host '      Run all tests in a folder. Path relative to tool root.'
    Write-Host '        test Tests'
    Write-Host '        test Tests/Integration'
    Write-Host '        test Tests/UnitTests'
    Write-Host ''
    Write-Host '  test <path/to/Script.cs>' -ForegroundColor Cyan
    Write-Host '      Run tests in a single script. .cs extension required.'
    Write-Host '        test Tests/UnitTests/HealthSystemTests.cs'
    Write-Host '        test Tests/Integration/CombatIntegrationTests.cs'
    Write-Host ''
    Write-Host '  test info' -ForegroundColor Cyan
    Write-Host '      Show this help.'
    Write-Host '        test info'
    Write-Host ''
    Write-Host '  COVERAGE FLAG' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  Add ''coverage'' anywhere in the command to collect code coverage,'
    Write-Host '  generate an HTML report and open it in the browser.'
    Write-Host ''
    Write-Host '        test examples coverage'
    Write-Host '        test all coverage'
    Write-Host '        test Tests/UnitTests/HealthSystemTests.cs coverage'
    Write-Host '        test Tests/Integration coverage'
    Write-Host '        test coverage examples      # flag position does not matter'
    Write-Host ''
    Write-Host '  TEST TYPES' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  Unit tests        Tests\UnitTests\'
    Write-Host '      Pure C# logic. No Unity engine. Fast (<1ms each).'
    Write-Host '      Frameworks: NUnit 4, NSubstitute 5, Shouldly 4'
    Write-Host ''
    Write-Host '  Integration tests  Tests\Integration\'
    Write-Host '      Multiple real systems + mocks/stubs.'
    Write-Host '      Frameworks: NUnit 4, NSubstitute 5, Shouldly 4'
    Write-Host ''
    Write-Host $line -ForegroundColor DarkGray
}

    Write-Host '  test examples' -ForegroundColor Cyan
    Write-Host '      Run all built-in package tests (100+ unit + integration + NSubstitute examples).'
    Write-Host ''
    Write-Host '      Example:'
    Write-Host '        .\test examples'
    Write-Host ''
# -- Main -------------------------------------------------------------------

$t = $Target.Trim()

if ($t.ToLower() -eq 'info') { Show-Info; exit 0 }
if ([string]::IsNullOrWhiteSpace($t)) { Write-Host "Error: no command specified. Run '.\test info' to see usage." -ForegroundColor Red; exit 1 }

# Extract 'coverage' flag - split by spaces so it never matches inside a path
$tokens = $t -split '\s+'
$hasCoverage = ($tokens -contains 'coverage')
$base = ($tokens | Where-Object { $_ -ine 'coverage' }) -join ' '

if ($base.ToLower() -eq 'all') {
    if ($hasCoverage) {
        Write-Host "Running project tests with coverage..." -ForegroundColor Cyan
        Run-WithCoverage "FullyQualifiedName!~Project.Tests" (Join-Path $ScriptDir "coverage")
    } else {
        Write-Host "Running project tests..." -ForegroundColor Cyan
        Write-Host "  (Add your project scripts to the csproj and use a custom namespace)" -ForegroundColor DarkGray
        Invoke-DotnetTest ""
    }
    exit $LASTEXITCODE
}

if ($base.ToLower() -eq 'examples') {
    if ($hasCoverage) {
        Write-Host "Running examples with coverage..." -ForegroundColor Cyan
        Run-WithCoverage "FullyQualifiedName~Project.Tests" (Join-Path $ScriptDir "coverage-examples")
    } else {
        Write-Host "Running package example tests..." -ForegroundColor Cyan
        Invoke-DotnetTest "FullyQualifiedName~Project.Tests"
    }
    exit $LASTEXITCODE
}

# Normalize slashes for path commands
$norm = $base.TrimEnd('\/').Replace('/', '\')
$full = Join-Path $SearchRoot $norm

# Script: must end with .cs
if ($norm -match '\.cs$') {
    $fileMatches = @(Find-File $norm)
    if ($fileMatches.Count -eq 1) {
        if ($hasCoverage) {
            $classes = @(Get-TestClassNames $fileMatches[0] | Where-Object { $_ })
            $filter = Build-Filter $classes
            Write-Host "Running [$(Split-Path -Leaf $fileMatches[0])] with coverage..." -ForegroundColor Cyan
            Run-WithCoverage $filter (Join-Path $ScriptDir "coverage")
        } else { Run-File $fileMatches[0] }
        exit $LASTEXITCODE
    }
    if ($fileMatches.Count -gt 1) {
        Write-Host "Ambiguous: '$norm' matches $($fileMatches.Count) scripts:" -ForegroundColor Yellow
        $fileMatches | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
        $choice = (Read-Host 'Enter full path').Trim()
        if (Test-Path $choice -PathType Leaf) {
            if ($hasCoverage) { $cls=@(Get-TestClassNames $choice|Where-Object{$_}); Run-WithCoverage (Build-Filter $cls) (Join-Path $ScriptDir "coverage") }
            else { Run-File $choice }
            exit $LASTEXITCODE
        }
        Write-Host "Error: '$choice' is not a valid file." -ForegroundColor Red; exit 1
    }
    Write-Host "Error: script not found: $norm" -ForegroundColor Red
    Write-Host "  Searched under tool root by direct path and suffix match." -ForegroundColor DarkGray
    exit 1
}

# Folder: no .cs extension
$fMatches = @(Find-Folder $norm)
if ($fMatches.Count -eq 1) {
    if ($hasCoverage) {
        $files = @(Get-ChildItem -Path $fMatches[0] -Filter '*.cs' -Recurse)
        $classes = @($files | ForEach-Object { Get-TestClassNames $_.FullName } | Where-Object { $_ })
        $rel = ($fMatches[0] -ireplace [regex]::Escape($SearchRoot), '.')
        Write-Host "Running folder [$rel] with coverage..." -ForegroundColor Cyan
        Run-WithCoverage (Build-Filter $classes) (Join-Path $ScriptDir "coverage")
    } else { Run-Folder $fMatches[0] }
    exit $LASTEXITCODE
}
if ($fMatches.Count -gt 1) {
    Write-Host "Ambiguous: '$norm' matches $($fMatches.Count) folders:" -ForegroundColor Yellow
    $fMatches | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    $choice = (Read-Host 'Enter full path').Trim()
    if (Test-Path $choice -PathType Container) {
        if ($hasCoverage) {
            $files=@(Get-ChildItem -Path $choice -Filter '*.cs' -Recurse)
            $cls=@($files|ForEach-Object{Get-TestClassNames $_.FullName}|Where-Object{$_})
            Run-WithCoverage (Build-Filter $cls) (Join-Path $ScriptDir "coverage")
        } else { Run-Folder $choice }
        exit $LASTEXITCODE
    }
    Write-Host "Error: '$choice' is not a valid folder." -ForegroundColor Red; exit 1
}

# Nothing matched
if ($norm -notmatch '[/\\]' -and $norm -notmatch '\.cs$') {
    Write-Host "Unknown command: '$t'." -ForegroundColor Red
    Write-Host "  Run '.\test info' to see available commands." -ForegroundColor DarkGray
} else {
    Write-Host "Not found: '$t'." -ForegroundColor Red
    if (Test-Path (Join-Path $SearchRoot ($norm + '.cs')) -PathType Leaf) {
        Write-Host "  Did you mean: $norm.cs  (add .cs extension)" -ForegroundColor DarkGray
    }
}
exit 1
