using System.CommandLine;

namespace JoeBot.Commands;

public static class GetInternetStatusCommand {
  // Attempt to connect to multiple domains.
  // If they both fail, assume internet connectivity is lost.
  // Exit Code 0: Internet connection success.
  // Exit Code 1: Internet connection failed.
  // Exit Code 2: Internet connection exception.
  public static Command Get() {
    var command = new Command("internet-status", "Check internet connectivity.");
    var influxOption = new Option<string>("--influx-host") {
      Description = "Hostname for Influx database. If provided, uploads will be attempted."
    };
    var logOption = new Option<bool>("--log") {
      Description = "Enable logging."
    };
    command.Options.Add(influxOption);
    command.Options.Add(logOption);
    command.SetAction(parseResult => {
      var influxHostname = parseResult.GetValue<string>("--influx-host");
      var log = parseResult.GetValue<bool>("--log");


      try {
        // Ping both Google and Wikipedia in case one is down.
        var primaryHost = "google.com";
        var secondaryHost = "wikipedia.org";
        var timeout = 1000;

        var primaryStatus = Services.NetworkPing.CanReach(primaryHost, timeout);
        var secondaryStatus = Services.NetworkPing.CanReach(secondaryHost, timeout);

        if (!primaryStatus && !secondaryStatus) {
          if (log) Services.Console.WriteLine("Internet connection failure.");
          // Both pings failed.
          Services.Environment.Exit(1);
          return;
        }

        if (primaryStatus && secondaryStatus) {
          if (log) Services.Console.WriteLine("Internet connected.");
        }
        else {
          // One of the hosts is reachable but not both
          var reachableHost = primaryStatus ? primaryHost : secondaryHost;
          if (log) Services.Console.WriteLine($"Partial connectivity. Only {reachableHost} is reachable.");
        }
      }
      catch (Exception) {
        // Something weird happened.
        if (log) Services.Console.WriteLine("Unknown failure.");
        Services.Environment.Exit(2);
      }
    });
    return command;
  }
}
