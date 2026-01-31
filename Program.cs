using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;
using NAudio.Wave;
using System.IO;

namespace ProjectStarkCS
{
    class Program
    {
        private static Config _config;
        private static SpeechLogic _speech;
        private static AudioMonitor _audioMonitor;
        private static string _baseDir;
        private static bool _waitingForClaps = false;
        private static Timer _timeoutTimer;

        static void Main(string[] args)
        {
            _baseDir = AppContext.BaseDirectory;
            Log("Application Starting...");

            try
            {
                // 1. Load Config (Absolute Path)
                string configPath = Path.Combine(_baseDir, "config.json");
                _config = AppLauncher.LoadConfig(configPath);

                // 2. Monitor Handshake (Startup logic removed for safety - moved to Voice Command)

                // 3. Initialize Components
                Log("Initializing Speech and Audio...");
                _speech = new SpeechLogic(_config.WakeWord);
                _audioMonitor = new AudioMonitor(_config.ClapThreshold);

                // 4. Wire Events
                _speech.OnWakeWordDetected += Speech_OnWakeWordDetected;
                _speech.OnFixDisplayCommand += Speech_OnFixDisplayCommand;
                _audioMonitor.OnDoubleClapDetected += AudioMonitor_OnDoubleClapDetected;

                // 5. Start Listening
                _speech.StartListening();

                Console.WriteLine("System Online. Say the wake word to arm.");
                Log("System Online and Listening.");
                
                // Keep app alive
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Log($"CRITICAL STARTUP ERROR: {ex}");
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadLine(); // Keep window open to see error
            }
        }

        private static void Log(string message)
        {
            string logFile = Path.Combine(_baseDir, "error_log.txt");
            try
            {
                File.AppendAllText(logFile, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch { }
        }

        private static void Speech_OnWakeWordDetected(object sender, EventArgs e)
        {
            if (_waitingForClaps) return; 

            Console.WriteLine(">>> SYSTEM ARMED. CLAP TWICE TO EXECUTE <<<");
            Log("Wake Word Detected. Waiting for claps.");
            _waitingForClaps = true;
            
            _audioMonitor.Start();

            _timeoutTimer = new Timer(OnTimeout, null, 5000, Timeout.Infinite);
        }

        private static void Speech_OnFixDisplayCommand(object sender, EventArgs e)
        {
            Log("Voice Command: Fix Display");
            Console.WriteLine("Executing Monitor Fix Protocol...");
            
            // Optional: Play acknowledgment sound
            if (!string.IsNullOrEmpty(_config.StartupSoundPath))
            {
                // Play sound in background so we don't block
                 string soundPath = _config.StartupSoundPath;
                 if (!Path.IsPathRooted(soundPath)) soundPath = Path.Combine(_baseDir, soundPath);
                 Task.Run(() => PlaySound(soundPath));
            }

            AppLauncher.PerformMonitorHandshake();
        }

        private static void AudioMonitor_OnDoubleClapDetected(object sender, EventArgs e)
        {
            if (!_waitingForClaps) return;

            Console.WriteLine("Authentication Confirmed.");
            Log("Double Clap Detected.");
            _timeoutTimer?.Dispose();
            _audioMonitor.Stop();
            _waitingForClaps = false;

            // Play Startup Sound
            if (!string.IsNullOrEmpty(_config.StartupSoundPath))
            {
                // Ensure absolute path
                string soundPath = _config.StartupSoundPath;
                if (!Path.IsPathRooted(soundPath))
                {
                    soundPath = Path.Combine(_baseDir, soundPath);
                }
                
                Task.Run(() => PlaySound(soundPath));
            }

            Console.WriteLine("Initiating 4-second pre-flight sequence...");
            Thread.Sleep(4000); 

            // EXECUTE PROTOCOL
            AppLauncher.ExecuteProtocol();

            Console.WriteLine("Protocol Complete. Resuming Listen Mode...");
            Log("Protocol Executed.");
        }

        private static void PlaySound(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Log($"Audio File Not Found: {path}");
                return;
            }

            try
            {
                Console.WriteLine($"Playing audio: {path}");
                using (var audioFile = new AudioFileReader(path))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Audio Error: {ex.Message}");
                Console.WriteLine($"Failed to play sound: {ex.Message}");
            }
        }

        private static void OnTimeout(object state)
        {
            if (_waitingForClaps)
            {
                Console.WriteLine("Timeout. System Disarmed.");
                _audioMonitor.Stop();
                _waitingForClaps = false;
            }
            _timeoutTimer?.Dispose();
        }

        private static void EnsureStartup()
        {
            // Removed
        }

        private static string GetExecutablePath()
        {
             return Environment.ProcessPath;
        }


    }
}
