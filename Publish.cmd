@echo off
echo Cleaning...
dotnet clean --verbosity minimal
echo Publishing single file...

dotnet publish SqlDatabaseDump.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
