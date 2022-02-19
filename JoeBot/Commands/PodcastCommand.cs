using System.CommandLine;
using System.Xml.Serialization;
using JoeBot.Models.Rss;

namespace JoeBot.Commands;

public static class PodcastCommand
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
    command.SetHandler(async (string feed, string path) =>
    {
      var client = new HttpClient();
      var content = await client.GetStreamAsync(feed);
      var serializer = new XmlSerializer(typeof(RssFeed));
      var deserialized = (RssFeed) serializer.Deserialize(content)!;
      var items = deserialized.Channel.Item;
      foreach (var item in items)
      {
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
        var fullPath = $"{path}/{formDate}.mp3";
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
      }
    }, rssFeedArg, filePathArg);
    return command;
  }
}