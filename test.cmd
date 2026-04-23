@echo off
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0test.ps1" %*
