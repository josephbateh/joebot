using System.CommandLine;
using System.Diagnostics;

namespace JoeBot.Commands.Convert;

public static class ConvertVideoCommand {
  private const string AudioBitrate = "192k";
  private const int StereoDownmixChannelThreshold = 2;

  private static readonly Dictionary<string, PresetSettings> Presets = new() {
    ["480p"] = new PresetSettings(480, 23, "1.5M"),
    ["720p"] = new PresetSettings(720, 22, "4M"),
    ["1080p-low"] = new PresetSettings(1080, 22, "4M"),
    ["1080p"] = new PresetSettings(1080, 21, "8M"),
    ["4K"] = new PresetSettings(2160, 18, "20M")
  };

  private static readonly Dictionary<string, string> Codecs = new() {
    ["h264"] = "libx264",
    ["hevc"] = "libx265"
  };

  private static readonly Dictionary<string, string> GpuCodecs = new() {
    ["h264"] = "h264_videotoolbox",
    ["hevc"] = "hevc_videotoolbox"
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
      Description = "Video quality preset (480p, 720p, 1080p-low, 1080p, 4K)",
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
      DefaultValueFactory = _ => Environment.ProcessorCount
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
          Console.WriteLine($"Error: Invalid preset '{preset}'. Valid presets are: {string.Join(", ", Presets.Keys)}");
          return;
        }

        if (!ValidFormats.Contains(format.ToLower())) {
          Console.WriteLine($"Error: Invalid format '{format}'. Valid formats are: {string.Join(", ", ValidFormats)}");
          return;
        }

        var codecLib = gpu && GpuCodecs.TryGetValue(codec.ToLower(), out var gpuEncoder)
          ? gpuEncoder
          : (Codecs.TryGetValue(codec.ToLower(), out var cpuEncoder) ? cpuEncoder : null);
        if (codecLib == null) {
          Console.WriteLine($"Error: Invalid codec '{codec}'. Valid codecs are: {string.Join(", ", Codecs.Keys)}");
          return;
        }

        if (directory) {
          RunDirectoryMode(input, presetSettings, preset, format, codecLib, threads, gpu, jobs, delete);
          return;
        }

        if (bulk) {
          if (output == null) {
            Console.WriteLine("Error: An output list path is required when using --bulk mode.");
            return;
          }
          RunBulkMode(input, output, presetSettings, format, codecLib, threads, gpu);
          return;
        }

        if (output == null) {
          Console.WriteLine("Error: An output path is required when not using --directory mode.");
          return;
        }

        var resolvedInput = ResolvePath(input);
        var resolvedOutput = ResolvePath(output);

        if (!File.Exists(resolvedInput)) {
          Console.WriteLine($"Error: Input file '{input}' does not exist.");
          return;
        }

        Console.WriteLine($"Converting video...");
        Console.WriteLine($"  Input:  {resolvedInput}");
        Console.WriteLine($"  Output: {resolvedOutput}");
        Console.WriteLine($"  Preset: {preset}");
        Console.WriteLine($"  Format: {format}");
        Console.WriteLine($"  Codec:  {codec} ({codecLib})");
        Console.WriteLine();

        var exitCode = ExecuteFfmpeg(resolvedInput, resolvedOutput, presetSettings, format, codecLib, threads, gpu, null);

