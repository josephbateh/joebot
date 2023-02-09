using System.CommandLine;
using System.Diagnostics;
using System.Text;

namespace JoeBot.Commands;

public static class ExecuteScriptCommand
{
  public static Command Get()
  {
    var filePathArg = new Argument<string>("path", "Path to script.");
    var command = new Command("script", "Execute script(s) in parallel. Output the results to separate files.");
    command.AddAlias("scripts");
    command.AddArgument(filePathArg);
    command.SetHandler((string path) =>
    {
      var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      var processStartInfo = new ProcessStartInfo {
        FileName = path,
        Arguments = "",
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };
      var process = new Process();
      var standardOutput = new StringBuilder();
      var standardError = new StringBuilder();;
      process.StartInfo = processStartInfo;
      
      // Start process and wait for completion
      process.Start();
      process.WaitForExit();

      // Get output
      standardOutput.Append(process.StandardOutput.ReadToEnd());
      standardError.Append(process.StandardError.ReadToEnd());
      
      // Write output to files
      File.WriteAllText($"standard-output-{startTime}.txt", standardOutput.ToString());
      File.WriteAllText($"standard-error-{startTime}.txt", standardError.ToString());
    }, filePathArg);
    return command;
  }
}