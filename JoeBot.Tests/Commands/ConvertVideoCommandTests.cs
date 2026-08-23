using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Xunit;

namespace JoeBot.Tests.Commands;

public class ConvertVideoCommandTests : CommandTestBase {
  [Fact]
  public void ConvertVideo_WithValidFile_CallsFfmpeg() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // Setup: ffprobe (has audio), video encode, audio encode, mux
    ProcessRunner.SetupNextResult(0, "1,6,eng"); // ffprobe finds one audio stream
    ProcessRunner.SetupNextResult(0, "", "frame=100 fps=30 time=00:00:10");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0);

    // Verify ffmpeg was called for each stage with correct arguments
    ProcessRunner.Calls.Should().HaveCount(4);
    var (probeFileName, _, _) = ProcessRunner.Calls[0];
    probeFileName.Should().Be("ffprobe");

    var (videoFileName, videoArguments, _) = ProcessRunner.Calls[1];
    videoFileName.Should().Be("ffmpeg");
    videoArguments.Should().Contain("-i");
    videoArguments.Should().Contain(inputPath);
    videoArguments.Should().Contain("-c:v libx264"); // default codec

    // The source track is 6-channel (5.1), so a stereo downmix should be
    // added automatically alongside the original-layout track.
    var (_, audioArguments, _) = ProcessRunner.Calls[2];
    audioArguments.Should().Contain("-filter:a:0 \"aformat=channel_layouts=mono|stereo|5.1|7.1\""); // original layout
    audioArguments.Should().Contain("-filter:a:1 \"aformat=channel_layouts=stereo\""); // downmix

    var (_, muxArguments, _) = ProcessRunner.Calls[3];
    muxArguments.Should().Contain(outputPath);
    muxArguments.Should().Contain("-map 2:s?"); // copy subtitles from original without re-encoding
    muxArguments.Should().Contain("-c copy");
    muxArguments.Should().Contain("-metadata:s:a:1 title=\"Stereo\"");
    muxArguments.Should().Contain("-metadata:s:a:1 language=eng");
    muxArguments.Should().Contain("-disposition:a:1 default"); // stereo downmix is the default track
    muxArguments.Should().Contain("-disposition:a:0 0"); // original multichannel track is no longer default

    // Verify success message
    Console.Lines.Should().Contain("Video conversion completed successfully.");
  }

  [Fact]
  public void ConvertVideo_WithAlreadyStereoSource_DoesNotAddRedundantDownmix() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // Source track is already stereo (2 channels) - no downmix needed
    ProcessRunner.SetupNextResult(0, "1,2,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0);

    var (_, audioArguments, _) = ProcessRunner.Calls[2];
    audioArguments.Should().Contain("-filter:a:0 \"aformat=channel_layouts=mono|stereo|5.1|7.1\"");
    audioArguments.Should().NotContain("-filter:a:1"); // no second (downmix) stream

    var (_, muxArguments, _) = ProcessRunner.Calls[3];
    muxArguments.Should().NotContain("title=\"Stereo\"");
    muxArguments.Should().Contain("-disposition:a:0 default"); // already-stereo track stays default
  }

  [Fact]
  public void ConvertVideo_WithTwoMultichannelTracks_AddsMatchingDownmixPerLanguage() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // An English 7.1 track and a Turkish 5.1 track
    ProcessRunner.SetupNextResult(0, "1,8,eng\n2,6,tur");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0);

    var (_, audioArguments, _) = ProcessRunner.Calls[2];
    audioArguments.Should().Contain("-map 0:1");
    audioArguments.Should().Contain("-map 0:2");
    // Output order: eng original(0), eng downmix(1), tur original(2), tur downmix(3)
    audioArguments.Should().Contain("-filter:a:0 \"aformat=channel_layouts=mono|stereo|5.1|7.1\"");
    audioArguments.Should().Contain("-filter:a:1 \"aformat=channel_layouts=stereo\"");
    audioArguments.Should().Contain("-filter:a:2 \"aformat=channel_layouts=mono|stereo|5.1|7.1\"");
    audioArguments.Should().Contain("-filter:a:3 \"aformat=channel_layouts=stereo\"");

    var (_, muxArguments, _) = ProcessRunner.Calls[3];
    muxArguments.Should().Contain("-metadata:s:a:1 language=eng");
    muxArguments.Should().Contain("-metadata:s:a:3 language=tur");
    muxArguments.Should().NotContain("-metadata:s:a:0 "); // original tracks are untouched
    muxArguments.Should().NotContain("-metadata:s:a:2 ");

    // Only the first track's stereo downmix (index 1) is default; everything
    // else, including the second language's downmix, is not.
    muxArguments.Should().Contain("-disposition:a:1 default");
    muxArguments.Should().Contain("-disposition:a:0 0");
    muxArguments.Should().Contain("-disposition:a:2 0");
    muxArguments.Should().Contain("-disposition:a:3 0");
  }

  [Fact]
  public void ConvertVideo_WithNonExistentFile_OutputsError() {
    // Arrange
    var inputPath = "/videos/nonexistent.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0); // Command completes but outputs error
    Console.Lines.Should().Contain(line =>
        line.Contains("Error") && line.Contains("does not exist"));

    // Verify ffmpeg was NOT called
    ProcessRunner.Calls.Should().BeEmpty();
  }

  [Fact]
  public void ConvertVideo_WhenFfmpegFails_ReturnsErrorMessage() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // ffprobe succeeds (has audio), then the video stage fails
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(1, "", "Error: codec not found");

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0); // Command itself completes
    Console.Lines.Should().Contain(line => line.Contains("ffmpeg exited with code 1"));
  }

  [Fact]
  public void ConvertVideo_WithNoAudioStream_SkipsAudioStageAndMuxesVideoOnly() {
    // Arrange
    var inputPath = "/videos/silent.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // ffprobe finds no audio streams (empty stdout), then video encode, then mux
    ProcessRunner.SetupNextResult(0, "");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0);

    // No audio stage is run - only ffprobe, video encode, and mux
    ProcessRunner.Calls.Should().HaveCount(3);
    var (_, muxArguments, _) = ProcessRunner.Calls[2];
    muxArguments.Should().NotContain("tmpaudio");
    muxArguments.Should().Contain("-map 0:v");
    muxArguments.Should().Contain("-map 1:s?");

    Console.Lines.Should().Contain("Video conversion completed successfully.");
  }

  [Fact]
  public void ConvertVideo_WhenAudioStageFailsAfterVideoSucceeds_PreservesVideoAndReusesItOnRetry() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";
    var tempVideoPath = $"{outputPath}.tmpvideo.mkv";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // First attempt: ffprobe succeeds, video succeeds, audio fails
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(1, "", "audio encode error");

    // Act - first attempt
    var firstResult = RunCommand("convert", "video", inputPath, outputPath);

    // Assert - failed, but the completed video encode was not discarded
    firstResult.Should().Be(0); // command itself completes
    Console.Lines.Should().Contain(line => line.Contains($"ffmpeg exited with code 1"));
    ProcessRunner.Calls.Should().HaveCount(3);

    // Simulate ffmpeg having actually written the video-only temp file
    FileSystem.AddFile(tempVideoPath, new MockFileData("video-only content"));

    // Second attempt: ffprobe succeeds, audio now succeeds, mux succeeds -
    // video stage should be skipped entirely since tempVideo already exists
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act - retry
    var secondResult = RunCommand("convert", "video", inputPath, outputPath);

    // Assert - video stage was skipped (only 3 more calls: ffprobe, audio, mux)
    secondResult.Should().Be(0);
    ProcessRunner.Calls.Should().HaveCount(6);
    Console.Lines.Should().Contain(line => line.Contains("Reusing video encode"));

    var secondAttemptCalls = ProcessRunner.Calls.Skip(3);
    secondAttemptCalls.Should().NotContain(
      call => call.Arguments.Contains("tmpvideo") && call.Arguments.Contains("-c:v"),
      "the video stage should not run again on retry");
  }

  [Fact]
  public void ConvertVideo_WithPresetOption_UsesCorrectSettings() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act - use 720p preset
    var result = RunCommand("convert", "video", inputPath, outputPath, "--preset", "720p");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[1];
    arguments.Should().Contain("-crf 22"); // 720p CRF
    arguments.Should().Contain("scale=-2:720"); // 720p scaling
  }

  [Fact]
  public void ConvertVideo_WithInvalidPreset_OutputsError() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--preset", "invalid");

    // Assert
    result.Should().Be(0);
    Console.Lines.Should().Contain(line =>
        line.Contains("Error") && line.Contains("Invalid preset"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  [Fact]
  public void ConvertVideo_WithHevcCodec_UsesLibx265() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--codec", "hevc");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[1];
    arguments.Should().Contain("-c:v libx265");
  }

  [Fact]
  public void ConvertVideo_WithThreadsOption_PassesThreadCount() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--threads", "8");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[1];
    arguments.Should().Contain("-threads 8");
  }

  [Fact]
  public void ConvertVideo_WithGpuFlag_UsesVideotoolbox() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act - default preset 1080p, default codec h264
    var result = RunCommand("convert", "video", inputPath, outputPath, "--gpu");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[1];
    arguments.Should().Contain("-hwaccel videotoolbox");
    arguments.Should().Contain("-c:v h264_videotoolbox");
    arguments.Should().Contain("-b:v 8M"); // Plex 1080p preset
    arguments.Should().Contain("scale_vt");
    arguments.Should().Contain("h=1080");
    arguments.Should().NotContain("-preset");
    arguments.Should().NotContain("-crf");
  }

  [Fact]
  public void ConvertVideo_WithGpuAnd720pPreset_UsesScaleVt() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--gpu", "--preset", "720p");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[1];
    arguments.Should().Contain("-hwaccel videotoolbox");
    arguments.Should().Contain("scale_vt");
    arguments.Should().Contain("h=720");
    arguments.Should().NotContain("scale=-2:720");
  }

  [Fact]
  public void ConvertVideo_WithGpuAndHevc_UsesHevcVideotoolbox() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--gpu", "--codec", "hevc");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[1];
    arguments.Should().Contain("-c:v hevc_videotoolbox");
  }

  [Fact]
  public void ConvertVideo_WithBulk_TwoPairs_BothSucceed() {
    // Arrange
    FileSystem.AddDirectory("/videos");
    FileSystem.AddDirectory("/lists");
    FileSystem.AddFile("/videos/a.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/videos/b.mkv", new MockFileData("video b"));
    FileSystem.AddFile("/lists/in.txt", new MockFileData("/videos/a.mkv\n/videos/b.mkv"));
    FileSystem.AddFile("/lists/out.txt", new MockFileData("/videos/a.mp4\n/videos/b.mp4"));

    // Bulk conversions run in parallel with zero synchronization, so calls from
    // both pairs can interleave in any order. Results are keyed by which
    // executable is being run rather than relying on FIFO queue position, so
    // the outcome doesn't depend on interleaving.
    ProcessRunner.SetupResultFor((fileName, _) => fileName == "ffprobe", 0, "1,6,eng");
    ProcessRunner.SetupResultFor((fileName, _) => fileName == "ffmpeg", 0);

    // Act
    var result = RunCommand("convert", "video", "/lists/in.txt", "/lists/out.txt", "--bulk");

    // Assert
    result.Should().Be(0);
    ProcessRunner.Calls.Should().HaveCount(8); // 2 conversions x (ffprobe + 3 ffmpeg stages) each
    Console.Lines.Should().Contain("All 2 conversion(s) completed successfully.");
  }

  [Fact]
  public void ConvertVideo_WithBulk_LineCountMismatch_OutputsErrorAndNoFfmpegCalls() {
    FileSystem.AddDirectory("/videos");
    FileSystem.AddDirectory("/lists");
    FileSystem.AddFile("/videos/a.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/lists/in.txt", new MockFileData("/videos/a.mkv\n/videos/b.mkv"));
    FileSystem.AddFile("/lists/out.txt", new MockFileData("/videos/a.mp4"));

    var result = RunCommand("convert", "video", "/lists/in.txt", "/lists/out.txt", "--bulk");

    result.Should().Be(0);
    Console.Lines.Should().Contain(line =>
      line.Contains("Error") && line.Contains("Input list has 2 paths but output list has 1"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  [Fact]
  public void ConvertVideo_WithBulk_MissingInputFileInList_OutputsErrorAndNoFfmpegCalls() {
    FileSystem.AddDirectory("/videos");
    FileSystem.AddDirectory("/lists");
    FileSystem.AddFile("/videos/a.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/lists/in.txt", new MockFileData("/videos/a.mkv\n/videos/nonexistent.mkv"));
    FileSystem.AddFile("/lists/out.txt", new MockFileData("/videos/a.mp4\n/videos/b.mp4"));

    var result = RunCommand("convert", "video", "/lists/in.txt", "/lists/out.txt", "--bulk");

    result.Should().Be(0);
    Console.Lines.Should().Contain(line =>
      line.Contains("Error") && line.Contains("does not exist"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  [Fact]
  public void ConvertVideo_WithBulk_OneFfmpegFails_ExitsWithCode1AndPrintsFailure() {
    FileSystem.AddDirectory("/videos");
    FileSystem.AddDirectory("/lists");
    FileSystem.AddFile("/videos/a.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/videos/b.mkv", new MockFileData("video b"));
    FileSystem.AddFile("/lists/in.txt", new MockFileData("/videos/a.mkv\n/videos/b.mkv"));
    FileSystem.AddFile("/lists/out.txt", new MockFileData("/videos/a.mp4\n/videos/b.mp4"));

    // Bulk conversions run in parallel with zero synchronization, so calls from
    // both pairs interleave in any order. Results are keyed by which pair's
    // video-stage call they match rather than relying on FIFO queue position:
    // b's video-stage call always fails, everything else always succeeds,
    // regardless of interleaving.
    ProcessRunner.SetupResultFor((fileName, args) => fileName == "ffmpeg" && args.Contains("/videos/b.mkv") && args.Contains("tmpvideo"), 1, "", "ffmpeg error");
    ProcessRunner.SetupResultFor((fileName, _) => fileName == "ffprobe", 0, "1,6,eng");
    ProcessRunner.SetupResultFor((fileName, _) => fileName == "ffmpeg", 0);

    var result = RunCommand("convert", "video", "/lists/in.txt", "/lists/out.txt", "--bulk");

    ProcessRunner.Calls.Should().HaveCount(6); // a: ffprobe+video+audio+mux, b: ffprobe+video(fails)
    Console.Lines.Should().Contain(line =>
      line.Contains("Failed") && line.Contains("/videos/b.mkv") && line.Contains("exit 1"));
    Environment.ExitCode.Should().Be(1);
  }

  [Fact]
  public void ConvertVideo_WithBulkAndNoOutput_PrintsError() {
    FileSystem.AddFile("/lists/in.txt", new MockFileData("/videos/a.mkv"));

    var result = RunCommand("convert", "video", "/lists/in.txt", "--bulk");

    result.Should().Be(0);
    Console.Lines.Should().Contain(line => line.Contains("output list path is required"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  // --directory mode: CLI wiring

  [Fact]
  public void ConvertVideo_WithDirectory_NoOutputArgRequired() {
    FileSystem.AddDirectory("/movies");
    FileSystem.AddFile("/movies/Movie.mkv", new MockFileData("video"));
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    var result = RunCommand("convert", "video", "/movies", "--directory");

    result.Should().Be(0);
    Console.Lines.Should().NotContain(line => line.Contains("output path is required"));
  }

  [Fact]
  public void ConvertVideo_WithoutDirectoryAndNoOutput_PrintsError() {
    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile("/videos/input.mkv", new MockFileData("video"));

    var result = RunCommand("convert", "video", "/videos/input.mkv");

    result.Should().Be(0);
    Console.Lines.Should().Contain(line => line.Contains("output path is required"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  // --directory mode: ScanDirectory

  [Fact]
  public void ConvertVideo_WithDirectory_ConvertsAllVideoFiles() {
    FileSystem.AddDirectory("/movies/MovieA");
    FileSystem.AddDirectory("/movies/MovieB");
    FileSystem.AddFile("/movies/MovieA/MovieA.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/movies/MovieB/MovieB.mp4", new MockFileData("video b"));
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    var result = RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    result.Should().Be(0);
    ProcessRunner.Calls.Should().HaveCount(8); // 2 conversions x (ffprobe + 3 ffmpeg stages) each
    Console.Lines.Should().Contain(line => line.Contains("All 2 conversion(s) completed successfully."));
  }

  [Fact]
  public void ConvertVideo_WithDirectory_OutputPathUsesPresetAndFormat() {
    FileSystem.AddDirectory("/movies/Star Wars");
    FileSystem.AddFile("/movies/Star Wars/Star Wars.mkv", new MockFileData("video"));
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    var (_, arguments, _) = ProcessRunner.Calls[3]; // mux stage writes the final output path
    arguments.Should().Contain("Star Wars.1080p.mkv");
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ExcludesAlreadyConvertedOutputs() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("original"));
    FileSystem.AddFile("/movies/Film/Film.1080p.mkv", new MockFileData("already converted"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    // Only Film.mkv should be converted, not Film.1080p.mkv
    ProcessRunner.Calls.Should().HaveCount(4); // 1 conversion x (ffprobe + 3 ffmpeg stages)
    var (_, videoArguments, _) = ProcessRunner.Calls[1];
    videoArguments.Should().Contain("-i \"/movies/Film/Film.mkv\"");
    ProcessRunner.Calls.Should().NotContain(call => call.Arguments.Contains("-i \"/movies/Film/Film.1080p.mkv\""));
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ExcludesAnyKnownPresetSuffix() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("original"));
    FileSystem.AddFile("/movies/Film/Film.720p.mkv", new MockFileData("previous 720p output"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Scanning with 1080p preset — Film.720p.mkv should still be excluded
    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    ProcessRunner.Calls.Should().HaveCount(4); // 1 conversion x (ffprobe + 3 ffmpeg stages)
  }

  [Fact]
  public void ConvertVideo_WithDirectory_IncludesFileWithPresetInNameButNotSuffix() {
    FileSystem.AddDirectory("/movies");
    FileSystem.AddFile("/movies/Star Wars 1080p.mkv", new MockFileData("video"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    // "Star Wars 1080p.mkv" has no dot before "1080p" — should be included
    ProcessRunner.Calls.Should().HaveCount(4); // 1 conversion x (ffprobe + 3 ffmpeg stages)
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ExcludesNonVideoFiles() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("video"));
    FileSystem.AddFile("/movies/Film/Film.nfo", new MockFileData("metadata"));
    FileSystem.AddFile("/movies/Film/poster.jpg", new MockFileData("image"));

    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory");

    ProcessRunner.Calls.Should().HaveCount(4); // 1 conversion x (ffprobe + 3 ffmpeg stages)
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ScansRecursively() {
    FileSystem.AddDirectory("/movies/Series/Season 1");
    FileSystem.AddFile("/movies/Series/Season 1/Episode 1.mkv", new MockFileData("ep1"));
    FileSystem.AddFile("/movies/Series/Season 1/Episode 2.mkv", new MockFileData("ep2"));
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory");

    ProcessRunner.Calls.Should().HaveCount(8); // 2 conversions x (ffprobe + 3 ffmpeg stages) each
  }

  [Fact]
  public void ConvertVideo_WithDirectory_EmptyDirectory_PrintsMessage() {
    FileSystem.AddDirectory("/movies");

    var result = RunCommand("convert", "video", "/movies", "--directory");

    result.Should().Be(0);
    Console.Lines.Should().Contain(line => line.Contains("No video files found"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  [Fact]
  public void ConvertVideo_WithDirectory_NonExistentDirectory_PrintsError() {
    var result = RunCommand("convert", "video", "/nonexistent", "--directory");

    result.Should().Be(0);
    Console.Lines.Should().Contain(line => line.Contains("Error") && line.Contains("does not exist"));
    ProcessRunner.Calls.Should().BeEmpty();
  }

  // --directory mode: RunDirectoryMode failure handling

  [Fact]
  public void ConvertVideo_WithDirectory_OneFails_ExitsWithCode1() {
    FileSystem.AddDirectory("/movies/A");
    FileSystem.AddDirectory("/movies/B");
    FileSystem.AddFile("/movies/A/A.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/movies/B/B.mkv", new MockFileData("video b"));
    // A's ffprobe, video, audio, and mux stages all succeed
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    // B's ffprobe succeeds but its video stage fails, short-circuiting before audio/mux
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(1, "", "encode error");

    RunCommand("convert", "video", "/movies", "--directory");

    Environment.ExitCode.Should().Be(1);
    Console.Lines.Should().Contain(line => line.Contains("Failed") && line.Contains("exit 1"));
  }

  // --directory mode: --delete flag

  [Fact]
  public void ConvertVideo_WithDirectoryAndDelete_RenamesOutputToOriginal() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("original content"));
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Simulate ffmpeg creating the output file
    FileSystem.AddFile("/movies/Film/Film.1080p.mkv", new MockFileData("converted content"));

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv", "--delete");

    // Original should have been replaced by the converted output
    FileSystem.File.Exists("/movies/Film/Film.mkv").Should().BeTrue();
    FileSystem.File.ReadAllText("/movies/Film/Film.mkv").Should().Be("converted content");
    FileSystem.File.Exists("/movies/Film/Film.1080p.mkv").Should().BeFalse();
    Console.Lines.Should().Contain(line => line.Contains("Replaced:") && line.Contains("Film.mkv"));
  }

  [Fact]
  public void ConvertVideo_WithDirectoryAndDelete_FailedConversionOriginalNotDeleted() {
    FileSystem.AddDirectory("/movies/A");
    FileSystem.AddDirectory("/movies/B");
    FileSystem.AddFile("/movies/A/A.mkv", new MockFileData("video a"));
    FileSystem.AddFile("/movies/B/B.mkv", new MockFileData("video b"));

    // A succeeds (ffprobe, video, audio, mux), B's ffprobe succeeds but video stage fails
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(1, "", "encode error");

    // Simulate ffmpeg creating output only for A
    FileSystem.AddFile("/movies/A/A.1080p.mkv", new MockFileData("converted a"));

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv", "--delete");

    // RunDeleteRename is skipped entirely when any conversion fails, so both originals are untouched
    FileSystem.File.Exists("/movies/B/B.mkv").Should().BeTrue();
    Environment.ExitCode.Should().Be(1);
  }

  [Fact]
  public void ConvertVideo_WithDirectoryNoDelete_KeepsBothFiles() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("original"));
    ProcessRunner.SetupNextResult(0, "1,6,eng");
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Simulate ffmpeg creating output
    FileSystem.AddFile("/movies/Film/Film.1080p.mkv", new MockFileData("converted"));

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    // Without --delete both files exist
    FileSystem.File.Exists("/movies/Film/Film.mkv").Should().BeTrue();
    FileSystem.File.Exists("/movies/Film/Film.1080p.mkv").Should().BeTrue();
  }
}
