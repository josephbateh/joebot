using System.CommandLine;
using System.Diagnostics;
using System.Text;

namespace JoeBot.Commands;

public static class ExecuteScriptCommand
{
  public static Command Get()
  {
    var filePathArg = new Argument<string>("path")
    {
      Description = "Path to script."
    };
    var command = new Command("script", "Execute script(s) in parallel. Output the results to separate files.");
    command.Aliases.Add("scripts");
    command.Arguments.Add(filePathArg);
    command.SetAction(parseResult =>
    {
      var path = parseResult.GetValue<string>("path")!;
      
      // Set up process
      var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      var processStartInfo = new ProcessStartInfo {
        FileName = path,
        Arguments = "",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
      };
      var process = new Process();
      var processOutput = new StringBuilder();
      process.StartInfo = processStartInfo;
      
      // Start process
      process.Start();
      Console.WriteLine("Process started.");

      // Create new combined stream
      var combinedStream = new MemoryStream();
      
      // Wait for process to exit
      Console.WriteLine("Waiting for process to exit...");
      process.WaitForExit();
      Console.WriteLine("Process has exited.");
      
      // Copy the StandardOutput stream to the combined stream
      process.StandardOutput.BaseStream.CopyTo(combinedStream);
      
      // Copy the StandardError stream to the combined stream
      process.StandardError.BaseStream.CopyTo(combinedStream);

      // Get streamreader
      var combinedStreamReader = new StreamReader(combinedStream);

      processOutput.Append(combinedStreamReader.ReadToEnd());
      // Get output
      // while (combinedStream.CanRead) {
      //   // var line = combinedStreamReader.ReadLine();
      //   // Console.WriteLine(line);
      //   processOutput.Append(combinedStreamReader.ReadToEnd());
      // }
      
      // Write output to files
      File.WriteAllText($"output-{startTime}.txt", processOutput.ToString());
    });
    return command;
  }
}