using System.CommandLine;
using JoeBot.Abstractions;

namespace JoeBot.Commands.Convert;

public static class ConvertVideoCommand {
  private static readonly Dictionary<string, PresetSettings> Presets = new() {
    ["480p"] = new PresetSettings(480, 23, "128k"),
    ["720p"] = new PresetSettings(720, 22, "128k"),
    ["1080p"] = new PresetSettings(1080, 21, "160k"),
    ["4K"] = new PresetSettings(2160, 18, "192k")
  };

  private static readonly Dictionary<string, string> Codecs = new() {
    ["h264"] = "libx264",
    ["hevc"] = "libx265"
  };

  private static readonly string[] ValidFormats = { "mkv", "mp4" };

  public static Command Get() {
    var inputArg = new Argument<string>("input") {
      Description = "Path to the input video file"
    };

    var outputArg = new Argument<string>("output") {
      Description = "Path to the output video file"
    };

    var presetOption = new Option<string>("--preset", "-p") {
      Description = "Video quality preset (480p, 720p, 1080p, 4K)",
      DefaultValueFactory = _ => "1080p"
    };

    var formatOption = new Option<string>("--format", "-f") {
      Description = "Container format (mkv, mp4)",
      DefaultValueFactory = _ => "mp4"
    };

    var codecOption = new Option<string>("--codec", "-c") {
      Description = "Video codec (h264, hevc)",
      DefaultValueFactory = _ => "h264"
    };

    var threadsOption = new Option<int>("--threads", "-t") {
      Description = "Number of threads to use for encoding (default: number of processors)",
      DefaultValueFactory = _ => Services.Environment.ProcessorCount
    };

    var command = new Command("video", "Convert a video file using ffmpeg");
    command.Arguments.Add(inputArg);
    command.Arguments.Add(outputArg);
    command.Options.Add(presetOption);
    command.Options.Add(formatOption);
    command.Options.Add(codecOption);
    command.Options.Add(threadsOption);

    command.SetAction(parseResult => {
      var input = parseResult.GetValue<string>("input")!;
      var output = parseResult.GetValue<string>("output")!;
      var preset = parseResult.GetValue<string>("--preset")!;
      var format = parseResult.GetValue<string>("--format")!;
      var codec = parseResult.GetValue<string>("--codec")!;
      var threads = parseResult.GetValue<int>("--threads");

      try {
        var resolvedInput = ResolvePath(input);
        var resolvedOutput = ResolvePath(output);

        if (!Services.FileSystem.File.Exists(resolvedInput)) {
          Services.Console.WriteLine($"Error: Input file '{input}' does not exist.");
          return;
        }

        if (!Presets.TryGetValue(preset, out var presetSettings)) {
          Services.Console.WriteLine($"Error: Invalid preset '{preset}'. Valid presets are: {string.Join(", ", Presets.Keys)}");
          return;
        }

        if (!ValidFormats.Contains(format.ToLower())) {
          Services.Console.WriteLine($"Error: Invalid format '{format}'. Valid formats are: {string.Join(", ", ValidFormats)}");
          return;
        }

        if (!Codecs.TryGetValue(codec.ToLower(), out var codecLib)) {
          Services.Console.WriteLine($"Error: Invalid codec '{codec}'. Valid codecs are: {string.Join(", ", Codecs.Keys)}");
          return;
        }

        Services.Console.WriteLine($"Converting video...");
        Services.Console.WriteLine($"  Input:  {resolvedInput}");
        Services.Console.WriteLine($"  Output: {resolvedOutput}");
        Services.Console.WriteLine($"  Preset: {preset}");
        Services.Console.WriteLine($"  Format: {format}");
        Services.Console.WriteLine($"  Codec:  {codec} ({codecLib})");
        Services.Console.WriteLine();

        var exitCode = ExecuteFfmpeg(resolvedInput, resolvedOutput, presetSettings, format, codecLib, threads);

        if (exitCode == 0) {
          Services.Console.WriteLine();
          Services.Console.WriteLine("Video conversion completed successfully.");
        }
        else {
          Services.Console.WriteLine($"Error: ffmpeg exited with code {exitCode}");
        }
      }
      catch (Exception ex) {
        Services.Console.WriteLine($"Error: {ex.Message}");
      }
    });

    return command;
  }

  private static int ExecuteFfmpeg(string input, string output, PresetSettings settings, string format, string codecLib, int threads) {
    // Build ffmpeg arguments
    // Base: ffmpeg -i input -c:v {codec} -preset slow -crf {crf} -c:a aac -b:a {audioBitrate} -c:s copy output
    // With scaling for non-1080p presets: -vf scale=-2:{height}

    var scaleFilter = settings.Height == 1080 ? "" : $"-vf scale=-2:{settings.Height} ";
    var subtitleCodec = format.ToLower() == "mkv" ? "srt" : "mov_text";

    var arguments = $"-y -i \"{input}\" -c:v {codecLib} -preset slow -crf {settings.Crf} -threads {threads} {scaleFilter}-c:a aac -b:a {settings.AudioBitrate} -c:s {subtitleCodec} \"{output}\"";

    Services.Console.WriteLine($"Running: ffmpeg {arguments}");
    Services.Console.WriteLine();

    var result = Services.ProcessRunner.Run("ffmpeg", arguments);

    // Output any stderr (ffmpeg writes progress to stderr)
    if (!string.IsNullOrEmpty(result.StandardError)) {
      Services.Console.WriteLine(result.StandardError.TrimEnd());
    }

    // Output any stdout
    if (!string.IsNullOrEmpty(result.StandardOutput)) {
      Services.Console.WriteLine(result.StandardOutput.TrimEnd());
    }

    return result.ExitCode;
  }

  private static string ResolvePath(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      throw new ArgumentException("Path cannot be null or empty.", nameof(path));
    }

    if (path.StartsWith("~")) {
      var homeDir = Services.Environment.UserProfilePath;
      path = Services.FileSystem.Path.Combine(homeDir, path.Substring(1).TrimStart(
          Services.FileSystem.Path.DirectorySeparatorChar,
          Services.FileSystem.Path.AltDirectorySeparatorChar));
    }

    return Services.FileSystem.Path.GetFullPath(path);
  }

  private record PresetSettings(int Height, int Crf, string AudioBitrate);
}
