using System.CommandLine;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using JoeBot.Models.Rss;

namespace JoeBot.Commands;

public static class GetPodcastCommand
{
  public static Command Get()
  {
    var rssFeedArg = new Argument<string>("feed")
    {
      Description = "RSS feed."
    };
    var filePathArg = new Argument<string>("path")
    {
      Description = "Path to save file."
    };
    var command = new Command("podcasts", "Download podcast episodes from RSS feed.");
    command.Aliases.Add("pod");
    command.Aliases.Add("pods");
    command.Aliases.Add("podcast");
    command.Arguments.Add(rssFeedArg);
    command.Arguments.Add(filePathArg);
    var limitOption = new Option<int>("--limit")
    {
      Description = "Limit the number of recent podcasts to download.",
      DefaultValueFactory = _ => 0
    };
    command.Options.Add(limitOption);
    command.SetAction(async (parseResult, cancellationToken) =>
    {
      var feed = parseResult.GetValue<string>("feed")!;
      var path = parseResult.GetValue<string>("path")!;
      var limit = parseResult.GetValue<int>("--limit");
      
      // Validate inputs
      if (string.IsNullOrEmpty(feed)) throw new ArgumentException("Feed URL cannot be empty", nameof(feed));
      if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be empty", nameof(path));
      if (limit < 0) throw new ArgumentException("Limit must be non-negative", nameof(limit));

      // Create directory if it doesn't exist
      Directory.CreateDirectory(path);

      var unlimitedDownloads = limit == 0;

      // Use static HttpClient to follow best practices
      using var client = new HttpClient();
      RssFeed deserialized;

      try
      {
        using var content = await client.GetStreamAsync(feed, cancellationToken).ConfigureAwait(false);
        var serializer = new XmlSerializer(typeof(RssFeed));
        deserialized = (RssFeed)serializer.Deserialize(content)!;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to fetch or parse feed: {ex.Message}");
        return;
      }

      var items = deserialized.Channel.Item;
      var counter = 0;

      foreach (var item in items)
      {
        if (!unlimitedDownloads && counter >= limit)
        {
          break;
        }
        counter++;

        var hash = HashString(item.Description);
        var url = item.Enclosure.Url;

        try
        {
          using var data = await client.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
          var date = DateTime.Parse(item.PubDate);
          var formDate = date.ToUniversalTime().ToString("yyyy-MM-dd");
          var fullPath = Path.Combine(path, $"{formDate}-{hash}.mp3");

          try
          {
            // Check if file already exists.
            await using Stream inStream = File.OpenRead(fullPath);
            Console.WriteLine($"Skipping: {fullPath}");
          }
          catch (FileNotFoundException)
          {
            // File does not exist, download it.
            await using Stream outStream = File.OpenWrite(fullPath);
            await data.CopyToAsync(outStream, cancellationToken);
            Console.WriteLine($"Downloaded: {fullPath}");
          }
          catch (IOException)
          {
            // This can happen when another process is using the file. Example: iCloud Drive syncing a file.
            Console.WriteLine($"Skipping: {fullPath}. Another process is using the file.");
          }
        }
        catch (Exception)
        {
          Console.WriteLine($"Failed on {url}");
          continue;
        }
      }
    });
    return command;
  }

  /// <summary>
  /// Generates a 5-character hash from the input string using MD5. Can be used to generate a reproducible hash for a
  /// given string.
  /// Note: This is used for filename generation only, not for security purposes.
  /// </summary>
  /// <param name="message">The string to be hashed</param>
  /// <returns>A 5-character uppercase hexadecimal string</returns>
  private static string HashString(string message)
  {
    using var md5 = MD5.Create();
    var input = Encoding.ASCII.GetBytes(message);
    var hash = md5.ComputeHash(input);
    var sb = new StringBuilder();
    foreach (var val in hash) sb.Append(val.ToString("X2"));
    return sb.ToString().ToUpper().Substring(0, 5);
  }
}