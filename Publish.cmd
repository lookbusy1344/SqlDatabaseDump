@echo off
setlocal

echo Cleaning...
dotnet clean --verbosity minimal

for %%R in (win-x64 win-arm64 osx-arm64 linux-x64) do (
	echo Publishing %%R...
	dotnet publish SqlDatabaseDump.csproj -c Release -r %%R --self-contained false -o bin\publish\%%R /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None
	if errorlevel 1 exit /b 1
)
