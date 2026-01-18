using System.Reflection;
using System.Runtime.InteropServices;

namespace Buddy.Cli.AgentRuntime;

public class EnvironmentLoader {
    public static AgentEnvironment Load() {
        ;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var workingDirectory = System.Environment.CurrentDirectory;
        var currentDate = DateTimeOffset.Now;
        var osEnvironment = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        return new AgentEnvironment(version, workingDirectory, currentDate, osEnvironment);
    }
}