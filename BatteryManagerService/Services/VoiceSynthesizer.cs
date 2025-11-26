using System.Speech.Synthesis;

namespace BatteryManagerService.Services
{
    /// <summary>
    /// Interface for text-to-speech voice synthesis.
    /// </summary>
    public interface IVoiceSynthesizer
    {
        /// <summary>
        /// Speaks the given message asynchronously.
        /// </summary>
        Task SpeakAsync(string message);

        /// <summary>
        /// Cancels any ongoing speech.
        /// </summary>
        void CancelSpeech();
    }

    /// <summary>
    /// Voice synthesizer using System.Speech.Synthesis (SAPI) for offline TTS.
    /// </summary>
    public class VoiceSynthesizer : IVoiceSynthesizer, IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;
        private readonly ILogger<VoiceSynthesizer> _logger;
        private readonly SemaphoreSlim _speechLock = new(1, 1);

        public VoiceSynthesizer(ILogger<VoiceSynthesizer> logger)
        {
            _logger = logger;
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.Rate = 0; // Normal speed
            _synthesizer.Volume = 100; // Maximum volume
        }

        /// <summary>
        /// Speaks the message asynchronously. Thread-safe.
        /// </summary>
        public async Task SpeakAsync(string message)
        {
            await _speechLock.WaitAsync();
            try
            {
                _logger.LogInformation("Speaking: {Message}", message);
                await Task.Run(() => _synthesizer.Speak(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error speaking message: {Message}", message);
            }
            finally
            {
                _speechLock.Release();
            }
        }

        /// <summary>
        /// Cancels any ongoing speech immediately.
        /// </summary>
        public void CancelSpeech()
        {
            try
            {
                _synthesizer.SpeakAsyncCancelAll();
                _logger.LogDebug("Speech canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling speech");
            }
        }

        public void Dispose()
        {
            _synthesizer?.Dispose();
            _speechLock?.Dispose();
        }
    }
}
