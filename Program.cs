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
        private static bool _waitingForVoice = false;
        private static bool _hasPlayedSound = false;
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

                // 2. Monitor Handshake
                if (_config.EnableMonitorFix)
                {
                    Log("Config: Monitor Fix Enabled. Executing Handshake...");
                    AppLauncher.PerformMonitorHandshake();
                }

                // 3. Initialize Components
                Log("Initializing Speech and Audio...");
                _speech = new SpeechLogic(_config.WakeWord);
                _audioMonitor = new AudioMonitor(_config.ClapThreshold);

                // 4. Wire Events
                _speech.OnWakeWordDetected += Speech_OnWakeWordDetected;
                _speech.OnFixDisplayCommand += Speech_OnFixDisplayCommand;
                _audioMonitor.OnDoubleClapDetected += AudioMonitor_OnDoubleClapDetected;

                // 5. Start Listening for CLAPS first (Logic Inversion)
                Log("System Online. Listening for Double Clap...");
                Console.WriteLine("System Online. CLAP TWICE to arm.");
                _audioMonitor.Start();
                // do NOT start speech yet

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
            if (!_waitingForVoice) return;

            Console.WriteLine(">>> VOICE CONFIRMED. EXECUTING PROTOCOL <<<");
            Log("Voice Command Detected. Initiating Sequence.");

            // 1. Stop Listeners
            _timeoutTimer?.Dispose();
            _speech.StopListening();
            _waitingForVoice = false;

            // 2. Play Sound IMMEDIATELY (Async)
            if (!string.IsNullOrEmpty(_config.StartupSoundPath) && !_hasPlayedSound)
            {
                _hasPlayedSound = true;
                string soundPath = _config.StartupSoundPath;
                if (!Path.IsPathRooted(soundPath)) soundPath = Path.Combine(_baseDir, soundPath);
                Task.Run(() => PlaySound(soundPath));
            }

            // 3. Wait 2 Seconds
            Console.WriteLine("Standby for application launch...");
            Thread.Sleep(2000);

            // 4. Launch Apps
            AppLauncher.ExecuteProtocol();

            // 5. TERMINATE
            Console.WriteLine("Sequence Complete. Shutting down agent.");
            Log("Sequence Complete. Exiting.");
            Environment.Exit(0);
        }

        private static void Speech_OnFixDisplayCommand(object sender, EventArgs e)
        {
            Log("Voice Command: Fix Display");
            Console.WriteLine("Executing Monitor Fix Protocol...");
            
            // Optional: Play acknowledgment sound (Once)
            if (!string.IsNullOrEmpty(_config.StartupSoundPath) && !_hasPlayedSound)
            {
                _hasPlayedSound = true;
                // Play sound in background so we don't block
                 string soundPath = _config.StartupSoundPath;
                 if (!Path.IsPathRooted(soundPath)) soundPath = Path.Combine(_baseDir, soundPath);
                 Task.Run(() => PlaySound(soundPath));
            }

            AppLauncher.PerformMonitorHandshake();
        }

        private static void AudioMonitor_OnDoubleClapDetected(object sender, EventArgs e)
        {
            // If we are already waiting for voice (or executed), ignore
            if (_waitingForVoice) return;

            Console.WriteLine(">>> CLAP DETECTED. SAY 'WAKE UP' <<<");
            Log("Double Clap Detected. Waiting for Voice.");

            // Switch to Voice Mode
            _audioMonitor.Stop();
            _waitingForVoice = true;
            _speech.StartListening();

            // Start 5s Timeout
            _timeoutTimer = new Timer(OnTimeout, null, 5000, Timeout.Infinite);
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
            if (_waitingForVoice)
            {
                Console.WriteLine("Timeout. Voice not detected. Resetting...");
                _speech.StopListening();
                _waitingForVoice = false;
                _audioMonitor.Start();
                Console.WriteLine("System Reset. Listening for Double Clap...");
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
