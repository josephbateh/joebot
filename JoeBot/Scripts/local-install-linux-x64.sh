#!/bin/sh -ex

buildPath=out/linux-x64
binPath=/usr/local/bin/joe

chmod +x "${buildPath}/joe"
sudo cp "${buildPath}/joe" "${binPath}"

joe -h
