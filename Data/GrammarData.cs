using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using Speech_Recognition.Data.Functions;
using Speech_Recognition.Data.Files;
using System.Globalization;

namespace Speech_Recognition.Data
{
    public class GrammarData
    {
        public static Dictionary<String, String> listBotVoices = new Dictionary<String, String>();

        public static Grammar changeBotVoice(CultureInfo _cultureToSet, Choices _cBotVoices) {
            GrammarBuilder cBotVoiceBuilder = new GrammarBuilder("Change to mode");
            cBotVoiceBuilder.Culture = _cultureToSet;
            cBotVoiceBuilder.Append(new SemanticResultKey("cBotVoice", _cBotVoices));
            Grammar cBotVoiceGrammar = new Grammar(cBotVoiceBuilder);
            cBotVoiceGrammar.Name = "cBotVoice";
            return cBotVoiceGrammar;
        }

        public static APIsData loadGrammarData(SpeechRecognitionEngine _recog, String _pathSchema, String _pathJSON, CultureInfo _cultureToSet) {
            APIsData currentData = new APIsData();
            // Reload all bot voices options (installed + enabled)
            OSData.getBotVoiceData();
            _recog.LoadGrammarAsync(changeBotVoice(_cultureToSet, new Choices(listBotVoices.Keys.ToArray())));
            // Get new data from JSON file
            FileData localFileData = new FileData(_pathSchema, _pathJSON);
            if (localFileData.isValidJson()) currentData = localFileData.getAPIsData();
            // Play-in speech
            _recog.LoadGrammarAsync(mainGrammar(_cultureToSet));
            _recog.LoadGrammarAsync(enableBotData(_cultureToSet));
            _recog.LoadGrammarAsync(disableBotData(_cultureToSet));
            _recog.LoadGrammarAsync(statusBotData(_cultureToSet));
            _recog.LoadGrammarAsync(botProcedureData(_cultureToSet));
            _recog.LoadGrammarAsync(newTimerComm(_cultureToSet));
            // Loading extra grammar data
            if (cAppsList.Count >= 1) {
                _recog.LoadGrammarAsync(checkProgramComm(_cultureToSet, new Choices(cAppsList.ToArray())));
                _recog.LoadGrammarAsync(closeProgramComm(_cultureToSet, new Choices(cAppsList.ToArray())));
                _recog.LoadGrammarAsync(sendDataProgramComm(_cultureToSet, new Choices(cAppsList.ToArray())));
            }
            if(cServicesList.Keys.Count >= 1) {
                _recog.LoadGrammarAsync(checkServiceComm(_cultureToSet, new Choices(cServicesList.Keys.ToArray())));
                _recog.LoadGrammarAsync(stopServiceComm(_cultureToSet, new Choices(cServicesList.Keys.ToArray())));
            }
            _recog.LoadGrammarAsync(checkCryptoComm(_cultureToSet));
            _recog.LoadGrammarAsync(getFiatToCryptoComm(_cultureToSet));
            _recog.LoadGrammarAsync(timeCityComm(_cultureToSet));
            _recog.LoadGrammarAsync(weathCityComm(_cultureToSet));
            _recog.LoadGrammarAsync(tempCityComm(_cultureToSet));
            _recog.LoadGrammarAsync(getTeamMembersComm(_cultureToSet));
            _recog.LoadGrammarAsync(getTeamGamesComm(_cultureToSet));
            _recog.LoadGrammarAsync(predNextGameComm(_cultureToSet));
            _recog.LoadGrammarAsync(getSteamUsersGamesComm(_cultureToSet));
            if (cSearchEnginesList.Keys.Count >= 1) 
                _recog.LoadGrammarAsync(getSearchEnginesComm(_cultureToSet, new Choices(cSearchEnginesList.Keys.ToArray())));
            _recog.LoadGrammarAsync(setVolumeSpotifyComm(_cultureToSet));
            _recog.LoadGrammarAsync(getSpotifyTopDataComm(_cultureToSet));
            _recog.LoadGrammarAsync(newSpotifyPlaylistComm(_cultureToSet));
            // MyNumber and another one (at least)
            if (cContactsList.Keys.Count >= 1) {
                _recog.LoadGrammarAsync(sendSMSMssgComm(_cultureToSet, new Choices(cContactsList.Keys.ToArray())));
                _recog.LoadGrammarAsync(sendWhatsAppMssgComm(_cultureToSet, new Choices(cContactsList.Keys.ToArray())));
                _recog.LoadGrammarAsync(sendWhatsAppImgsComm(_cultureToSet, new Choices(cContactsList.Keys.ToArray())));
            } return currentData;
        }

