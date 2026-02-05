using System.CommandLine;
using System.Text;

namespace JoeBot.Commands;

public static class ExecuteScriptCommand {
  public static Command Get() {
    var filePathArg = new Argument<string>("path") {
      Description = "Path to script."
    };
    var command = new Command("script", "Execute script(s) in parallel. Output the results to separate files.");
    command.Aliases.Add("scripts");
    command.Arguments.Add(filePathArg);
    command.SetAction(parseResult => {
      var path = parseResult.GetValue<string>("path")!;

      // Get start time for output filename
      var startTime = Services.Time.GetUtcNow().ToUnixTimeMilliseconds();

      Services.Console.WriteLine("Process started.");

      // Run the script using process runner
      var result = Services.ProcessRunner.Run(path, "");

      Services.Console.WriteLine("Waiting for process to exit...");
      Services.Console.WriteLine("Process has exited.");

      // Combine stdout and stderr
      var processOutput = new StringBuilder();
      processOutput.Append(result.StandardOutput);
      processOutput.Append(result.StandardError);

      // Write output to file
      var outputFileName = $"output-{startTime}.txt";
      Services.FileSystem.File.WriteAllText(outputFileName, processOutput.ToString());

      Services.Console.WriteLine($"Output written to: {outputFileName}");
    });
    return command;
  }
}
