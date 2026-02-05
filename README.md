# JoeBot

Code that automates tasks normally done by Joseph Bateh.

## Installation

Requirements:

- .NET 10 SDK

### Linux

```shell
cd JoeBot
./Scripts/build.sh
./Scripts/local-install-linux-x64.sh
```

### MacOS

Install .NET 10: [download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

OR

```shell
brew install dotnet@10
echo 'export PATH="/opt/homebrew/opt/dotnet@10/bin:$PATH"' >> ~/.zshrc
```

Build and install JoeBot:

```shell
cd JoeBot
./Scripts/build.sh
./Scripts/local-install-osx.sh
```

## Development

Run commands through the dotnet CLI during development:

```shell
cd JoeBot
dotnet run -- -h                           # Show help
dotnet run -- get -h                       # Show get command help
dotnet run -- convert video -h             # Show convert video help
```

## Usage

### Podcast Downloads

Download podcast episodes from an RSS feed.

To find the URI of a podcast feed, use [Podcast Addict](https://podcastaddict.com/) or
[GetRSSFeed](https://getrssfeed.com/).

```shell
joe get podcasts <feed-uri> <directory>
```

Examples:

```shell
joe get podcasts https://feed.uri.here /path/to/directory

# Development mode
dotnet run -- get podcasts https://feed.uri.here /path/to/directory
```

Limitations:

- If a file with the same name already exists, the download will be skipped.
- If a request to download fails, it will not retry.
- If the directory doesn't exist, it will fail.
- Each episode will be named based on the date it released.
- Each episode will receive a hash to handle days where multiple episodes were released.

### Internet Status

Check internet connectivity by pinging Google and Wikipedia.

```shell
joe get internet-status [options]
```

Options:

- `--influx-host` - Hostname for Influx database (optional, for metric uploads)
- `--log` - Enable logging output

Exit codes:

- `0` - Internet connection successful
- `1` - Internet connection failed (both pings failed)
- `2` - Unknown exception occurred

Examples:

```shell
joe get internet-status --log

# Development mode
dotnet run -- get internet-status --log
```

### Script Execution

Execute a script and save the output to a file.

```shell
joe execute script <path>
```

The output (stdout and stderr) is saved to `output-{timestamp}.txt` in the current directory.

Examples:

```shell
joe execute script ./my-script.sh

# Development mode
dotnet run -- execute script ./my-script.sh
```

### File Rename

Rename files in a directory based on their creation date. Files are renamed to the format
`YYYY-MM-DD-{hash}.{extension}` where the hash is generated from the original filename.

```shell
joe rename <directory>
```

Examples:

```shell
joe rename ~/Downloads/photos
joe rename /path/to/files

# Development mode
dotnet run -- rename ~/Downloads/photos
```

### Video Conversion

Convert video files using ffmpeg with various presets, codecs, and formats.

Requirements:

- ffmpeg installed and available in PATH

```shell
joe convert video <input> <output> [options]
```

Options:

- `-p, --preset` - Quality preset: 480p, 720p, 1080p (default), 4K
- `-f, --format` - Container format: mkv, mp4 (default)
- `-c, --codec` - Video codec: h264 (default), hevc

Examples:

```shell
# Default conversion (1080p, mp4, h264)
joe convert video input.avi output.mp4

# Convert to MKV with HEVC codec at 4K
joe convert video input.mov output.mkv -f mkv -c hevc -p 4K

# Convert to 720p with default codec and format
joe convert video input.mp4 output.mp4 -p 720p

# Development mode (via dotnet CLI)
dotnet run -- convert video input.mp4 output.mkv -f mkv -c hevc -p 1080p
```

## Future Work

- Create a command that analyzes an iCloud Photo library on MacOS and can delete files that match a criteria. For
  example, files that are video files and have a resolution lower than 1080p.
