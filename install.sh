#!/bin/sh

# Todo: Gir.Core export

# Generate nuget sources file
python3 ./flatpak/flatpak-dotnet-generator.py --dotnet 10 --freedesktop 24.08 nuget-sources.json ./src/Aria.App/Aria.App.csproj

# Build the package
flatpak-builder --force-clean --user --install-deps-from=flathub --repo=repo --install builddir ./flatpak/nl.mirthestam.aria.yml 