        /* ***************************************************************** */

        public static Dictionary<String, String[]> strBotData = new Dictionary<String, String[]> {
            { "Applications", 
                new String[] {
                    "Initially you need to enable application data",
                    "Then, you can already know the status of each one and close them",
                    "And just to remind, I only operate on applications defined by you"
                }
            },
            { "Services",
                new String[] {
                    "Initially you need to enable services data",
                    "Then, you can already know the status of each one and stop them",
                    "And just to remind, I only operate on services defined by you"
                }
            },
            { "Alarm",
                new String[] {
                    "Initially you need to enable alarm data",
                    "Then, you can set new values ​​for the timers and start or stop them, if they are already running"
                }
            },
            { "Discord",
                new String[] {
                    "Initially, you need to manually add the bot to the discord server you want",
                    "Then, you need to enable discord data, and applications data if you want to send messages",
                    "And to finish, you need to connect discord bot and wait for it to be completed",
                    "After that, you can already get data from the server or channels, defined by you, and send your messages"
                }
            },
            { "Spotify",
                new String[] {
                    "Initially you need to enable spotify data",
                    "Then, you need to authorize the connection with your spotify",
                    "After that, you can already play, pause, skip and set new volumes of your playbacks"
                }
            },
            { "WhatsApp",
                new String[] {
                    "Initially you need to enable WhatsApp data",
                    "Then, you need to connect the rest twilio client",
                    "After that, you can already send your SMS or WhatsApp messages for your contacts"
                }
            }
        };

        private static Choices cBotData = new Choices(strBotData.Keys.ToArray());

        public static Grammar mainGrammar(CultureInfo _cultureToSet) {
            // Main struct culture
            Choices mainChoices = new Choices();
            mainChoices.Add(SpeechChoices.mainBotChoices.Keys.ToArray());
            mainChoices.Add(SpeechChoices.DiscordData.botChoices.Keys.ToArray());
            mainChoices.Add(SpeechChoices.SpotifyData.botChoices.Keys.ToArray());
            mainChoices.Add(SpeechChoices.WhatsAppData.botChoices.Keys.ToArray());
            GrammarBuilder nGrammarBuild = new GrammarBuilder(mainChoices);
            nGrammarBuild.Culture = _cultureToSet;
            return new Grammar(nGrammarBuild);
        }

        public static Grammar enableBotData(CultureInfo _cultureToSet) {
            GrammarBuilder enabBotDataBuilder = new GrammarBuilder("Enable");
            enabBotDataBuilder.Culture = _cultureToSet;
            enabBotDataBuilder.Append(new SemanticResultKey("enableBotData", cBotData));
            enabBotDataBuilder.Append("data");
            return new Grammar(enabBotDataBuilder);
        }

        public static Grammar disableBotData(CultureInfo _cultureToSet) {
            GrammarBuilder disabBotDataBuilder = new GrammarBuilder("Disable");
            disabBotDataBuilder.Culture = _cultureToSet;
            disabBotDataBuilder.Append(new SemanticResultKey("disableBotData", cBotData));
            disabBotDataBuilder.Append("data");
            return new Grammar(disabBotDataBuilder);
        }

        public static Grammar statusBotData(CultureInfo _cultureToSet) {
            GrammarBuilder sttBotDataBuilder = new GrammarBuilder("Data status of");
            sttBotDataBuilder.Culture = _cultureToSet;
            sttBotDataBuilder.Append(new SemanticResultKey("sttBotData", cBotData));
            return new Grammar(sttBotDataBuilder);
        }

