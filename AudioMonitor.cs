using NAudio.Wave;
using System;
using System.Linq;

namespace ProjectStarkCS
{
    public class AudioMonitor
    {
        private WaveInEvent _waveIn;
        private double _threshold = 0.5;
        private DateTime _lastClapTime = DateTime.MinValue;
        private int _clapCount = 0;
        public event EventHandler OnDoubleClapDetected;

        public AudioMonitor(double threshold)
        {
            _threshold = threshold;
            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = 0; // Default microphone
            _waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz mono
            _waveIn.BufferMilliseconds = 20; // Short buffer for low latency
            _waveIn.DataAvailable += OnDataAvailable;
        }

        public void Start()
        {
            try
            {
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting microphone: {ex.Message}");
            }
        }

        public void Stop()
        {
            _waveIn.StopRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            // Calculate RMS amplitude
            double sum2 = 0;
            // 16-bit audio, so grab every 2 bytes
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                // Serialize to -1.0 to 1.0 range
                double val = sample / 32768.0;
                sum2 += val * val;
            }
            double rms = Math.Sqrt(sum2 / (e.BytesRecorded / 2));

            // Simple Clap Detection Logic
            // Debug: Show volume if it's somewhat loud (to help tuning)
            if (rms > 0.1) 
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[Vol: {rms:F2}] ");
                Console.ResetColor();
            }

            // If RMS exceeds threshold, we consider it a loud noise (potential clap)
            if (rms > _threshold)
            {
                // Check debounce (e.g., 100ms - faster response)
                if ((DateTime.Now - _lastClapTime).TotalMilliseconds > 100)
                {
                    _clapCount++;
                    _lastClapTime = DateTime.Now;
                    Console.WriteLine($"Clap Detected! (RMS: {rms:F2}) Count: {_clapCount}");

                    // Check for double clap
                    if (_clapCount == 2)
                    {
                        // Reset count and fire event
                        _clapCount = 0; 
                        OnDoubleClapDetected?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            // Reset clap count if too much time passes between claps (e.g., 1.0 seconds - quicker window)
            if (_clapCount > 0 && (DateTime.Now - _lastClapTime).TotalSeconds > 1.0)
            {
                Console.WriteLine("Clap sequence timed out.");
                _clapCount = 0;
            }
        }
    }
}
