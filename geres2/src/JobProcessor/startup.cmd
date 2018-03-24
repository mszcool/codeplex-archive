@echo off
net localgroup JobProcessors /add
powershell -command "Set-ExecutionPolicy Unrestricted" 2>> error.out