        public static Grammar botProcedureData(CultureInfo _cultureToSet) {
            GrammarBuilder botProcedureBuilder = new GrammarBuilder("What is the procedure for");
            botProcedureBuilder.Culture = _cultureToSet;
            botProcedureBuilder.Append(new SemanticResultKey("botProcedure", cBotData));
            return new Grammar(botProcedureBuilder);
        }

        /* ***************************************************************** */

        // Gets and sets applications list
        public static List<String> cAppsList = new List<String>();

        public static Grammar checkProgramComm(CultureInfo _cultureToSet, Choices _cApps) {
            GrammarBuilder statAppBuilder = new GrammarBuilder("Status of application");
            statAppBuilder.Culture = _cultureToSet;
            statAppBuilder.Append(new SemanticResultKey("stateApp", _cApps));
            return new Grammar(statAppBuilder);
        }

        public static Grammar closeProgramComm(CultureInfo _cultureToSet, Choices _cApps) {
            GrammarBuilder closeAppBuilder = new GrammarBuilder("Close application");
            closeAppBuilder.Culture = _cultureToSet;
            closeAppBuilder.Append(new SemanticResultKey("closeApp", _cApps));
            return new Grammar(closeAppBuilder);
        }

        public static Grammar sendDataProgramComm(CultureInfo _cultureToSet, Choices _cApps) {
            GrammarBuilder sendDataAppBuilder = new GrammarBuilder("Send to");
            sendDataAppBuilder.Culture = _cultureToSet;
            sendDataAppBuilder.Append(new SemanticResultKey("sendApp", _cApps));
            sendDataAppBuilder.AppendDictation();
            return new Grammar(sendDataAppBuilder);
        }

        /* ***************************************************************** */

        // Gets and sets services (name and service) dictionary
        public static Dictionary<String, String> cServicesList = new Dictionary<String, String>();

        public static Grammar checkServiceComm(CultureInfo _cultureToSet, Choices _cServices) {
            GrammarBuilder statServiceBuilder = new GrammarBuilder("Status of service");
            statServiceBuilder.Culture = _cultureToSet;
            statServiceBuilder.Append(new SemanticResultKey("stateService", _cServices));
            return new Grammar(statServiceBuilder);
        }

        public static Grammar stopServiceComm(CultureInfo _cultureToSet, Choices _cServices) {
            GrammarBuilder stopServiceBuilder = new GrammarBuilder("Stop service");
            stopServiceBuilder.Culture = _cultureToSet;
            stopServiceBuilder.Append(new SemanticResultKey("stopService", _cServices));
            return new Grammar(stopServiceBuilder);
        }

        /* ***************************************************************** */

        public static Grammar checkCryptoComm(CultureInfo _cultureToSet) {
            GrammarBuilder checkCryptoBuilder = new GrammarBuilder(new SemanticResultKey("checkCrypto", new Choices("What is the price of")));
            checkCryptoBuilder.Culture = _cultureToSet;
            checkCryptoBuilder.AppendDictation();
            return new Grammar(checkCryptoBuilder);
        }

        public static Grammar getFiatToCryptoComm(CultureInfo _cultureToSet) {
            GrammarBuilder convCryptoBuilder = new GrammarBuilder(new SemanticResultKey("fiatToCrypto", new Choices("How much can i buy in")));
            convCryptoBuilder.Culture = _cultureToSet;
            convCryptoBuilder.AppendDictation();
            convCryptoBuilder.Append("with");
            convCryptoBuilder.AppendDictation();
            convCryptoBuilder.Append("dollars");
            return new Grammar(convCryptoBuilder);
        }

        /* ***************************************************************** */

        public static Grammar timeCityComm(CultureInfo _cultureToSet) {
            GrammarBuilder timeCityBuilder = new GrammarBuilder(new SemanticResultKey("timeCity", new Choices("What time is it in")));
            timeCityBuilder.Culture = _cultureToSet;
            timeCityBuilder.AppendDictation();
            return new Grammar(timeCityBuilder);
        }

