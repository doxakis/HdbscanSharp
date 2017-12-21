@echo off
ECHO Clean up bin folder.
rd /S /Q HdbscanSharp\bin

ECHO Compile HdbscanSharp.
cd HdbscanSharp
dotnet pack --include-symbols --include-source

ECHO Press enter to upload the nuget package.
PAUSE

cd bin\Debug

ECHO Upload symbols.
dotnet nuget push *.symbols.nupkg --source https://nuget.smbsrc.net/

ECHO Upload nuget.
del *.symbols.nupkg
dotnet nuget push *.nupkg --source https://www.nuget.org/api/v2/package

ECHO Done
PAUSE
