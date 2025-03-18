# JoeBot

Code that automates tasks normally done by Joseph Bateh.

## Installation

Requirements:

- .NET 6 SDK

### Linux

```shell
cd JoeBot
./Scripts/build.sh
./Scripts/local-install-linux-x64.sh
```

### MacOS

Install .NET 6: [link](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).

OR

```shell
brew install dotnet@6
echo 'export PATH="/opt/homebrew/opt/dotnet@6/bin:$PATH"' >> ~/.zshrc
```

Build and install JoeBot:

```shell
cd JoeBot
./Scripts/build.sh
./Scripts/local-install-osx.sh
```

## Usage

### Podcast Downloads

To find the URI of a podcast feed, use [Podcast Addict](https://podcastaddict.com/) or [GetRSSFeed](https://getrssfeed.com/).

Once the URI is found, downloads can take place by calling:

```shell
joe get podcasts https://feed.uri.here /path/to/directory
```

#### Limitations

- If a file with the same name already exists, the download will be skipped.
- If a request to download fails, it will not retry.
- If the directory doesn't exist, it will fail.
- Each episode will be named based on the date it released.
- Each episode will receive a hash to handle days where multiple episodes were released.

## Future Work

- Create a command that analyzes an iCloud Photo library on MacOS and can delete files that match a criteria. For example, files that are video files and have a resolution lower than 1080p.
