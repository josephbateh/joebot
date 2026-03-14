using System.Net;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace JoeBot.Tests.Commands;

public class GetPodcastCommandTests : CommandTestBase {
  private const string SampleRssFeed = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Test Podcast</title>
                <item>
                    <title>Episode 1</title>
                    <description>First episode description</description>
                    <pubDate>Mon, 15 Jan 2024 10:00:00 GMT</pubDate>
                    <enclosure url="http://example.com/episode1.mp3" type="audio/mpeg" />
                </item>
                <item>
                    <title>Episode 2</title>
                    <description>Second episode description</description>
                    <pubDate>Tue, 16 Jan 2024 10:00:00 GMT</pubDate>
                    <enclosure url="http://example.com/episode2.mp3" type="audio/mpeg" />
                </item>
                <item>
                    <title>Episode 3</title>
                    <description>Third episode description</description>
                    <pubDate>Wed, 17 Jan 2024 10:00:00 GMT</pubDate>
                    <enclosure url="http://example.com/episode3.mp3" type="audio/mpeg" />
                </item>
            </channel>
        </rss>
        """;

  [Fact]
  public async Task GetPodcast_WithValidFeed_DownloadsEpisodes() {
    // Arrange
    var feedUrl = "http://example.com/feed.rss";
    var targetPath = "/podcasts";

    HttpHandler.When(feedUrl).Respond("application/xml", SampleRssFeed);
    HttpHandler.When("http://example.com/episode1.mp3").Respond("audio/mpeg", "audio content 1");
    HttpHandler.When("http://example.com/episode2.mp3").Respond("audio/mpeg", "audio content 2");
    HttpHandler.When("http://example.com/episode3.mp3").Respond("audio/mpeg", "audio content 3");

    // Act
    var result = await RunCommandAsync("get", "podcasts", feedUrl, targetPath);

    // Assert
    result.Should().Be(0);

    // Check that directory was created and files were downloaded
    FileSystem.Directory.Exists(targetPath).Should().BeTrue();
    var files = FileSystem.Directory.GetFiles(targetPath);
    files.Should().HaveCount(3);

    // Verify download messages
    Console.Lines.Should().Contain(line => line.Contains("Downloaded:"));
  }

  [Fact]
  public async Task GetPodcast_WithExistingFile_SkipsDownload() {
    // Arrange
    var feedUrl = "http://example.com/feed.rss";
    var targetPath = "/podcasts";

    HttpHandler.When(feedUrl).Respond("application/xml", SampleRssFeed);
    HttpHandler.When("http://example.com/episode1.mp3").Respond("audio/mpeg", "audio content 1");
    HttpHandler.When("http://example.com/episode2.mp3").Respond("audio/mpeg", "audio content 2");
    HttpHandler.When("http://example.com/episode3.mp3").Respond("audio/mpeg", "audio content 3");

    // Pre-create one of the files (Episode 1 hash based on "First episode description" - 871DB)
    FileSystem.AddDirectory(targetPath);
    FileSystem.AddFile("/podcasts/2024-01-15-871DB.mp3", "existing content");

    // Act
    var result = await RunCommandAsync("get", "podcasts", feedUrl, targetPath);

    // Assert
    result.Should().Be(0);

    // Verify that the existing file was skipped
    Console.Lines.Should().Contain(line => line.Contains("Skipping:"));

    // Should still download the other episodes
    Console.Lines.Count(line => line.Contains("Downloaded:")).Should().Be(2);
  }

  [Fact]
  public async Task GetPodcast_WithLimit_OnlyDownloadsSpecifiedCount() {
    // Arrange
    var feedUrl = "http://example.com/feed.rss";
    var targetPath = "/podcasts";

    HttpHandler.When(feedUrl).Respond("application/xml", SampleRssFeed);
    HttpHandler.When("http://example.com/episode1.mp3").Respond("audio/mpeg", "audio content 1");
    HttpHandler.When("http://example.com/episode2.mp3").Respond("audio/mpeg", "audio content 2");

    // Act - limit to 2 episodes
    var result = await RunCommandAsync("get", "podcasts", feedUrl, targetPath, "--limit", "2");

    // Assert
    result.Should().Be(0);

    // Check that only 2 files were downloaded
    var files = FileSystem.Directory.GetFiles(targetPath);
    files.Should().HaveCount(2);

    // Verify download messages - should have exactly 2
    Console.Lines.Count(line => line.Contains("Downloaded:")).Should().Be(2);
  }

  [Fact]
  public async Task GetPodcast_WithInvalidFeed_OutputsError() {
    // Arrange
    var feedUrl = "http://example.com/invalid.rss";
    var targetPath = "/podcasts";

    // Mock returns invalid XML that will fail to parse
    HttpHandler.When(feedUrl).Respond("application/xml", "not valid xml");

    // Act
    var result = await RunCommandAsync("get", "podcasts", feedUrl, targetPath);

    // Assert
    result.Should().Be(0); // Command succeeds but outputs error message
    Console.Lines.Should().Contain(line => line.Contains("Failed to fetch or parse feed"));
  }
}
