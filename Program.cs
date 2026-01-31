using System;
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;

using System.IO;

namespace ProjectStarkCS
{
    class Program
    {
        private static Config _config;
        private static SpeechLogic _speech;
        private static AudioMonitor _audioMonitor;
        private static bool _waitingForClaps = false;
        private static Timer _timeoutTimer;

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Project Stark (Jarvis Protocol)...");
            // EnsureStartup(); // Disabled to prevent Registry/Startup folder conflicts. Using Startup folder only.

            // 1. Load Config
            _config = AppLauncher.LoadConfig();

            // 2. Initialize Components
            _speech = new SpeechLogic(_config.WakeWord);
            _audioMonitor = new AudioMonitor(_config.ClapThreshold);

            // 3. Wire Events
            _speech.OnWakeWordDetected += Speech_OnWakeWordDetected;
            _audioMonitor.OnDoubleClapDetected += AudioMonitor_OnDoubleClapDetected;

            // 4. Start Listening
            _speech.StartListening();

            Console.WriteLine("System Online. Say the wake word to arm.");
            
            // Keep app alive
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Speech_OnWakeWordDetected(object sender, EventArgs e)
        {
            if (_waitingForClaps) return; // Already armed

            Console.WriteLine(">>> SYSTEM ARMED. CLAP TWICE TO EXECUTE <<<");
            _waitingForClaps = true;

            // Stop Speech to free up audio (optional, but good practice + prevents self-triggering)
            // _speech.StopListening(); 
            // Note: Parallel operation might work depending on driver. 
            // Let's try running AudioMonitor alongside. 
            // If it crashes, we will need to stop speech first.
            
            _audioMonitor.Start();

            // Set a timeout (e.g., 5 seconds) to reset if no claps occur
            _timeoutTimer = new Timer(OnTimeout, null, 5000, Timeout.Infinite);
        }

        private static void AudioMonitor_OnDoubleClapDetected(object sender, EventArgs e)
        {
            if (!_waitingForClaps) return;

            Console.WriteLine("Authentication Confirmed.");
            _timeoutTimer?.Dispose();
            _audioMonitor.Stop();
            _waitingForClaps = false;

            Console.WriteLine($"Standby for deployment in {_config.LaunchDelaySeconds}s...");
            Thread.Sleep(_config.LaunchDelaySeconds * 1000);

            // EXECUTE PROTOCOL
            AppLauncher.ExecuteProtocol();

            Console.WriteLine("Protocol Complete. Resuming Listen Mode...");
            // _speech.StartListening(); // If we stopped it
        }

        private static void OnTimeout(object state)
        {
            if (_waitingForClaps)
            {
                Console.WriteLine("Timeout. System Disarmed.");
                _audioMonitor.Stop();
                _waitingForClaps = false;
                // _speech.StartListening(); // If we stopped it
            }
            _timeoutTimer?.Dispose();
        }

        private static void EnsureStartup()
        {
            try
            {
                string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    string appName = "ProjectStark";
                    string currentExePath = GetExecutablePath();

                    // Check if we need to update the key
                    object existingVal = key.GetValue(appName);
                    if (existingVal == null || existingVal.ToString() != currentExePath)
                    {
                        key.SetValue(appName, currentExePath);
                        Console.WriteLine($"Added/Updated {appName} in startup: {currentExePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set startup registry: {ex.Message}");
            }
        }

        private static string GetExecutablePath()
        {
            // Best effort to find the actual executable
            var processModule = Process.GetCurrentProcess().MainModule;
            string fileName = processModule?.FileName;

            // If running via 'dotnet run', we get 'dotnet.exe'. simpler check:
            if (fileName != null && Path.GetFileNameWithoutExtension(fileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                // We are likely in dev mode. Try to find the build output exe in BaseDirectory.
                string candidate = Path.Combine(AppContext.BaseDirectory, "ProjectStarkCS.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Fallback to the process module (usually correct for published single-file or normal usage)
            return fileName ?? Environment.ProcessPath;
        }
    }
}
