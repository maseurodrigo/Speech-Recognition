using System.ServiceProcess;
using System.Speech.Synthesis;

namespace Speech_Recognition.Data.Functions
{
    public class OSData
    {
        public static string serviceStatus(ServiceController _sc) {
            // Return state of the given service
            switch (_sc.Status) {
                case ServiceControllerStatus.Running:
                    return "Running";
                case ServiceControllerStatus.Stopped:
                    return "Stopped";
                case ServiceControllerStatus.Paused:
                    return "Paused";
                case ServiceControllerStatus.StopPending:
                    return "Stopping";
                case ServiceControllerStatus.StartPending:
                    return "Starting";
                default:
                    return "Status Changing";
            }
        }

        public static void getBotVoiceData() {
            // Store windows installed voices
            GrammarData.listBotVoices.Clear();
            foreach (var voice in new SpeechSynthesizer().GetInstalledVoices()) 
                GrammarData.listBotVoices.Add(voice.VoiceInfo.Name, voice.VoiceInfo.Culture.Name);
        }
    }
}
