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

    // Setup ffmpeg to succeed
    ProcessRunner.SetupNextResult(0, "", "frame=100 fps=30 time=00:00:10");

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0);

    // Verify ffmpeg was called with correct arguments
    ProcessRunner.Calls.Should().HaveCount(1);
    var (fileName, arguments, _) = ProcessRunner.Calls[0];
    fileName.Should().Be("ffmpeg");
    arguments.Should().Contain("-i");
    arguments.Should().Contain(inputPath);
    arguments.Should().Contain(outputPath);
    arguments.Should().Contain("-c:v libx264"); // default codec
    arguments.Should().Contain("-c:s copy"); // copy subtitles without re-encoding

    // Verify success message
    Console.Lines.Should().Contain("Video conversion completed successfully.");
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

    // Setup ffmpeg to fail
    ProcessRunner.SetupNextResult(1, "", "Error: codec not found");

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath);

    // Assert
    result.Should().Be(0); // Command itself completes
    Console.Lines.Should().Contain(line => line.Contains("ffmpeg exited with code 1"));
  }

  [Fact]
  public void ConvertVideo_WithPresetOption_UsesCorrectSettings() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0);

    // Act - use 720p preset
    var result = RunCommand("convert", "video", inputPath, outputPath, "--preset", "720p");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[0];
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

    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--codec", "hevc");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[0];
    arguments.Should().Contain("-c:v libx265");
  }

  [Fact]
  public void ConvertVideo_WithThreadsOption_PassesThreadCount() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--threads", "8");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[0];
    arguments.Should().Contain("-threads 8");
  }

  [Fact]
  public void ConvertVideo_WithGpuFlag_UsesVideotoolbox() {
    // Arrange
    var inputPath = "/videos/input.mkv";
    var outputPath = "/videos/output.mp4";

    FileSystem.AddDirectory("/videos");
    FileSystem.AddFile(inputPath, new MockFileData("video content"));

    ProcessRunner.SetupNextResult(0);

    // Act - default preset 1080p, default codec h264
    var result = RunCommand("convert", "video", inputPath, outputPath, "--gpu");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[0];
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

    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--gpu", "--preset", "720p");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[0];
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

    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", inputPath, outputPath, "--gpu", "--codec", "hevc");

    // Assert
    result.Should().Be(0);

    var (_, arguments, _) = ProcessRunner.Calls[0];
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

    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    // Act
    var result = RunCommand("convert", "video", "/lists/in.txt", "/lists/out.txt", "--bulk");

    // Assert
    result.Should().Be(0);
    ProcessRunner.Calls.Should().HaveCount(2);
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

    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(1, "", "ffmpeg error");

    var result = RunCommand("convert", "video", "/lists/in.txt", "/lists/out.txt", "--bulk");

    ProcessRunner.Calls.Should().HaveCount(2);
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
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    var result = RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    result.Should().Be(0);
    ProcessRunner.Calls.Should().HaveCount(2);
    Console.Lines.Should().Contain(line => line.Contains("All 2 conversion(s) completed successfully."));
  }

  [Fact]
  public void ConvertVideo_WithDirectory_OutputPathUsesPresetAndFormat() {
    FileSystem.AddDirectory("/movies/Star Wars");
    FileSystem.AddFile("/movies/Star Wars/Star Wars.mkv", new MockFileData("video"));
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    var (_, arguments, _) = ProcessRunner.Calls[0];
    arguments.Should().Contain("Star Wars.1080p.mkv");
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ExcludesAlreadyConvertedOutputs() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("original"));
    FileSystem.AddFile("/movies/Film/Film.1080p.mkv", new MockFileData("already converted"));

    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    // Only Film.mkv should be converted, not Film.1080p.mkv
    ProcessRunner.Calls.Should().HaveCount(1);
    var (_, arguments, _) = ProcessRunner.Calls[0];
    arguments.Should().NotContain("Film.1080p.mkv\" -c:v");
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ExcludesAnyKnownPresetSuffix() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("original"));
    FileSystem.AddFile("/movies/Film/Film.720p.mkv", new MockFileData("previous 720p output"));

    ProcessRunner.SetupNextResult(0);

    // Scanning with 1080p preset — Film.720p.mkv should still be excluded
    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    ProcessRunner.Calls.Should().HaveCount(1);
  }

  [Fact]
  public void ConvertVideo_WithDirectory_IncludesFileWithPresetInNameButNotSuffix() {
    FileSystem.AddDirectory("/movies");
    FileSystem.AddFile("/movies/Star Wars 1080p.mkv", new MockFileData("video"));

    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    // "Star Wars 1080p.mkv" has no dot before "1080p" — should be included
    ProcessRunner.Calls.Should().HaveCount(1);
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ExcludesNonVideoFiles() {
    FileSystem.AddDirectory("/movies/Film");
    FileSystem.AddFile("/movies/Film/Film.mkv", new MockFileData("video"));
    FileSystem.AddFile("/movies/Film/Film.nfo", new MockFileData("metadata"));
    FileSystem.AddFile("/movies/Film/poster.jpg", new MockFileData("image"));

    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory");

    ProcessRunner.Calls.Should().HaveCount(1);
  }

  [Fact]
  public void ConvertVideo_WithDirectory_ScansRecursively() {
    FileSystem.AddDirectory("/movies/Series/Season 1");
    FileSystem.AddFile("/movies/Series/Season 1/Episode 1.mkv", new MockFileData("ep1"));
    FileSystem.AddFile("/movies/Series/Season 1/Episode 2.mkv", new MockFileData("ep2"));
    ProcessRunner.SetupNextResult(0);
    ProcessRunner.SetupNextResult(0);

    RunCommand("convert", "video", "/movies", "--directory");

    ProcessRunner.Calls.Should().HaveCount(2);
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
    ProcessRunner.SetupNextResult(0);
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

    // A succeeds, B fails
    ProcessRunner.SetupNextResult(0);
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
    ProcessRunner.SetupNextResult(0);

    // Simulate ffmpeg creating output
    FileSystem.AddFile("/movies/Film/Film.1080p.mkv", new MockFileData("converted"));

    RunCommand("convert", "video", "/movies", "--directory", "--preset", "1080p", "--format", "mkv");

    // Without --delete both files exist
    FileSystem.File.Exists("/movies/Film/Film.mkv").Should().BeTrue();
    FileSystem.File.Exists("/movies/Film/Film.1080p.mkv").Should().BeTrue();
  }
}
