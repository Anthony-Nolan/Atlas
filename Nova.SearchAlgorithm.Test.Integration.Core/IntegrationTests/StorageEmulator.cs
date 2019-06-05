using System.Configuration;
using System.Diagnostics;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    public class StorageEmulator
    {
        private static readonly string StorageEmulatorLocation = ConfigurationManager.AppSettings["emulatorLocation"];

        private static bool? wasRunning;

        public static void Start()
        {
            wasRunning = wasRunning ?? IsRunning();
            if (!(bool) wasRunning)
            {
                ExecuteCommandOnEmulator("start");
            }
        }

        public static void Stop()
        {
            if (wasRunning != null && !(bool) wasRunning)
            {
                ExecuteCommandOnEmulator("stop");
            }
        }

        private static bool IsRunning()
        {
            var output = ExecuteCommandOnEmulator("status");
            return output.Contains("IsRunning: True");
        }

        private static string ExecuteCommandOnEmulator(string arguments)
        {
            var start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = StorageEmulatorLocation,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            var proc = new Process
            {
                StartInfo = start
            };

            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }
    }
}