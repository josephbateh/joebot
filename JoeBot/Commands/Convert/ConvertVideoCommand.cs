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

  private static readonly Dictionary<string, string> GpuCodecs = new() {
    ["h264"] = "h264_videotoolbox",
    ["hevc"] = "hevc_videotoolbox"
  };

  // Plex-style bitrates for GPU encoding (VideoToolbox)
  private static readonly Dictionary<int, string> GpuBitrates = new() {
    [480] = "1.5M",
    [720] = "4M",
    [1080] = "8M",
    [2160] = "20M"
  };

  private static readonly string[] ValidFormats = { "mkv", "mp4" };

  private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase) {
    ".mkv", ".mp4", ".avi", ".mov", ".m4v", ".ts", ".wmv"
  };

  public static Command Get() {
    var inputArg = new Argument<string>("input") {
      Description = "Path to the input video file (or directory when using --directory)"
    };

    var outputArg = new Argument<string?>("output") {
      Description = "Path to the output video file (not required when using --directory)",
      Arity = ArgumentArity.ZeroOrOne
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

    var gpuOption = new Option<bool>("--gpu") {
      Description = "Use GPU encoding (VideoToolbox on macOS)",
      Arity = ArgumentArity.ZeroOrOne,
      DefaultValueFactory = _ => false
    };

    var bulkOption = new Option<bool>("--bulk") {
      Description = "Treat input and output as paths to text files containing one video path per line (one conversion per pair, in parallel)",
      Arity = ArgumentArity.ZeroOrOne,
      DefaultValueFactory = _ => false
    };

    var directoryOption = new Option<bool>("--directory") {
      Description = "Treat input as a directory to scan recursively for video files to convert",
      Arity = ArgumentArity.ZeroOrOne,
      DefaultValueFactory = _ => false
    };

    var deleteOption = new Option<bool>("--delete") {
      Description = "After all conversions succeed, delete originals and rename outputs to the original filenames",
      Arity = ArgumentArity.ZeroOrOne,
      DefaultValueFactory = _ => false
    };

    var jobsOption = new Option<int>("--jobs", "-j") {
      Description = "Maximum number of parallel conversions when using --directory (default: 1)",
      DefaultValueFactory = _ => 1
    };

    var command = new Command("video", "Convert a video file using ffmpeg");
    command.Arguments.Add(inputArg);
    command.Arguments.Add(outputArg);
    command.Options.Add(presetOption);
    command.Options.Add(formatOption);
    command.Options.Add(codecOption);
    command.Options.Add(threadsOption);
    command.Options.Add(gpuOption);
    command.Options.Add(bulkOption);
    command.Options.Add(directoryOption);
    command.Options.Add(deleteOption);
    command.Options.Add(jobsOption);

    command.SetAction(parseResult => {
      var input = parseResult.GetValue<string>("input")!;
      var output = parseResult.GetValue<string?>("output");
      var preset = parseResult.GetValue<string>("--preset")!;
      var format = parseResult.GetValue<string>("--format")!;
      var codec = parseResult.GetValue<string>("--codec")!;
      var threads = parseResult.GetValue<int>("--threads");
      var gpu = parseResult.GetValue<bool>("--gpu");
      var bulk = parseResult.GetValue<bool>("--bulk");
      var directory = parseResult.GetValue<bool>("--directory");
      var delete = parseResult.GetValue<bool>("--delete");
      var jobs = parseResult.GetValue<int>("--jobs");

      try {
        if (!Presets.TryGetValue(preset, out var presetSettings)) {
          Services.Console.WriteLine($"Error: Invalid preset '{preset}'. Valid presets are: {string.Join(", ", Presets.Keys)}");
          return;
        }

        if (!ValidFormats.Contains(format.ToLower())) {
          Services.Console.WriteLine($"Error: Invalid format '{format}'. Valid formats are: {string.Join(", ", ValidFormats)}");
          return;
        }

        var codecLib = gpu && GpuCodecs.TryGetValue(codec.ToLower(), out var gpuEncoder)
          ? gpuEncoder
          : (Codecs.TryGetValue(codec.ToLower(), out var cpuEncoder) ? cpuEncoder : null);
        if (codecLib == null) {
          Services.Console.WriteLine($"Error: Invalid codec '{codec}'. Valid codecs are: {string.Join(", ", Codecs.Keys)}");
          return;
        }

        if (directory) {
          RunDirectoryMode(input, presetSettings, preset, format, codecLib, threads, gpu, jobs, delete);
          return;
        }

        if (bulk) {
          if (output == null) {
            Services.Console.WriteLine("Error: An output list path is required when using --bulk mode.");
            return;
          }
          RunBulkMode(input, output, presetSettings, format, codecLib, threads, gpu);
          return;
        }

        if (output == null) {
          Services.Console.WriteLine("Error: An output path is required when not using --directory mode.");
          return;
        }

        var resolvedInput = ResolvePath(input);
        var resolvedOutput = ResolvePath(output);

        if (!Services.FileSystem.File.Exists(resolvedInput)) {
          Services.Console.WriteLine($"Error: Input file '{input}' does not exist.");
          return;
        }

        Services.Console.WriteLine($"Converting video...");
        Services.Console.WriteLine($"  Input:  {resolvedInput}");
        Services.Console.WriteLine($"  Output: {resolvedOutput}");
        Services.Console.WriteLine($"  Preset: {preset}");
        Services.Console.WriteLine($"  Format: {format}");
        Services.Console.WriteLine($"  Codec:  {codec} ({codecLib})");
        Services.Console.WriteLine();

        var exitCode = ExecuteFfmpeg(resolvedInput, resolvedOutput, presetSettings, format, codecLib, threads, gpu, null);

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

  private static void RunBulkMode(string inputListPath, string outputListPath, PresetSettings presetSettings, string format, string codecLib, int threads, bool gpu) {
    var resolvedListInput = ResolvePath(inputListPath);
    var resolvedListOutput = ResolvePath(outputListPath);

    if (!Services.FileSystem.File.Exists(resolvedListInput)) {
      Services.Console.WriteLine($"Error: Input list file '{inputListPath}' does not exist.");
      return;
    }

    if (!Services.FileSystem.File.Exists(resolvedListOutput)) {
      Services.Console.WriteLine($"Error: Output list file '{outputListPath}' does not exist.");
      return;
    }

    var inputPaths = ParseListFile(resolvedListInput);
    var outputPaths = ParseListFile(resolvedListOutput);

    if (inputPaths.Count != outputPaths.Count) {
      Services.Console.WriteLine($"Error: Input list has {inputPaths.Count} paths but output list has {outputPaths.Count}. Counts must match.");
      return;
    }

    for (var i = 0; i < inputPaths.Count; i++) {
      if (!Services.FileSystem.File.Exists(inputPaths[i])) {
        Services.Console.WriteLine($"Error: Input file '{inputPaths[i]}' (line {i + 1}) does not exist.");
        return;
      }
    }

    var pairs = inputPaths.Zip(outputPaths, (inp, outp) => (Input: inp, Output: outp)).ToList();
    var consoleLock = new object();

    var tasks = pairs.Select(pair =>
      Task.Run(() => (
        pair.Input,
        pair.Output,
        ExitCode: ExecuteFfmpeg(pair.Input, pair.Output, presetSettings, format, codecLib, threads, gpu, consoleLock)
      ))).ToList();

    var results = Task.WhenAll(tasks).GetAwaiter().GetResult();

    var anyFailed = false;
    foreach (var (resInput, resOutput, exitCode) in results) {
      if (exitCode != 0) {
        anyFailed = true;
        Services.Console.WriteLine($"Failed: {resInput} -> {resOutput} (exit {exitCode})");
      }
    }

    if (anyFailed) {
      Services.Environment.Exit(1);
    }
    else {
      Services.Console.WriteLine($"All {results.Length} conversion(s) completed successfully.");
    }
  }

  private static void RunDirectoryMode(string dirPath, PresetSettings presetSettings, string preset, string format, string codecLib, int threads, bool gpu, int jobs, bool delete) {
    var resolvedDir = ResolvePath(dirPath);

    if (!Services.FileSystem.Directory.Exists(resolvedDir)) {
      Services.Console.WriteLine($"Error: Directory '{dirPath}' does not exist.");
      return;
    }

    var pairs = ScanDirectory(resolvedDir, preset, format);

    if (pairs.Count == 0) {
      Services.Console.WriteLine($"No video files found in '{dirPath}'.");
      return;
    }

    Services.Console.WriteLine($"Found {pairs.Count} video file(s):");
    foreach (var (input, output) in pairs) {
      Services.Console.WriteLine($"  {input} -> {output}");
    }
    Services.Console.WriteLine();

    var consoleLock = new object();
    using var semaphore = new SemaphoreSlim(jobs, jobs);

    var tasks = pairs.Select(pair =>
      Task.Run(() => {
        semaphore.Wait();
        try {
          return (
            pair.Input,
            pair.Output,
            ExitCode: ExecuteFfmpeg(pair.Input, pair.Output, presetSettings, format, codecLib, threads, gpu, consoleLock)
          );
        }
        finally {
          semaphore.Release();
        }
      })).ToList();

    var results = Task.WhenAll(tasks).GetAwaiter().GetResult();

    var anyFailed = false;
    var successfulPairs = new List<(string Input, string Output)>();

    foreach (var (resInput, resOutput, exitCode) in results) {
      if (exitCode != 0) {
        anyFailed = true;
        Services.Console.WriteLine($"Failed: {resInput} -> {resOutput} (exit {exitCode})");
      }
      else {
        successfulPairs.Add((resInput, resOutput));
      }
    }

    if (anyFailed) {
      Services.Console.WriteLine($"{successfulPairs.Count} of {results.Length} conversion(s) completed successfully.");
      Services.Environment.Exit(1);
    }
    else {
      Services.Console.WriteLine($"All {results.Length} conversion(s) completed successfully.");
      if (delete) {
        RunDeleteRename(successfulPairs);
      }
    }
  }

  private static List<(string Input, string Output)> ScanDirectory(string dirPath, string preset, string format) {
    var files = Services.FileSystem.Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

    return files
      .Where(f => VideoExtensions.Contains(Services.FileSystem.Path.GetExtension(f)))
      .Where(f => !Presets.Keys.Any(p =>
        Services.FileSystem.Path.GetFileNameWithoutExtension(f)
          .EndsWith("." + p, StringComparison.OrdinalIgnoreCase)))
      .Select(f => {
        var dir = Services.FileSystem.Path.GetDirectoryName(f)!;
        var baseName = Services.FileSystem.Path.GetFileNameWithoutExtension(f);
        var output = Services.FileSystem.Path.Combine(dir, $"{baseName}.{preset}.{format}");
        return (Input: f, Output: output);
      })
      .ToList();
  }

  private static void RunDeleteRename(IEnumerable<(string Input, string Output)> pairs) {
    foreach (var (input, output) in pairs) {
      try {
        Services.FileSystem.File.Delete(input);
        Services.FileSystem.File.Move(output, input);
        Services.Console.WriteLine($"Replaced: {input}");
      }
      catch (Exception ex) {
        Services.Console.WriteLine($"Error replacing '{input}': {ex.Message}");
      }
    }
  }

  private static List<string> ParseListFile(string path) {
    var lines = Services.FileSystem.File.ReadAllLines(path);
    return lines
      .Select(line => line.Trim())
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .Select(ResolvePath)
      .ToList();
  }

  private static int ExecuteFfmpeg(string input, string output, PresetSettings settings, string format, string codecLib, int threads, bool useGpu, object? consoleLock) {
    var videoAudioSubs = $"-c:a aac -b:a {settings.AudioBitrate} -c:s copy \"{output}\"";

    string arguments;
    if (useGpu) {
      var videoBitrate = GpuBitrates.TryGetValue(settings.Height, out var br) ? br : "8M";
      var scaleFilter = $"-vf scale_vt=w=iw*{settings.Height}/ih:h={settings.Height} ";
      arguments = $"-y -hwaccel videotoolbox -hwaccel_output_format videotoolbox_vld -i \"{input}\" -c:v {codecLib} -b:v {videoBitrate} {scaleFilter}{videoAudioSubs}";
    }
    else {
      var scaleFilter = $"-vf scale=-2:{settings.Height} ";
      arguments = $"-y -i \"{input}\" -c:v {codecLib} -preset slow -crf {settings.Crf} -threads {threads} {scaleFilter}{videoAudioSubs}";
    }

    void WriteLine(string line) {
      if (consoleLock != null) {
        lock (consoleLock) {
          Services.Console.WriteLine(line);
        }
      }
      else {
        Services.Console.WriteLine(line);
      }
    }

    WriteLine($"Running: ffmpeg {arguments}");
    WriteLine(string.Empty);

    var result = Services.ProcessRunner.Run(
      "ffmpeg",
      arguments,
      onStderrLine: line => WriteLine(line));

    if (!string.IsNullOrEmpty(result.StandardOutput)) {
      WriteLine(result.StandardOutput.TrimEnd());
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
