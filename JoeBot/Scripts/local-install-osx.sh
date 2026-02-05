#!/bin/sh -ex

# Default to generic x64
buildPath=out/osx-x64
binPath=/usr/local/bin/joe

arch=$(uname -m)
version=$(sw_vers -productVersion | sed "s:.[[:digit:]]*.$::g")

# Check if ARM
if [ "${arch}" = 'arm64' ]; then
  buildPath=out/osx-arm64
fi

chmod +x ${buildPath}/joe
sudo cp ${buildPath}/joe ${binPath}

joe -h
