@echo off
setlocal
set SCRIPT_DIR=%~dp0
rem Prefer published exe if available
if exist "%SCRIPT_DIR%Gbm\bin\Release\net10.0\publish\Gbm.exe" (
	"%SCRIPT_DIR%Gbm\bin\Release\net10.0\publish\Gbm.exe" %*
	exit /b %errorlevel%
)
rem Fallback to debug dll via dotnet
if exist "%SCRIPT_DIR%Gbm\bin\Debug\net10.0\Gbm.dll" (
	dotnet "%SCRIPT_DIR%Gbm\bin\Debug\net10.0\Gbm.dll" %*
	exit /b %errorlevel%
)
rem Last resort: try running from project directory
if exist "%SCRIPT_DIR%Gbm\Gbm.csproj" (
	pushd "%SCRIPT_DIR%Gbm"
	dotnet run -- %*
	set EC=%errorlevel%
	popd
	exit /b %EC%
)
echo Could not find Gbm executable. Build the project first: dotnet build Gbm
exit /b 1

