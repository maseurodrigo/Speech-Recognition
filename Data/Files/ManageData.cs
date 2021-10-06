using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Xceed.Words.NET;

namespace Speech_Recognition.Data.Files
{
    public class ManageData
    {
        public static void exportBotCommands() {
            Random localRand = new Random();
            // SaveFileDialog to store speech commands
            using (var exportFileDialog = new SaveFileDialog()) {
                exportFileDialog.InitialDirectory = @"C:\";
                exportFileDialog.Filter = "Text Files (*.txt)|*.txt";
                exportFileDialog.FilterIndex = 2;
                exportFileDialog.RestoreDirectory = true;
                StringBuilder finalString = new StringBuilder();
                if (exportFileDialog.ShowDialog() == DialogResult.OK) {
                    // Loop through all bot main choices and fill stringbuilder
                    foreach (var mChoice in SpeechChoices.mainBotChoices) 
                        if (mChoice.Value) finalString.AppendLine(mChoice.Key);
                    foreach (var mChoice in SpeechChoices.DiscordData.botChoices)
                        if (mChoice.Value) finalString.AppendLine(mChoice.Key);
                    foreach (var mChoice in SpeechChoices.SpotifyData.botChoices)
                        if (mChoice.Value) finalString.AppendLine(mChoice.Key);
                    foreach (var mChoice in SpeechChoices.WhatsAppData.botChoices)
                        if (mChoice.Value) finalString.AppendLine(mChoice.Key);
                    // Data Bot Management
                    finalString.AppendLine($"Enable { GrammarData.strBotData.Keys.ToList()[localRand.Next(GrammarData.strBotData.Keys.Count)] } data");
                    finalString.AppendLine($"Disable { GrammarData.strBotData.Keys.ToList()[localRand.Next(GrammarData.strBotData.Keys.Count)] } data");
                    finalString.AppendLine($"Data status of { GrammarData.strBotData.Keys.ToList()[localRand.Next(GrammarData.strBotData.Keys.Count)] }");
                    finalString.AppendLine($"What is the procedure for { GrammarData.strBotData.Keys.ToList()[localRand.Next(GrammarData.strBotData.Keys.Count)] }");
                    // Applications Data
                    finalString.AppendLine($"Status of application { GrammarData.cAppsList[localRand.Next(GrammarData.cAppsList.Count)] }");
                    finalString.AppendLine($"Close application { GrammarData.cAppsList[localRand.Next(GrammarData.cAppsList.Count)] }");
                    finalString.AppendLine("Send to Discord Test");
                    // Services Data
                    finalString.AppendLine($"Status of service { GrammarData.cServicesList.Keys.ToList()[localRand.Next(GrammarData.cServicesList.Keys.Count)] }");
                    finalString.AppendLine($"Stop service { GrammarData.cServicesList.Keys.ToList()[localRand.Next(GrammarData.cServicesList.Keys.Count)] }");
                    // Cryptos Data
                    finalString.AppendLine("What is the price of Bitcoin");
                    finalString.AppendLine($"How much can i buy in Bitcoin with { localRand.Next(2000) } dollars");
                    // Cities Data
                    finalString.AppendLine("What time is it in Lisbon");
                    finalString.AppendLine("How is the weather in Lisbon");
                    finalString.AppendLine("How is the temperature in Lisbon");
                    // Football Teams Data
                    finalString.AppendLine("Who are the team members of PSG");
                    finalString.AppendLine($"What are the next { localRand.Next(20) } games for PSG");
                    finalString.AppendLine("Predict for the next game of PSG");
                    // New Alarm Data
                    finalString.AppendLine($"Set a new { GrammarData.cTimersList[localRand.Next(GrammarData.cTimersList.Count)] } for { localRand.Next(60) } minutes");
                    // Steam Games Data
                    finalString.AppendLine("Tell me who is playing Battlefield");
                    // Search Engines Data
                    finalString.AppendLine($"Search me Test on { GrammarData.cSearchEnginesList.Keys.ToList()[localRand.Next(GrammarData.cSearchEnginesList.Keys.Count)] }");
                    // Spotify Volume Data
                    finalString.AppendLine($"Spotify set volume to { localRand.Next(100) }");
                    // Spotify most heard artists and tracks
                    finalString.AppendLine($"Spotify what is my top { localRand.Next(20) } { GrammarData.cSpotifyTopDataList[localRand.Next(GrammarData.cSpotifyTopDataList.Count)] }");
                    // Create a playlist on Spotify and add tracks from a trending playlist to it
                    finalString.AppendLine($"Spotify create a playlist named Test and add { localRand.Next(50) } tracks from one trending Rock playlist");
                    // SMS message Data
                    finalString.AppendLine($"SMS message to { GrammarData.cContactsList.Keys.ToList()[localRand.Next(GrammarData.cContactsList.Keys.Count)] } with Test");
                    // WhatsApp message Data
                    finalString.AppendLine($"WhatsApp message to { GrammarData.cContactsList.Keys.ToList()[localRand.Next(GrammarData.cContactsList.Keys.Count)] } with Test");
                    // WhatsApp message with attached image(s) Data
                    finalString.AppendLine($"Send images via WhatsApp to { GrammarData.cContactsList.Keys.ToList()[localRand.Next(GrammarData.cContactsList.Keys.Count)] }");
                    File.WriteAllText($"{ exportFileDialog.FileName }.txt", finalString.ToString());
                }
            }
        }

        public static StringBuilder readTextFile() {
            StringBuilder strTextFile = new StringBuilder();
            // Trigger a filedialog to choose the text file location
            OpenFileDialog localTextData = new OpenFileDialog();
            localTextData.InitialDirectory = @"C:\";
            localTextData.Filter = "Text Files (*.txt)|*.txt";
            localTextData.FilterIndex = 2;
            if (localTextData.ShowDialog() == DialogResult.OK) {
                FileInfo fileInfo = new FileInfo(localTextData.FileName);
                // Check the text file extension
                if (fileInfo.Extension.Equals(".txt")) {
                    foreach (String line in File.ReadAllLines(localTextData.FileName)) {
                        strTextFile.AppendLine(line);
                    }
                } else if (fileInfo.Extension.Equals(".docx")) {
                    using (DocX doc = DocX.Load(localTextData.FileName)) {
                        for (int i=0; i < doc.Paragraphs.Count; i++) {
                            foreach (var line in doc.Paragraphs[i].Text.Split(new string[] { "\n" }, 
                                StringSplitOptions.RemoveEmptyEntries)) strTextFile.AppendLine(line);
                        }
                    }
                }
            } return strTextFile;
        }

        public static String convertTextIntoAudio() {
            // SaveFileDialog to store a new audio file from a text file
            using (var audioFileDialog = new SaveFileDialog()) {
                audioFileDialog.InitialDirectory = @"C:\";
                audioFileDialog.Filter = "Wave Files (*.wav)|*.wav";
                audioFileDialog.FilterIndex = 2;
                audioFileDialog.RestoreDirectory = true;
                if (audioFileDialog.ShowDialog() == DialogResult.OK) {
                    return audioFileDialog.FileName;
                } return String.Empty;
            }
        }
    }
}