        if (exitCode == 0) {
          Console.WriteLine();
          Console.WriteLine("Video conversion completed successfully.");
        }
        else {
          Console.WriteLine($"Error: ffmpeg exited with code {exitCode}");
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error: {ex.Message}");
      }
    });

    return command;
  }

  private static void RunBulkMode(string inputListPath, string outputListPath, PresetSettings presetSettings, string format, string codecLib, int threads, bool gpu) {
    var resolvedListInput = ResolvePath(inputListPath);
    var resolvedListOutput = ResolvePath(outputListPath);

    if (!File.Exists(resolvedListInput)) {
      Console.WriteLine($"Error: Input list file '{inputListPath}' does not exist.");
      return;
    }

    if (!File.Exists(resolvedListOutput)) {
      Console.WriteLine($"Error: Output list file '{outputListPath}' does not exist.");
      return;
    }

    var inputPaths = ParseListFile(resolvedListInput);
    var outputPaths = ParseListFile(resolvedListOutput);

    if (inputPaths.Count != outputPaths.Count) {
      Console.WriteLine($"Error: Input list has {inputPaths.Count} paths but output list has {outputPaths.Count}. Counts must match.");
      return;
    }

    for (var i = 0; i < inputPaths.Count; i++) {
      if (!File.Exists(inputPaths[i])) {
        Console.WriteLine($"Error: Input file '{inputPaths[i]}' (line {i + 1}) does not exist.");
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
        Console.WriteLine($"Failed: {resInput} -> {resOutput} (exit {exitCode})");
      }
    }

    if (anyFailed) {
      Environment.Exit(1);
    }
    else {
      Console.WriteLine($"All {results.Length} conversion(s) completed successfully.");
    }
  }

  private static void RunDirectoryMode(string dirPath, PresetSettings presetSettings, string preset, string format, string codecLib, int threads, bool gpu, int jobs, bool delete) {
    var resolvedDir = ResolvePath(dirPath);

    if (!Directory.Exists(resolvedDir)) {
      Console.WriteLine($"Error: Directory '{dirPath}' does not exist.");
      return;
    }

    var pairs = ScanDirectory(resolvedDir, preset, format);

    if (pairs.Count == 0) {
      Console.WriteLine($"No video files found in '{dirPath}'.");
      return;
    }

    Console.WriteLine($"Found {pairs.Count} video file(s):");
    foreach (var (input, output) in pairs) {
      Console.WriteLine($"  {input} -> {output}");
    }
    Console.WriteLine();

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
        Console.WriteLine($"Failed: {resInput} -> {resOutput} (exit {exitCode})");
      }
      else {
        successfulPairs.Add((resInput, resOutput));
      }
    }

    if (anyFailed) {
      Console.WriteLine($"{successfulPairs.Count} of {results.Length} conversion(s) completed successfully.");
      Environment.Exit(1);
    }
    else {
      Console.WriteLine($"All {results.Length} conversion(s) completed successfully.");
      if (delete) {
        RunDeleteRename(successfulPairs);
      }
    }
  }

  private static List<(string Input, string Output)> ScanDirectory(string dirPath, string preset, string format) {
    var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

    return files
      .Where(f => VideoExtensions.Contains(Path.GetExtension(f)))
      .Where(f => !Presets.Keys.Any(p =>
        Path.GetFileNameWithoutExtension(f)
          .EndsWith("." + p, StringComparison.OrdinalIgnoreCase)))
      .Select(f => {
        var dir = Path.GetDirectoryName(f)!;
        var baseName = Path.GetFileNameWithoutExtension(f);
        var output = Path.Combine(dir, $"{baseName}.{preset}.{format}");
        return (Input: f, Output: output);
      })
      .ToList();
  }

  private static void RunDeleteRename(IEnumerable<(string Input, string Output)> pairs) {
    foreach (var (input, output) in pairs) {
      try {
        File.Delete(input);
        File.Move(output, input);
        Console.WriteLine($"Replaced: {input}");
      }
      catch (Exception ex) {
        Console.WriteLine($"Error replacing '{input}': {ex.Message}");
      }
    }
  }

  private static List<string> ParseListFile(string path) {
    var lines = File.ReadAllLines(path);
    return lines
      .Select(line => line.Trim())
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .Select(ResolvePath)
      .ToList();
  }

  private static int ExecuteFfmpeg(string input, string output, PresetSettings settings, string format, string codecLib, int threads, bool useGpu, object? consoleLock) {
    void WriteLine(string line) {
      if (consoleLock != null) {
        lock (consoleLock) {
          Console.WriteLine(line);
        }
      }
      else {
        Console.WriteLine(line);
      }
    }

    int RunFfmpeg(string arguments) {
      WriteLine($"Running: ffmpeg {arguments}");
      WriteLine(string.Empty);

      var result = RunProcess(
        "ffmpeg",
        arguments,
        onStderrLine: line => WriteLine(line));

      if (!string.IsNullOrEmpty(result.StandardOutput)) {
        WriteLine(result.StandardOutput.TrimEnd());
      }

      return result.ExitCode;
    }

    // Video and audio are encoded in separate ffmpeg processes and muxed together
    // afterward. Encoding both in a single process can starve the audio encoder
    // under sustained heavy video encoding on long files, silently truncating or
    // dropping audio output with no error and a successful exit code.
    var tempVideo = $"{output}.tmpvideo.mkv";
    var tempAudio = $"{output}.tmpaudio.mkv";
    var audioTracks = GetAudioTracks(input);
    var hasAudio = audioTracks.Count > 0;
    var videoStageValid = false;
    var succeeded = false;

    try {
      // If a previous attempt's video encode is still sitting here, reuse it
      // rather than redoing the most expensive stage - it's only left behind
      // when that attempt got past video but failed on audio/mux. This assumes
      // a retry uses the same input and preset as the run that produced it.
      if (File.Exists(tempVideo)) {
        WriteLine($"Reusing video encode from a previous attempt: {tempVideo}");
        videoStageValid = true;
      }
      else if (IsAlreadyAtOrBelowTarget(input, settings)) {
        // Input is already at or below this preset's target resolution and
        // bitrate (e.g. re-running against a file this same command already
        // produced, to pick up a new feature). Re-encoding it again would
        // cost time for no quality benefit and would actually lose quality
        // to a second generation of lossy compression, so just copy it.
        WriteLine("Input is already at or below the target resolution/bitrate for this preset - copying video without re-encoding.");
        var copyExitCode = RunFfmpeg($"-y -i \"{input}\" -map 0:v -c:v copy \"{tempVideo}\"");
        if (copyExitCode != 0) {
          return copyExitCode;
        }
        videoStageValid = true;
      }
      else {
        string videoArguments;
        if (useGpu) {
          var videoBitrate = settings.GpuBitrate;
          var scaleFilter = $"-vf scale_vt=w=iw*{settings.Height}/ih:h={settings.Height}";
          videoArguments = $"-y -hwaccel videotoolbox -hwaccel_output_format videotoolbox_vld -i \"{input}\" -map 0:v -c:v {codecLib} -b:v {videoBitrate} {scaleFilter} \"{tempVideo}\"";
        }
        else {
          var scaleFilter = $"-vf scale=-2:{settings.Height}";
          videoArguments = $"-y -i \"{input}\" -map 0:v -c:v {codecLib} -preset slow -crf {settings.Crf} -threads {threads} {scaleFilter} \"{tempVideo}\"";
        }

        var videoExitCode = RunFfmpeg(videoArguments);
        if (videoExitCode != 0) {
          return videoExitCode;
        }
        videoStageValid = true;
      }

      // For each source audio track, produce its normal AAC track plus - when
      // the source is multichannel (5.1/7.1) - an extra stereo downmix so
      // stereo-only playback devices get a properly mixed track (blending
      // center/LFE/surrounds into L/R) instead of relying on the player to
      // downmix a multichannel track itself, which not all of them do well.
      // The stereo track for the first source track becomes the default for
      // playback; every other track (including the original multichannel
      // ones) is kept but no longer default.
      var outputTracks = new List<(bool IsDownmix, string Language)>();
      var defaultTrackIndex = -1;
      if (hasAudio) {
        var audioArgs = new List<string> { $"-y -i \"{input}\"" };
        for (var t = 0; t < audioTracks.Count; t++) {
          var track = audioTracks[t];
          var originalOutIndex = outputTracks.Count;
          audioArgs.Add($"-map 0:{track.Index}");
          audioArgs.Add($"-filter:a:{originalOutIndex} \"aformat=channel_layouts=mono|stereo|5.1|7.1\"");
          audioArgs.Add($"-c:a:{originalOutIndex} aac -b:a:{originalOutIndex} {AudioBitrate}");
          outputTracks.Add((false, track.Language));

          if (track.Channels > StereoDownmixChannelThreshold) {
            var downmixOutIndex = outputTracks.Count;
            audioArgs.Add($"-map 0:{track.Index}");
            audioArgs.Add($"-filter:a:{downmixOutIndex} \"aformat=channel_layouts=stereo\"");
            audioArgs.Add($"-c:a:{downmixOutIndex} aac -b:a:{downmixOutIndex} {AudioBitrate}");
            outputTracks.Add((true, track.Language));

            if (t == 0) {
              defaultTrackIndex = downmixOutIndex;
            }
          }
          else if (t == 0) {
            defaultTrackIndex = originalOutIndex; // already stereo/mono - no downmix needed
          }
        }
        audioArgs.Add($"\"{tempAudio}\"");

        var audioExitCode = RunFfmpeg(string.Join(' ', audioArgs));
        if (audioExitCode != 0) {
          return audioExitCode;
        }
      }

      string muxArguments;
      if (hasAudio) {
        var muxArgs = new List<string> {
          $"-y -i \"{tempVideo}\" -i \"{tempAudio}\" -i \"{input}\" -map 0:v -map 1:a -map 2:s? -c copy"
        };
        for (var i = 0; i < outputTracks.Count; i++) {
          if (outputTracks[i].IsDownmix) {
            muxArgs.Add($"-metadata:s:a:{i} title=\"Stereo\"");
            if (!string.IsNullOrWhiteSpace(outputTracks[i].Language)) {
              muxArgs.Add($"-metadata:s:a:{i} language={outputTracks[i].Language}");
            }
          }
          muxArgs.Add(i == defaultTrackIndex ? $"-disposition:a:{i} default" : $"-disposition:a:{i} 0");
        }
        muxArgs.Add($"\"{output}\"");
        muxArguments = string.Join(' ', muxArgs);
      }
      else {
        muxArguments = $"-y -i \"{tempVideo}\" -i \"{input}\" -map 0:v -map 1:s? -c copy \"{output}\"";
      }

      var muxExitCode = RunFfmpeg(muxArguments);
      succeeded = muxExitCode == 0;
      return muxExitCode;
    }
    finally {
      // tempVideo is deleted once its content has safely landed in the final
      // muxed output, or if this attempt's own video stage failed (leaving a
      // partial/invalid file that must not be mistaken for a reusable one next
      // time). It's preserved only when video succeeded but a later stage
      // didn't, so a retry can skip re-encoding it. tempAudio is cheap to redo,
      // so it's always cleaned up.
      if ((succeeded || !videoStageValid) && File.Exists(tempVideo)) {
        File.Delete(tempVideo);
      }
      if (File.Exists(tempAudio)) {
        File.Delete(tempAudio);
      }
    }
  }

  // Allows a real encoding variance margin above the nominal target bitrate
  // before deciding a re-encode is actually needed - GPU bitrate targets are
  // an average, not a hard cap, so a file this same preset already produced
  // can legitimately land a bit above its nominal target.
  private const double BitrateToleranceFactor = 1.15;

  private static bool IsAlreadyAtOrBelowTarget(string input, PresetSettings settings) {
    var currentHeight = GetVideoHeight(input);
    var currentBitRate = GetOverallBitRate(input);
    var targetBitRate = ParseBitrateToBps(settings.GpuBitrate);

    return currentHeight.HasValue && currentHeight.Value <= settings.Height
      && currentBitRate.HasValue && targetBitRate.HasValue
      && currentBitRate.Value <= targetBitRate.Value * BitrateToleranceFactor;
  }

  private static int? GetVideoHeight(string input) {
    var result = RunProcess(
      "ffprobe",
      $"-v error -select_streams v:0 -show_entries stream=height -of csv=p=0 \"{input}\"");

    return result.ExitCode == 0 && int.TryParse(result.StandardOutput.Trim(), out var height) ? height : null;
  }

  private static long? GetOverallBitRate(string input) {
    var result = RunProcess(
      "ffprobe",
      $"-v error -show_entries format=bit_rate -of csv=p=0 \"{input}\"");

    return result.ExitCode == 0 && long.TryParse(result.StandardOutput.Trim(), out var bitRate) ? bitRate : null;
  }

  private static long? ParseBitrateToBps(string bitrate) {
    if (string.IsNullOrWhiteSpace(bitrate)) {
      return null;
    }

    var multiplier = 1d;
    var numberPart = bitrate;
    if (bitrate.EndsWith("M", StringComparison.OrdinalIgnoreCase)) {
      multiplier = 1_000_000d;
      numberPart = bitrate[..^1];
    }
    else if (bitrate.EndsWith("K", StringComparison.OrdinalIgnoreCase)) {
      multiplier = 1_000d;
      numberPart = bitrate[..^1];
    }

    return double.TryParse(numberPart, out var value) ? (long)(value * multiplier) : null;
  }

  private static List<(int Index, int Channels, string Language)> GetAudioTracks(string input) {
    var result = RunProcess(
      "ffprobe",
      $"-v error -select_streams a -show_entries stream=index,channels:stream_tags=language -of csv=p=0 \"{input}\"");

    if (result.ExitCode != 0) {
      return [];
    }

    var tracks = new List<(int Index, int Channels, string Language)>();
    foreach (var line in result.StandardOutput.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)) {
      var parts = line.Split(',');
      if (parts.Length < 2 || !int.TryParse(parts[0], out var index) || !int.TryParse(parts[1], out var channels)) {
        continue;
      }

      var language = parts.Length > 2 ? parts[2] : "";
      tracks.Add((index, channels, language));
    }

    return tracks;
  }

  // ffmpeg/ffprobe write progress and results to stdout/stderr; onStderrLine
  // lets callers stream stderr live (e.g. for console progress output) while
  // both streams are still fully captured for the caller to inspect after exit.
  private static ProcessResult RunProcess(string fileName, string arguments, Action<string>? onStderrLine = null) {
    var processStartInfo = new ProcessStartInfo {
      FileName = fileName,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = new Process();
    process.StartInfo = processStartInfo;

    var stdout = new System.Text.StringBuilder();
    var stderr = new System.Text.StringBuilder();

    process.OutputDataReceived += (_, e) => {
      if (e.Data != null) {
        stdout.AppendLine(e.Data);
      }
    };

    process.ErrorDataReceived += (_, e) => {
      if (e.Data != null) {
        stderr.AppendLine(e.Data);
        onStderrLine?.Invoke(e.Data);
      }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
  }

  private static string ResolvePath(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      throw new ArgumentException("Path cannot be null or empty.", nameof(path));
    }

    if (path.StartsWith("~")) {
      var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      path = Path.Combine(homeDir, path.Substring(1).TrimStart(
          Path.DirectorySeparatorChar,
          Path.AltDirectorySeparatorChar));
    }

    return Path.GetFullPath(path);
  }

  private record PresetSettings(int Height, int Crf, string GpuBitrate);

  private record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