        public static Grammar weathCityComm(CultureInfo _cultureToSet) {
            GrammarBuilder weathCityBuilder = new GrammarBuilder(new SemanticResultKey("weathCity", new Choices("How is the weather in")));
            weathCityBuilder.Culture = _cultureToSet;
            weathCityBuilder.AppendDictation();
            return new Grammar(weathCityBuilder);
        }

        public static Grammar tempCityComm(CultureInfo _cultureToSet) {
            GrammarBuilder tempCityBuilder = new GrammarBuilder(new SemanticResultKey("tempCity", new Choices("How is the temperature in")));
            tempCityBuilder.Culture = _cultureToSet;
            tempCityBuilder.AppendDictation();
            return new Grammar(tempCityBuilder);
        }

        /* ***************************************************************** */

        public static Grammar getTeamMembersComm(CultureInfo _cultureToSet) {
            GrammarBuilder getTeamMembersBuilder = new GrammarBuilder(new SemanticResultKey("teamMembers", new Choices("Who are the team members of")));
            getTeamMembersBuilder.Culture = _cultureToSet;
            getTeamMembersBuilder.AppendDictation();
            return new Grammar(getTeamMembersBuilder);
        }
        
        public static Grammar getTeamGamesComm(CultureInfo _cultureToSet) {
            GrammarBuilder getTeamGamesBuilder = new GrammarBuilder("What are the next");
            getTeamGamesBuilder.Culture = _cultureToSet;
            getTeamGamesBuilder.AppendDictation();
            getTeamGamesBuilder.Append("games");
            getTeamGamesBuilder.Append(new SemanticResultKey("teamGames", new Choices("for")));
            getTeamGamesBuilder.AppendDictation();
            return new Grammar(getTeamGamesBuilder);
        }

        public static Grammar predNextGameComm(CultureInfo _cultureToSet) {
            GrammarBuilder predNextGameBuilder = new GrammarBuilder(new SemanticResultKey("predictNGame", new Choices("Predict for the next game of")));
            predNextGameBuilder.Culture = _cultureToSet;
            predNextGameBuilder.AppendDictation();
            return new Grammar(predNextGameBuilder);
        }

        /* ***************************************************************** */

        public static List<String> cTimersList = new List<String> { "Alarm", "Seated alert" };

        public static Grammar newTimerComm(CultureInfo _cultureToSet) {
            GrammarBuilder newTimerBuilder = new GrammarBuilder("Set a new");
            newTimerBuilder.Culture = _cultureToSet;
            newTimerBuilder.Append(new SemanticResultKey("newTimer", new Choices(cTimersList.ToArray())));
            newTimerBuilder.Append("for");
            newTimerBuilder.AppendDictation();
            newTimerBuilder.Append("minutes");
            return new Grammar(newTimerBuilder);
        }

        /* ***************************************************************** */

        public static Grammar getSteamUsersGamesComm(CultureInfo _cultureToSet) {
            GrammarBuilder getSteamUsersGamesBuilder = new GrammarBuilder(new SemanticResultKey("uSteamGames", new Choices("Tell me who is playing")));
            getSteamUsersGamesBuilder.Culture = _cultureToSet;
            getSteamUsersGamesBuilder.AppendDictation();
            return new Grammar(getSteamUsersGamesBuilder);
        }

        /* ***************************************************************** */

        // Gets and sets search engines (name and url) dictionary
        public static Dictionary<String, String> cSearchEnginesList = new Dictionary<String, String>();

        public static Grammar getSearchEnginesComm(CultureInfo _cultureToSet, Choices _cSearchEngines) {
            GrammarBuilder searchEnginesBuilder = new GrammarBuilder("Search me");
            searchEnginesBuilder.Culture = _cultureToSet;
            searchEnginesBuilder.AppendDictation();
            searchEnginesBuilder.Append("on");
            searchEnginesBuilder.Append(new SemanticResultKey("searchEngines", _cSearchEngines));
            return new Grammar(searchEnginesBuilder);
        }

        /* ***************************************************************** */

