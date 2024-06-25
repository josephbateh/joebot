#!/bin/sh -ex

# Default to generic x64
buildPath=out/osx-x64
binPath=/usr/local/bin/joe

arch=$(uname -m)
version=$(sw_vers -productVersion | sed "s:.[[:digit:]]*.$::g")

# Check if ARM
if [ "${arch}" = 'arm64' ]; then
  buildPath=out/osx.12-arm64
fi

# Check if X64
if [ "${arch}" = 'x64' ]; then
  buildPath=out/osx.12-x64
fi

chmod +x ${buildPath}/joe
sudo cp ${buildPath}/joe ${binPath}

joe -h
