using System;
using System.Speech.Recognition;

namespace ProjectStarkCS
{
    public class SpeechLogic
    {
        private SpeechRecognitionEngine _recognizer;
        private string _wakeWord;
        private double _confidenceThreshold;
        public event EventHandler OnWakeWordDetected;
        public event EventHandler OnFixDisplayCommand;

        public SpeechLogic(string wakeWord, double confidenceThreshold = 0.6)
        {
            _wakeWord = wakeWord;
            _confidenceThreshold = confidenceThreshold;
            InitRecognizer();
        }

        private void InitRecognizer()
        {
            try
            {
                // Create a generic recognizer (uses default input device)
                _recognizer = new SpeechRecognitionEngine(System.Globalization.CultureInfo.CurrentCulture);

                // Create grammar for the wake word
                // Create grammar for the wake word and commands
                Choices commands = new Choices();
                commands.Add(_wakeWord);
                commands.Add(_wakeWord.ToLower()); 
                commands.Add("Fix Display");
                commands.Add("Fix Monitor");
                
                GrammarBuilder gb = new GrammarBuilder();
                gb.Append(commands);
                
                Grammar g = new Grammar(gb);
                _recognizer.LoadGrammar(g);

                _recognizer.SpeechRecognized += _recognizer_SpeechRecognized;
                _recognizer.SetInputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Speech Logic: {ex.Message}");
            }
        }

        public void StartListening()
        {
            try
            {
                Console.WriteLine($"Listening for wake word: '{_wakeWord}' or 'Fix Display'...");
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting speech listener: {ex.Message}");
            }
        }

        public void StopListening()
        {
            _recognizer.RecognizeAsyncStop();
        }

        private void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            float confidence = e.Result.Confidence;
            string text = e.Result.Text;

            if (text.Equals(_wakeWord, StringComparison.OrdinalIgnoreCase) && confidence >= _confidenceThreshold)
            {
                Console.WriteLine($"Wake word detected! ({confidence:F2})");
                OnWakeWordDetected?.Invoke(this, EventArgs.Empty);
            }
            else if ((text.Equals("Fix Display", StringComparison.OrdinalIgnoreCase) || 
                      text.Equals("Fix Monitor", StringComparison.OrdinalIgnoreCase)) && confidence > 0.6)
            {
                Console.WriteLine($"Command Detected: {text} ({confidence:F2})");
                OnFixDisplayCommand?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
