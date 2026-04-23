param([string]$Target = "")
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
            & dotnet test $CsprojPath --filter $Filter --logger "console;verbosity=normal"
        } else {
            & dotnet test $CsprojPath --logger "console;verbosity=normal"
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

function Show-Info {
    $w = 62
    $line = '-' * $w
    Write-Host $line -ForegroundColor DarkGray
    Write-Host '  test - headless NUnit runner' -ForegroundColor White
    Write-Host $line -ForegroundColor DarkGray
    Write-Host ''
    Write-Host '  COMMANDS' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  test all' -ForegroundColor Cyan
    Write-Host '      Run every test in the project (unit + integration).'
    Write-Host ''
    Write-Host '      Example:'
    Write-Host '        test all'
    Write-Host ''
    Write-Host '  test <path/to/folder>' -ForegroundColor Cyan
    Write-Host '      Run all tests inside a folder.'
    Write-Host '      Path is relative to tool root (CSharpTestToolForUnity/).'
    Write-Host '      Both slash directions accepted: / and \.'
    Write-Host '      Note: View/Views folders are not testable and will error.'
    Write-Host ''
    Write-Host '      Examples:'
    Write-Host '        test Tests'
    Write-Host '        test Tests/Integration'
    Write-Host '        test Tests/UnitTests'
    Write-Host ''
    Write-Host '  test <path/to/Script.cs>' -ForegroundColor Cyan
    Write-Host '      Run tests in a single script.'
    Write-Host '      Path is relative to tool root (CSharpTestToolForUnity/).'
    Write-Host '      .cs extension is required.'
    Write-Host ''
    Write-Host '      Examples:'
    Write-Host '        test Scripts/Tests/EditMode/UnitTests/HealthSystemTests.cs'
    Write-Host '        test Scripts/Tests/EditMode/Integration/CombatIntegrationTests.cs'
    Write-Host ''
    Write-Host '  test info' -ForegroundColor Cyan
    Write-Host '      Show this help.'
    Write-Host ''
    Write-Host '      Example:'
    Write-Host '        test info'
    Write-Host ''
    Write-Host '  TEST TYPES' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  test coverage' -ForegroundColor Cyan
    Write-Host '      Run all tests with code coverage. Outputs Cobertura XML.'
    Write-Host ''
    Write-Host '      Example:'
    Write-Host '        test coverage'
    Write-Host ''
    Write-Host '  TEST TYPES' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  Unit tests      Tests\UnitTests\'
    Write-Host '      Pure C# logic. No Unity engine. Fast (<1ms each).'
    Write-Host '      Frameworks: NUnit 4, NSubstitute 5, Shouldly 4'
    Write-Host ''
    Write-Host '  Integration tests  Tests\Integration\'
    Write-Host '      Multiple systems together. Real logic + mocks/stubs.'
    Write-Host '      Frameworks: NUnit 4, NSubstitute 5, Shouldly 4'
    Write-Host '      Mocking: Substitute.For<T>() creates mocks and stubs.'
    Write-Host '      Assertions: x.ShouldBe(y), x.ShouldNotBeNull(), etc.'
    Write-Host ''
    Write-Host $line -ForegroundColor DarkGray
}

    Write-Host '  test examples' -ForegroundColor Cyan
    Write-Host '      Run NSubstitute reference examples (mock, stub, spy, arg matchers).'
    Write-Host ''
    Write-Host '      Example:'
    Write-Host '        test examples'
    Write-Host ''
# -- Main -------------------------------------------------------------------

$t = $Target.Trim()

if ($t.ToLower() -eq 'info') { Show-Info; exit 0 }
if ([string]::IsNullOrWhiteSpace($t)) { Write-Host "Error: no command specified. Run 'test info' to see usage." -ForegroundColor Red; exit 1 }

if ($t.ToLower() -eq 'all') {
    Write-Host "Running ALL tests..." -ForegroundColor Cyan
    Invoke-DotnetTest ""
    exit $LASTEXITCODE
}

if ($t.ToLower() -eq 'coverage') {
    Write-Host "Running coverage..." -ForegroundColor Cyan
    $outDir = Join-Path $ScriptDir "coverage"
    dotnet test $CsprojPath --collect:"XPlat Code Coverage" --results-directory $outDir --logger "console;verbosity=minimal"
    $rep = @(Get-ChildItem -Path $outDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1)
    if ($rep.Count -gt 0) { Write-Host "  Report : $($rep[0].FullName)" -ForegroundColor DarkGray }
    exit $LASTEXITCODE
}

if ($t.ToLower() -eq 'examples') {
    Write-Host "Running examples..." -ForegroundColor Cyan
    dotnet test $CsprojPath --filter "FullyQualifiedName~SubstituteExamples" --logger "console;verbosity=normal"
    exit $LASTEXITCODE
}

# Normalize slashes, build full path from Assets root
$norm = $t.TrimEnd('\/').Replace('/', '\')
$full = Join-Path $SearchRoot $norm

# Script: must end with .cs
if ($norm -match '\.cs$') {
    $fileMatches = @(Find-File $norm)
    if ($fileMatches.Count -eq 1) { Run-File $fileMatches[0]; exit $LASTEXITCODE }
    if ($fileMatches.Count -gt 1) {
        Write-Host "Ambiguous: '$norm' matches $($fileMatches.Count) scripts:" -ForegroundColor Yellow
        $fileMatches | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
        $choice = (Read-Host 'Enter full path').Trim()
        if (Test-Path $choice -PathType Leaf) { Run-File $choice; exit $LASTEXITCODE }
        Write-Host "Error: '$choice' is not a valid file." -ForegroundColor Red; exit 1
    }
    Write-Host "Error: script not found: $norm" -ForegroundColor Red
    Write-Host "  Searched under tool root by direct path and suffix match." -ForegroundColor DarkGray
    exit 1
}

# Folder: no .cs extension
$fMatches = @(Find-Folder $norm)
if ($fMatches.Count -eq 1) { Run-Folder $fMatches[0]; exit $LASTEXITCODE }
if ($fMatches.Count -gt 1) {
    Write-Host "Ambiguous: '$norm' matches $($fMatches.Count) folders:" -ForegroundColor Yellow
    $fMatches | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    $choice = (Read-Host 'Enter full path').Trim()
    if (Test-Path $choice -PathType Container) { Run-Folder $choice; exit $LASTEXITCODE }
    Write-Host "Error: '$choice' is not a valid folder." -ForegroundColor Red; exit 1
}

# Nothing matched
Write-Host "Error: '$norm' not found in tool folder." -ForegroundColor Red
if (Test-Path (Join-Path $SearchRoot ($norm + '.cs')) -PathType Leaf) {
    Write-Host "  Did you mean: $norm.cs  (add .cs extension)" -ForegroundColor DarkGray
} else {
    Write-Host "  For scripts, add .cs extension. For folders, check spelling." -ForegroundColor DarkGray
    Write-Host "  Run 'test info' to see usage." -ForegroundColor DarkGray
}
exit 1
