using System.CommandLine;
using System.Net.NetworkInformation;

namespace JoeBot.Commands;

public static class GetInternetStatusCommand
{
  // Attempt to connect to multiple domains.
  // If they both fail, assume internet connectivity is lost.
  // Exit Code 0: Internet connection success.
  // Exit Code 1: Internet connection failed.
  // Exit Code 2: Internet connection exception.
  public static Command Get()
  {
    var command = new Command("internet-status", "Check internet connectivity.");
    var influxOption = new Option<string>(
      name: "--influx-host",
      description: "Hostname for Influx database. If provided, uploads will be attempted."
    );
    var logOption = new Option<bool>(
      name: "--log",
      description: "Enable logging."
    );
    command.AddOption(influxOption);
    command.AddOption(logOption);
    command.SetHandler((string influxHostname, bool log) =>
    {
      try {
        // Ping both Google and Wikipedia in case one is down.
        var ping = new Ping();
        var primaryHost = "google.com";
        var secondaryHost = "wikipedia.org";
        var buffer = new byte[32];
        var timeout = 1000;
        PingOptions pingOptions = new PingOptions();
        var primaryReply = ping.Send(primaryHost, timeout, buffer, pingOptions);
        var secondaryReply = ping.Send(secondaryHost, timeout, buffer, pingOptions);
        var primaryStatus = primaryReply.Status == IPStatus.Success;
        var secondaryStatus = secondaryReply.Status == IPStatus.Success;
        
        if (!primaryStatus && !secondaryStatus) {
          if (log) Console.WriteLine("Internet connection failure.");
          // Both pings failed.
          Environment.Exit(1);
        }
      } catch (Exception) {
        // Something weird happened.
        if (log) Console.WriteLine("Unknown failure.");
        Environment.Exit(2);
      }
      
      // If execution makes it here, you are connected to the internet.
      if (log) Console.WriteLine("Internet connected.");
      // client.UploadMetricAsync("localhost", "internet", 8086, "status", 0, null);
    },
    influxOption,
    logOption);
    return command;
  }
}