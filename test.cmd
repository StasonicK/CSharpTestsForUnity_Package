@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  CSharpTestToolForUnity - CMD Test Runner
::  Usage: test <command>
:: ============================================================

set SCRIPT_DIR=%~dp0
if "%SCRIPT_DIR:~-1%"=="\" set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%

set CSPROJ=%SCRIPT_DIR%\CSharpTestToolForUnity.csproj
set SETTINGS=%SCRIPT_DIR%\test.runsettings
set SEARCH_ROOT=%SCRIPT_DIR%

:: No arguments
if "%~1"=="" (
    echo Error: no command specified. Run: test info
    exit /b 1
)

:: ── Detect 'coverage' flag in any position ───────────────────────────────
set HAS_COVERAGE=0
for %%A in (%*) do (
    if /i "%%A"=="coverage" set HAS_COVERAGE=1
)

:: ── Build base command (all tokens minus 'coverage') ────────────────────
set BASE=
for %%A in (%*) do (
    if /i not "%%A"=="coverage" (
        if "!BASE!"=="" (
            set BASE=%%A
        ) else (
            set BASE=!BASE! %%A
        )
    )
)

:: ── Route command ──────────────────────────────────────────────────────
if /i "!BASE!"=="info"     goto :show_info
if /i "!BASE!"=="all"      goto :run_all
if /i "!BASE!"=="examples" goto :run_examples

:: Path-based command
goto :run_path


:: ============================================================
:: run_all
:: ============================================================
:run_all
if %HAS_COVERAGE%==1 (
    echo Running project tests with coverage...
    call :dotnet_coverage "FullyQualifiedName!~Project.Tests" "%SCRIPT_DIR%\coverage"
) else (
    echo Running project tests...
    echo   ^(Add your project scripts to the csproj and use a custom namespace^)
    dotnet test "%CSPROJ%" --settings "%SETTINGS%" --filter "FullyQualifiedName!~Project.Tests" --logger "console;verbosity=minimal"
)
exit /b %errorlevel%


:: ============================================================
:: run_examples
:: ============================================================
:run_examples
if %HAS_COVERAGE%==1 (
    echo Running examples with coverage...
    call :dotnet_coverage "FullyQualifiedName~Project.Tests" "%SCRIPT_DIR%\coverage-examples"
) else (
    echo Running package example tests...
    dotnet test "%CSPROJ%" --settings "%SETTINGS%" --filter "FullyQualifiedName~Project.Tests" --logger "console;verbosity=minimal"
)
exit /b %errorlevel%


:: ============================================================
:: run_path - handles folder or .cs file
:: ============================================================
:run_path
:: Normalize forward slashes and strip trailing slash
set NORM=!BASE:/=\!
if "!NORM:~-1!"=="\" set NORM=!NORM:~0,-1!

:: ── Try as .cs file ──────────────────────────────────────────
set NORM_EXT=!NORM:~-3!
if /i "!NORM_EXT!"==".cs" (
    :: 1. Direct path
    if exist "%SEARCH_ROOT%\!NORM!" (
        call :run_file "%SEARCH_ROOT%\!NORM!"
        exit /b !errorlevel!
    )
    :: 2. Filename-only search (user typed just 'HealthSystemTests.cs')
    set BASENAME=!NORM!
    for %%B in (!NORM!) do set BASENAME=%%~nxB
    set FOUND_FILE=
    for /f "delims=" %%F in ('dir /b /s "%SEARCH_ROOT%\!BASENAME!" 2^>nul') do (
        if not defined FOUND_FILE set FOUND_FILE=%%F
    )
    if defined FOUND_FILE (
        call :run_file "!FOUND_FILE!"
        exit /b !errorlevel!
    )
    echo Error: '!NORM!' not found.
    exit /b 1
)

:: ── Try as folder ────────────────────────────────────────────
if exist "%SEARCH_ROOT%\!NORM!\" (
    call :run_folder "%SEARCH_ROOT%\!NORM!"
    exit /b !errorlevel!
)

:: ── Nothing matched ──────────────────────────────────────────
echo !NORM! | findstr /c:"\" /c:"/" >nul 2>&1
if %errorlevel%==0 (
    echo Not found: '!BASE!'.
) else (
    echo Unknown command: '!BASE!'.
    echo   Run 'test info' to see available commands.
)
exit /b 1


