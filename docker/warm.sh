#!/usr/bin/env bash
# Restores every reference state and acceptance suite copied under /tmp/warm by
# `bench build-image`, then generates the Burgr ones: the NuGet and Burgr caches of the
# image then hold every package a pass needs.
set -eu
cd /tmp/warm
for state in states/*/; do
  (
    cd "$state"
    if [ -f dotnet-tools.json ]; then
      dotnet tool restore
      dotnet burgr specs > /dev/null
    fi
    dotnet restore ./*.slnx
  )
done
for suite in cases/*/acceptance/; do
  dotnet restore "$suite"
done
