#!/bin/sh
# Disable telemetry
export DOTNET_CLI_TELEMETRY_OPTOUT=1 

# Publish Aria
# Todo find a way that the gresource is always generated and copied
dotnet build ./src/Aria.App/Aria.App.csproj -c Release --no-self-contained --source ./nuget-sources
dotnet publish ./src/Aria.App/Aria.App.csproj -o ./dist -c Release --no-self-contained --source ./nuget-sources
cp ./src/Aria.App/bin/Release/net10.0/nl.mirthestam.aria.gresource ./dist

# Pack Aria
mkdir -p ${FLATPAK_DEST}/bin
cp -r ./dist/* ${FLATPAK_DEST}/bin