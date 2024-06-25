#!/bin/sh -ex

currentDir="$(basename "$(pwd)")"
if [ "$currentDir" != "JoeBot" ]; then
  echo "Script must be executed inside the JoeBot project directory."
  exit 1
fi

# Remove old builds
rm -rf out
mkdir -p out
touch out/version.txt
version=$(grep -e '<Version>' JoeBot.csproj | awk -F"[<>]" '{print $3}')
echo "$version" > out/version.txt

# Build for Generic MacOS x64
runtime=osx-x64
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot ${directory}/joe-${runtime}-"${version}"
cp ${directory}/joe-${runtime}-"${version}" ${directory}/joe

# Build for MacOS Monterrey x64
runtime=osx.12-x64
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot ${directory}/joe-${runtime}-"${version}"
cp ${directory}/joe-${runtime}-"${version}" ${directory}/joe

# Build for MacOS Monterrey ARM
runtime=osx.12-arm64
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot ${directory}/joe-${runtime}-"${version}"
cp ${directory}/joe-${runtime}-"${version}" ${directory}/joe

# Build for Linux x64
runtime=linux-x64
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot ${directory}/joe-${runtime}-"${version}"
cp ${directory}/joe-${runtime}-"${version}" ${directory}/joe

# Build for Linux ARM
runtime=linux-arm
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot ${directory}/joe-${runtime}-"${version}"
cp ${directory}/joe-${runtime}-"${version}" ${directory}/joe

# Build for Windows x64
runtime=win-x64
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot.exe ${directory}/joe-${runtime}-"${version}".exe
cp ${directory}/joe-${runtime}-"${version}".exe ${directory}/joe.exe

# Build for Windows ARM
runtime=win-arm64
directory=out/${runtime}
dotnet publish -c Release -r ${runtime} --self-contained -o ${directory} -p:PublishSingleFile=true
mv ${directory}/JoeBot.exe ${directory}/joe-${runtime}-"${version}".exe
cp ${directory}/joe-${runtime}-"${version}".exe ${directory}/joe.exe