using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class StorageEmulator
    {
        // TODO:configure this in case it changes from machine to machine
        private const string StorageEmulatorLocation = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";

        public void Start()
        {
            ExecuteCommandOnEmulator("start");
        }

        public void Stop()
        {
            ExecuteCommandOnEmulator("stop");
        }

        public void Clear()
        {
            ExecuteCommandOnEmulator("clear blob table");
        }

        private void ExecuteCommandOnEmulator(string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = StorageEmulatorLocation
            };
            Process proc = new Process
            {
                StartInfo = start
            };

            proc.Start();
            proc.WaitForExit();
        }
    }
}