        // Change spotify playback volume (speech value)
        public static Grammar setVolumeSpotifyComm(CultureInfo _cultureToSet) {
            GrammarBuilder setVolumeSpotifyBuilder = 
                new GrammarBuilder(new SemanticResultKey("volumeSpotify", new Choices(strBotData.Keys.ToList()[4])));
            setVolumeSpotifyBuilder.Culture = _cultureToSet;
            setVolumeSpotifyBuilder.Append("set volume to");
            setVolumeSpotifyBuilder.AppendDictation();
            return new Grammar(setVolumeSpotifyBuilder);
        }

        // Get the most heard artists and tracks from current Spotify user
        public static List<String> cSpotifyTopDataList = new List<String> { "Artists", "Tracks" };
        public static Grammar getSpotifyTopDataComm(CultureInfo _cultureToSet) {
            GrammarBuilder getSpotifyTopDataBuilder = new GrammarBuilder($"{ strBotData.Keys.ToList()[4] } what is my top");
            getSpotifyTopDataBuilder.Culture = _cultureToSet;
            getSpotifyTopDataBuilder.AppendDictation();
            getSpotifyTopDataBuilder.Append(new SemanticResultKey("dataSpotify", new Choices(cSpotifyTopDataList.ToArray())));
            return new Grammar(getSpotifyTopDataBuilder);
        }

        // Create playlist on Spotify and add tracks to it
        public static Grammar newSpotifyPlaylistComm(CultureInfo _cultureToSet) {
            GrammarBuilder newSpotifyPlaylistBuilder = 
                new GrammarBuilder(new SemanticResultKey("playlistSpotify", new Choices(strBotData.Keys.ToList()[4])));
            newSpotifyPlaylistBuilder.Culture = _cultureToSet;
            newSpotifyPlaylistBuilder.Append("create a playlist named");
            newSpotifyPlaylistBuilder.AppendDictation();
            newSpotifyPlaylistBuilder.Append("and add");
            newSpotifyPlaylistBuilder.AppendDictation();
            newSpotifyPlaylistBuilder.Append("tracks from one trending");
            newSpotifyPlaylistBuilder.AppendDictation();
            newSpotifyPlaylistBuilder.Append("playlist");
            return new Grammar(newSpotifyPlaylistBuilder);
        }

        /* ***************************************************************** */

        // Gets and sets contacts (name and number) dictionary
        public static Dictionary<String, String> cContactsList = new Dictionary<String, String>();

        // Send an SMS message (speech text)
        public static Grammar sendSMSMssgComm(CultureInfo _cultureToSet, Choices _cContacts) {
            GrammarBuilder sendSMSMssgBuilder = new GrammarBuilder("SMS message to");
            sendSMSMssgBuilder.Culture = _cultureToSet;
            sendSMSMssgBuilder.Append(new SemanticResultKey("contactSMS", _cContacts));
            sendSMSMssgBuilder.Append("with");
            sendSMSMssgBuilder.AppendDictation();
            return new Grammar(sendSMSMssgBuilder);
        }

        // Send a WhatsApp message (speech text)
        public static Grammar sendWhatsAppMssgComm(CultureInfo _cultureToSet, Choices _cContacts) {
            GrammarBuilder sendWhatsAppMssgBuilder = new GrammarBuilder("WhatsApp message to");
            sendWhatsAppMssgBuilder.Culture = _cultureToSet;
            sendWhatsAppMssgBuilder.Append(new SemanticResultKey("contactWhatsApp", _cContacts));
            sendWhatsAppMssgBuilder.Append("with");
            sendWhatsAppMssgBuilder.AppendDictation();
            return new Grammar(sendWhatsAppMssgBuilder);
        }

        // Send a WhatsApp message (attached image(s))
        public static Grammar sendWhatsAppImgsComm(CultureInfo _cultureToSet, Choices _cContacts) {
            GrammarBuilder sendWhatsAppImgsBuilder = new GrammarBuilder("Send images via WhatsApp to");
            sendWhatsAppImgsBuilder.Culture = _cultureToSet;
            sendWhatsAppImgsBuilder.Append(new SemanticResultKey("imgsWhatsApp", _cContacts));
            return new Grammar(sendWhatsAppImgsBuilder);
        }

        /* ***************************************************************** */
    }
}