:: ============================================================
:: run_file <full_path>
:: ============================================================
:run_file
set FILE_FULL=%~1
set FILE_NAME=%~nx1
for /f "delims=." %%N in ("%FILE_NAME%") do set CLASS_NAME=%%N
set FILTER=FullyQualifiedName~!CLASS_NAME!
echo Running [!FILE_NAME!]...
if %HAS_COVERAGE%==1 (
    call :dotnet_coverage "!FILTER!" "%SCRIPT_DIR%\coverage"
) else (
    dotnet test "%CSPROJ%" --settings "%SETTINGS%" --filter "!FILTER!" --logger "console;verbosity=minimal"
)
exit /b %errorlevel%


:: ============================================================
:: run_folder <full_path>
:: ============================================================
:run_folder
set FOLDER_FULL=%~1
set FILTER=
set FIRST=1
for /f "delims=" %%F in ('dir /b /s /a:-d "%FOLDER_FULL%\*.cs" 2^>nul') do (
    for /f "delims=." %%N in ("%%~nF") do (
        if !FIRST!==1 (
            set FILTER=FullyQualifiedName~%%N
            set FIRST=0
        ) else (
            set FILTER=!FILTER!^|FullyQualifiedName~%%N
        )
    )
)
if "!FILTER!"=="" (
    echo Warning: no .cs files found in: !FOLDER_FULL!
    exit /b 1
)
set REL=!FOLDER_FULL:%SEARCH_ROOT%\=!
echo Running folder [!REL!]...
if %HAS_COVERAGE%==1 (
    call :dotnet_coverage "!FILTER!" "%SCRIPT_DIR%\coverage"
) else (
    dotnet test "%CSPROJ%" --settings "%SETTINGS%" --filter "!FILTER!" --logger "console;verbosity=minimal"
)
exit /b %errorlevel%


:: ============================================================
:: dotnet_coverage <filter> <out_dir>
:: ============================================================
:dotnet_coverage
set COV_FILTER=%~1
set COV_DIR=%~2
set HTML_DIR=!COV_DIR!\html
dotnet test "%CSPROJ%" --settings "%SETTINGS%" --filter "!COV_FILTER!" --collect:"XPlat Code Coverage" --results-directory "!COV_DIR!" --logger "console;verbosity=minimal"
set TEST_RC=%errorlevel%
set LATEST_XML=
for /f "delims=" %%X in ('dir /b /s /a:-d /o:-d "!COV_DIR!\coverage.cobertura.xml" 2^>nul') do (
    if not defined LATEST_XML set LATEST_XML=%%X
)
if defined LATEST_XML (
    reportgenerator -reports:"!LATEST_XML!" -targetdir:"!HTML_DIR!" -reporttypes:Html >nul 2>&1
    if exist "!HTML_DIR!\index.html" (
        echo   Report : !HTML_DIR!\index.html
        start "" "!HTML_DIR!\index.html"
    ) else (
        echo   Report generation failed.
        echo   Install: dotnet tool install -g dotnet-reportgenerator-globaltool
    )
)
exit /b %TEST_RC%


:: ============================================================
:: show_info
:: ============================================================
:show_info
echo ================================================================
echo   test - headless NUnit runner (CMD)
echo   Open terminal: CPM RP Tools ^> CSharp Test Tool ^> Open Terminal Here
echo ================================================================
echo.
echo   COMMANDS
echo.
echo   test all
echo       Run your project tests (excludes built-in package tests).
echo         test all
echo.
echo   test examples
echo       Run all built-in package tests (unit + integration + NSubstitute).
echo         test examples
echo.
echo   test ^<path/to/Folder^>
echo       Run all tests in a folder. Path relative to tool root.
echo         test Tests
echo         test Tests\Integration
echo         test Tests\UnitTests
echo.
echo   test ^<path/to/Script.cs^>
echo       Run a single test script. .cs extension required.
echo         test Tests\UnitTests\HealthSystemTests.cs
echo         test Tests\Integration\CombatIntegrationTests.cs
echo         test HealthSystemTests.cs    (filename search)
echo.
echo   test info
echo       Show this help.
echo.
echo   COVERAGE FLAG
echo.
echo   Add 'coverage' anywhere in the command for code coverage + HTML report.
echo.
echo         test examples coverage
echo         test all coverage
echo         test Tests\UnitTests\HealthSystemTests.cs coverage
echo         test Tests\Integration coverage
echo         test coverage examples    ^(flag position does not matter^)
echo.
echo   TEST TYPES
echo.
echo   Unit tests        Tests\UnitTests\
echo       Pure C# logic. No Unity engine. Fast (^<1ms each).
echo       Frameworks: NUnit 4, NSubstitute 5, Shouldly 4
echo.
echo   Integration tests  Tests\Integration\
echo       Multiple real systems + mocks/stubs.
echo       Frameworks: NUnit 4, NSubstitute 5, Shouldly 4
echo.
echo ================================================================
exit /b 0
