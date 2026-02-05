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
}
