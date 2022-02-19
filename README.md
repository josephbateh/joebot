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

```shell
cd JoeBot
./Scripts/build.sh
./Scripts/local-install-osx-x64.sh
```

## Usage

### Podcast Downloads

To find the URI of a podcast feed, use [Podcast Addict](https://podcastaddict.com/) or [GetRSSFeed](https://getrssfeed.com/).

Once the URI is found, downloads can take place by calling: 

```shell
joe get podcasts https://feed.uri.here /path/to/directory
```

#### Limitations

- If two episodes were released on the same day, only one will be downloaded.
- If a file with the same name already exists, the download will be skipped.
- If a request to download fails, it will not retry.
- Each episode will be named based on the date it released.
