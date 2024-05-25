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
    var rssFeedArg = new Argument<string>("feed", "RSS feed.");
    var filePathArg = new Argument<string>("path", "Path to save file.");
    var command = new Command("podcasts", "Download podcast episodes from RSS feed.");
    command.AddAlias("pod");
    command.AddAlias("pods");
    command.AddAlias("podcast");
    command.AddArgument(rssFeedArg);
    command.AddArgument(filePathArg);
    var limitOption = new Option<int>(
      name: "--limit",
      description: "Limit the number of recent podcasts to download."
    );
    command.AddOption(limitOption);
    command.SetHandler(async (string feed, string path, int limit) =>
    {
      var client = new HttpClient();
      var content = await client.GetStreamAsync(feed);
      var serializer = new XmlSerializer(typeof(RssFeed));
      var deserialized = (RssFeed) serializer.Deserialize(content)!;
      var items = deserialized.Channel.Item;
      var counter = 0;
      foreach (var item in items)
      {
        if (counter >= limit) {
          break;
        }
        counter++;
        var hash = HashString(item.Description);
        var url = item.Enclosure.Url;

        Stream? data;
        try
        {
          data = await client.GetStreamAsync(url);
        }
        catch (Exception)
        {
          Console.WriteLine($"Failed on {url}");
          continue;
        }

        var date = DateTime.Parse(item.PubDate);
        var formDate = date.ToUniversalTime().ToString("yyyy-MM-dd");
        var fullPath = $"{path}/{formDate}-{hash}.mp3";
        try
        {
          await using Stream inStream = File.OpenRead(fullPath);
          Console.WriteLine($"Skipping: {fullPath}");
        }
        catch (FileNotFoundException)
        {
          await using Stream outStream = File.OpenWrite(fullPath);
          await data.CopyToAsync(outStream);
          Console.WriteLine($"Downloaded: {fullPath}");
        }
        catch (IOException) {
          // This can happen when another process is using the file. Example: iCloud Drive syncing a file.
          Console.WriteLine($"Skipping: {fullPath}");
        }
      }
    }, rssFeedArg, filePathArg, limitOption);
    return command;
  }

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