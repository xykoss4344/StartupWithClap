using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ProjectStarkCS
{
    public class Config
    {
        public string WakeWord { get; set; } = "Jarvis";
        public double ClapThreshold { get; set; } = 0.5;
        public int LaunchDelaySeconds { get; set; } = 5;
        public string StartupSoundPath { get; set; } = "";
        public bool EnableMonitorFix { get; set; } = false;
        public List<string> TargetApps { get; set; } = new List<string>();
    }

    public static class AppLauncher
    {
        private static Config _config;

        public static Config LoadConfig(string path = "config.json")
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Config file not found at {path}. Creating default.");
                _config = new Config();
                // Add some defaults if empty
                _config.TargetApps.Add("calc.exe");
                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                return _config;
            }

            try
            {
                string json = File.ReadAllText(path);
                _config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
                Console.WriteLine($"Config loaded. Wake Word: {_config.WakeWord}");
                return _config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                return new Config(); 
            }
        }

        public static void ExecuteProtocol()
        {
            if (_config == null || _config.TargetApps == null)
            {
                Console.WriteLine("No configuration loaded.");
                return;
            }

            Console.WriteLine(">>> PROTOCOL INITIATED <<<");

            foreach (var app in _config.TargetApps)
            {
                try
                {
                    // Obfuscate logging to prevent terminal/agent from trying to fetch URLs/Paths
                    string displayName = app.StartsWith("http") ? "Web URL" : Path.GetFileName(app);
                    Console.WriteLine($"Launching: {displayName}");
                    // Use cmd.exe /c start to launch as a fully detached process.
                    // This prevents the app from closing when the main terminal (Project Stark) is closed.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"\" \"{app}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    
                    // Add a small delay between launches to prevent system/display driver stutter
                    System.Threading.Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to launch {app}: {ex.Message}");
                }
            }
        }

        public static void PerformMonitorHandshake()
        {
            try
            {
                Console.WriteLine("Force-cycling display signal (Monitor Fix)...");
                
                // 1. Switch to Internal Only (disconnects external monitors logic-wise)
                Process.Start("DisplaySwitch.exe", "/internal");
                System.Threading.Thread.Sleep(2000); // Wait for signal drop

                // 2. Switch to Extend (re-negotiates signal)
                Process.Start("DisplaySwitch.exe", "/extend");
                System.Threading.Thread.Sleep(2000); // Wait for signal sync
                
                Console.WriteLine("Display signal cycled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Monitor handshake failed: {ex.Message}");
            }
        }
    }
}
