# Build script for generating NuGet packages

Write-Output "Cleaning output folders..."

if (Test-Path -Path "output\")
{
  Remove-Item output\* -Recurse -Force
}

# Clean both debug and release
dotnet clean source/Lite.StateMachine.slnx
dotnet clean source/Lite.StateMachine.slnx --configuration Release

# Build package for release
dotnet build source/Lite.StateMachine.slnx --configuration Release

# Publish
Write-Output "Cleaning publish folder.."

if (Test-Path -Path "publish\")
{
  Remove-Item publish\* -Recurse -Force
}
else
{
  New-Item -Path '.\publish' -ItemType Directory
}

$file = Get-Item -Path "output/Lite.StateMachine/Release/net10.0/Lite.StateMachine.dll";
Write-Output "-[ Version: v$($file.VersionInfo.FileVersion)..."
Write-Output "-[ VersionRaw: v$($file.VersionInfo.FileVersionRaw)..."
Write-Output "-[ ProductVersion: v$($file.VersionInfo.ProductVersion)..."
Write-Output "-[ ProductVersionRaw: v$($file.VersionInfo.ProductVersionRaw)..."

Write-Output "Copying package..."
Copy-Item -Path "output/Lite.StateMachine/Release/*.nupkg" -Destination "publish/"

## Publish build artifacts
##dotnet publish src/Lite.EventIpc/Lite.EventIpc.csproj /p:PublishProfile=src/Lite.EventIpc/Properties/PublishProfiles/win-x64.pubxml /p:DebugType=None /p:DebugSymbols=false
##
#### Compress published artifacts
##Write-Output "Compressing published artifacts..."
##$dttm = (Get-Date).ToString("yyyy-MM-dd")
##$version = (Get-Item -Path "publish/win-x64/Lite.EventIpc.dll").VersionInfo.FileVersion
##Compress-Archive -Path "publish/win-x64/*" -DestinationPath "publish/Lite.EventIpc-${version}-(win-x64)_${dttm}.zip"
