using System.Diagnostics;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    public interface IStorageEmulator
    {
        void Start();
        void Stop();
    }

    public class StorageEmulator : IStorageEmulator
    {
        private readonly string storageEmulatorLocation;

        private bool? wasRunning;

        public StorageEmulator(string storageEmulatorLocation)
        {
            this.storageEmulatorLocation = storageEmulatorLocation;
        }
        
        public void Start()
        {
            wasRunning = wasRunning ?? IsRunning();
            if (!(bool) wasRunning)
            {
                ExecuteCommandOnEmulator("start");
            }
        }

        public void Stop()
        {
            if (wasRunning != null && !(bool) wasRunning)
            {
                ExecuteCommandOnEmulator("stop");
            }
        }

        private bool IsRunning()
        {
            var output = ExecuteCommandOnEmulator("status");
            return output.Contains("IsRunning: True");
        }

        private string ExecuteCommandOnEmulator(string arguments)
        {
            var start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = storageEmulatorLocation,
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