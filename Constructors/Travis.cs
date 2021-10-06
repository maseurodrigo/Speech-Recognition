using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Forms;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Toolkit.Uwp.Notifications;
using Speech_Recognition.Data;
using Speech_Recognition.Data.Applications;
using Speech_Recognition.Data.Files;
using Speech_Recognition.Data.Files.Logs;
using Speech_Recognition.Data.Functions;
using Timer = System.Windows.Forms.Timer;

namespace Speech_Recognition.Constructors
{
    public class Travis 
    {
        // Main form instance
        private Form mainAppForm = new Form();
        private CultureInfo currentCulture;
        // In-process speech recognition engines
        private SpeechRecognitionEngine _speechEngine;
        private SpeechRecognitionEngine _standSpeechEngine;
        public SpeechSynthesizer _Travis { get; private set; } // Main voice operator
        private DiscordData discordClient = null; // Travis own DiscordClient
        private SpotifyData spotifyClient = null; // Travis own SpotifyClient
        private WhatsAppData whatsAppClient = null; // Travis own WhatsAppClient
        private AppsLog applicationsLog; // Applications logs instance
        private ServicesLog servicesLog; // Services logs instance
        // Custom data variables
        private String pathJSONData, pathJSONSchema;
        private FileData localFileData;
        private APIsData localAPIsData;
        public Dictionary<String, bool> botDataStatus { get; private set; }
        private Timer timerAlarm = new Timer(), timerSitting = new Timer(), timerTick = new Timer();
        private Stopwatch sittingTimer = new Stopwatch();
        private NotifyIcon notifyIcon = new NotifyIcon();
        private int speechRecTimeout, alarmTime, seatTime;
        private double minSpeechConfidence;

        public Travis(Form _mainApp, CultureInfo _cultureToSet, String _pathJSONSchema, String _pathJSONData, int _speechVolume, double _minConfidence) {
            mainAppForm = _mainApp; // Define main form instance
            currentCulture = _cultureToSet; // Define instantiated culture
            this.pathJSONSchema = _pathJSONSchema; // Define JSON schema
            this.pathJSONData = _pathJSONData; // Define default JSON data file
            try {
                // Initialize main voice operator
                _Travis = new SpeechSynthesizer();
                _Travis.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                _Travis.Volume = _speechVolume; // SpeechSynthesizer base volume
                localFileData = new FileData(pathJSONSchema, pathJSONData);
                // If the data from the provided JSON file is valid
                if (localFileData.isValidJson()) {
                    // Initialize both speech recognition engines
                    _speechEngine = new SpeechRecognitionEngine(currentCulture);
                    _speechEngine.SetInputToDefaultAudioDevice();
                    _speechEngine.InitialSilenceTimeout = TimeSpan.FromSeconds(2);
                    _speechEngine.BabbleTimeout = TimeSpan.FromSeconds(1.5);
                    _speechEngine.EndSilenceTimeout = TimeSpan.FromSeconds(0.75);
                    _speechEngine.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1);
                    _standSpeechEngine = new SpeechRecognitionEngine(currentCulture);
                    _standSpeechEngine.SetInputToDefaultAudioDevice();
                    _standSpeechEngine.InitialSilenceTimeout = TimeSpan.FromSeconds(2);
                    _standSpeechEngine.BabbleTimeout = TimeSpan.FromSeconds(1);
                    // Setting up main data (mainly APIs)
                    this.localAPIsData = GrammarData.loadGrammarData(_speechEngine, pathJSONSchema, pathJSONData, currentCulture);
                    // Setting up default speech recognition engine
                    this.minSpeechConfidence = _minConfidence;
                    _speechEngine.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(recog_SpeechRecognizer);
                    _speechEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recog_SpeechRecognition);
                    _speechEngine.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(recog_SpeechRecognitionRejected);
                    _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                    // Setting up idle speech recognition engine
                    _standSpeechEngine.LoadGrammarAsync(GrammarData.mainGrammar(currentCulture));
                    _standSpeechEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(startListen_SpeechRecognizer);
                    // Setting up custom data parameters
                    botDataStatus = new Dictionary<String, bool>() {
                        { GrammarData.strBotData.Keys.ToList()[0], false }, // Applications false
                        { GrammarData.strBotData.Keys.ToList()[1], false }, // Services false
                        { GrammarData.strBotData.Keys.ToList()[2], false }, // Alarm false
                        { GrammarData.strBotData.Keys.ToList()[3], false }, // Discord false
                        { GrammarData.strBotData.Keys.ToList()[4], false }, // Spotify false
                        { GrammarData.strBotData.Keys.ToList()[5], false } // Whatsapp false
                    };
                    notifyIcon.Text = "Speech Recognition";
                    notifyIcon.Icon = File.Exists(@"Resources\icon.ico") ? new System.Drawing.Icon(@"Resources\icon.ico") : null;
                    notifyIcon.DoubleClick += new EventHandler(notifyIcon_MouseDoubleClick);
                    timerTick.Interval = 100; // Timer for idle mode speech
                    timerTick.Tick += new EventHandler(timerTick_Tick);
                    timerTick.Start();
                    timerAlarm.Interval = 1000; // New alarm timer
                    timerAlarm.Tick += new EventHandler(timerAlarm_Tick);
                    this.seatTime = 3600; // 1h
                    timerSitting.Interval = 1000; // Sitting alert timer
                    timerSitting.Tick += new EventHandler(timerSitting_Tick);
                    timerSitting.Start();
                    sittingTimer.Start();
                    // Setting up logs filenames
                    applicationsLog = new AppsLog("apps");
                    servicesLog = new ServicesLog("services");
                    _Travis.SpeakAsync(SpeechChoices.defBotComms[1]);
                } else _Travis.SpeakAsync(SpeechChoices.defBotComms[2]);
            } catch (ArgumentException) {
                // Windows voice pack not installed/found nor enabled
                new ToastContentBuilder().AddArgument("action", "viewConversation").AddText("Voice not found")
                        .AddText($"{ _cultureToSet.DisplayName } ({ _cultureToSet.Name }) voice not found and/or not installed").Show();
                Application.ExitThread();
                Environment.Exit(0);
            } catch (IOException) {
                // JSON file its currently opened
                _Travis.SpeakAsync(SpeechChoices.defBotComms[3]);
                _Travis.SpeakAsync(SpeechChoices.defBotComms[4]);
                Thread.Sleep(10000);
                Application.Restart();
                Environment.Exit(0);
            }
        }

        private void recog_SpeechRecognizer(object sender, SpeechDetectedEventArgs e) { this.speechRecTimeout = 0; }

        private void startListen_SpeechRecognizer(object sender, SpeechRecognizedEventArgs e) {
            string speech = e.Result.Text;
            if (speech.Equals("Travis")) {
                _Travis.SpeakAsync(SpeechChoices.defBotComms[8]);
            } else if (speech.Equals("Wake up")) {
                _standSpeechEngine.RecognizeAsyncCancel();
                _Travis.SpeakAsync(SpeechChoices.defBotComms[9]);
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                mainAppForm.Show();
                // Starting sittingTimer
                if (!sittingTimer.IsRunning) {
                    timerSitting.Start();
                    sittingTimer.Start();
                }
            }
        }

        private void timerAlarm_Tick(object sender, EventArgs e) {
            if(alarmTime.Equals(0)) {
                timerAlarm.Stop();
                _Travis.SpeakAsync(SpeechChoices.defBotComms[7]);
                new ToastContentBuilder().AddArgument("action", "viewConversation")
                    .AddText("Alarm alert").AddText(SpeechChoices.defBotComms[35])
                    .AddAudio(new Uri("ms-winsoundevent:Notification.Looping.Alarm")).Show();
            } else alarmTime--;
        }

        private void timerSitting_Tick(object sender, EventArgs e) {
            if ((sittingTimer.ElapsedMilliseconds / 1000) > seatTime) {
                _Travis.SpeakAsync(SpeechChoices.defBotComms[5]);
                sittingTimer.Restart(); // Stop, reset and start sittingTimer
            }
        }

        private void timerTick_Tick(object sender, EventArgs e) {
            if (speechRecTimeout.Equals(10)) {
                _speechEngine.RecognizeAsyncCancel();
            } else if (speechRecTimeout.Equals(11)) {
                timerTick.Stop();
                _standSpeechEngine.RecognizeAsync(RecognizeMode.Multiple);
                speechRecTimeout = 0;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, EventArgs e) {
            mainAppForm.Show();
            //this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            _Travis.SpeakAsync(SpeechChoices.defBotComms[6]);
        }

        private void recog_SpeechRecognition(object sender, SpeechRecognizedEventArgs e) {
            // Checking locally if its a return from a pause
            bool seatReturn = false;
            if (!sittingTimer.IsRunning) {
                seatReturn = true;
                sittingTimer.Start();
            }
            // Minimum confidence level for speech recognition
            if (e.Result.Confidence >= minSpeechConfidence) {
                // Static speeches
                if (e.Result.Text.StartsWith(GrammarData.strBotData.Keys.ToList()[3])) {
                    switch (e.Result.Text) {
                        case "Discord bot connection status":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<String, String> discBotConn = discordClient.getDiscordBotConn();
                                _Travis.SpeakAsync($"{ discBotConn.Key } and { discBotConn.Value }");
                            }
                            break;
                        case "Discord connect bot":
                            if (discordClient is null || discordClient.checkDiscordBotConn(LoginState.LoggedOut, ConnectionState.Disconnected)) {
                                if(discordClient is null) discordClient = new DiscordData(_Travis, localAPIsData); // Instance a new DiscordData client
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], true, LoginState.LoggedOut, ConnectionState.Disconnected);
                                if (discBotReadyTo.Key) {
                                    // Starts the connection between discord and the client
                                    discordClient.connectDiscordBot();
                                    KeyValuePair<String, String> discordBotConn = discordClient.getDiscordBotConn();
                                    _Travis.SpeakAsync(discordBotConn.Key);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            } else _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[2]);
                            break;
                        case "Discord disconnect bot":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Stops the connection between discord and the client
                                    discordClient.disconnectDiscordBot();
                                    KeyValuePair<String, String> discordBotConn = discordClient.getDiscordBotConn();
                                    _Travis.SpeakAsync(discordBotConn.Key);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord enable voice channels feedback":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Enable all voice channels interactions
                                    _Travis.SpeakAsync(discordClient.changeDiscordFeedback(1, true).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord disable voice channels feedback":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Disable all voice channels interactions
                                    _Travis.SpeakAsync(discordClient.changeDiscordFeedback(1, false).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord enable tags feedback":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Enable all user tags interactions
                                    _Travis.SpeakAsync(discordClient.changeDiscordFeedback(2, true).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord disable tags feedback":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Disable all user tags interactions
                                    _Travis.SpeakAsync(discordClient.changeDiscordFeedback(2, false).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord enable bot commands":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Enable all bot commands interactions
                                    _Travis.SpeakAsync(discordClient.changeDiscordFeedback(3, true).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord disable bot commands":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Disable all bot commands interactions
                                    _Travis.SpeakAsync(discordClient.changeDiscordFeedback(3, false).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord users in voice channels":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Get total of users in voice channels
                                    _Travis.SpeakAsync(discordClient.getUsersInVoiceChannels(localAPIsData.DiscordServerIDs.ServerID.Value).Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                        case "Discord tag everyone":
                            if (discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> discBotReadyTo =
                                    discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                if (discBotReadyTo.Key) {
                                    // Tag @here on defined discord server and channel data
                                    _Travis.SpeakAsync(discordClient.sendTextToChannel(localAPIsData.DiscordServerIDs.ServerID.Value, localAPIsData.DiscordServerIDs.ChannelID.Value, true, "@here").Value);
                                } else _Travis.SpeakAsync(discBotReadyTo.Value);
                            }
                            break;
                    }
                } else if (e.Result.Text.StartsWith(GrammarData.strBotData.Keys.ToList()[4])) {
                    switch (e.Result.Text) {
                        case "Spotify connection status":
                            if(spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, bool?> spotifyClientStatus = spotifyClient.checkSpotifyClient(true);
                                if (!spotifyClientStatus.Key && !spotifyClientStatus.Value.Value) 
                                    _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[2]);
                                else {
                                    if(spotifyClientStatus.Key && spotifyClientStatus.Value.Value) 
                                        _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                                    else if (!spotifyClientStatus.Key && spotifyClientStatus.Value.Value) 
                                        _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[5]);
                                }
                            }
                            break;
                        case "Spotify connect me":
                            if (spotifyClient is null || spotifyClient.spotifyReadyToConnect(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]]).Key) {
                                // Instance a new SpotifyData client
                                if (spotifyClient is null) spotifyClient = new SpotifyData(_Travis, "http://localhost:5000/", localAPIsData.SpotifyAPI.ClientID, localAPIsData.SpotifyAPI.ClientSecret);
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToConnect(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]]);
                                if (spotifyReadyTo.Key) {
                                    if(String.IsNullOrWhiteSpace(spotifyReadyTo.Value)) {
                                        _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[0]);
                                        spotifyClient.setupSpotifyAuth();
                                        Thread.Sleep(2000); // Connection delay
                                        _Travis.SpeakAsync(spotifyClient.setSpotifyClient().Value);
                                    } else _Travis.SpeakAsync(spotifyClient.setSpotifyClient().Value);
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            } else _Travis.SpeakAsync(spotifyClient.spotifyReadyToConnect(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]]).Value);
                            break;
                        case "Spotify play":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.playSpotifyClient(); // Play command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify pause":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.pauseSpotifyClient(); // Pause command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify next":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.skipNextSpotifyClient(); // Skip to next command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify previous":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.skipPreviousSpotifyClient(); // Skip to previous command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify mute":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.muteSpotifyClient(); // Mute command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify enable shuffle":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.enableSuffleSpotifyClient(); // Enable shuffle command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify disable shuffle":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.disableSuffleSpotifyClient(); // Disable shuffle command for Spotify
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify name of the current music":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    String tmpMusicName = spotifyClient.getCurrentMusicName();
                                    _Travis.SpeakAsync(String.IsNullOrWhiteSpace(tmpMusicName) ? SpeechChoices.SpotifyData.botComms[6] : tmpMusicName); // Get the current music on the playback
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify repeat the current music":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                                if (spotifyReadyTo.Key) {
                                    spotifyClient.repeatCurrentMusic(); // Replay the current music
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                        case "Spotify list me some top categories":
                            if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                            else {
                                KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], null);
                                if (spotifyReadyTo.Key) {
                                    // Loop through a list of Spotify's top categories
                                    foreach (String category in spotifyClient.getSpotifyTopCategories()) _Travis.SpeakAsync(category);
                                } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                            }
                            break;
                    }
                } else if (e.Result.Text.StartsWith(GrammarData.strBotData.Keys.ToList()[5])) {
                    switch (e.Result.Text) {
                        case "WhatsApp client connection status":
                            if (whatsAppClient is null) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                            else {
                                if (whatsAppClient.getStatusTwilioConn().Key) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[1]);
                                else _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                            }
                            break;
                        case "WhatsApp connect client":
                            if (whatsAppClient is null || !whatsAppClient.getStatusTwilioConn().Key) {
                                // Instance a new WhatsAppData client
                                whatsAppClient = new WhatsAppData(new KeyValuePair<String, String>(localAPIsData.Contacts[0].WhatsAppName, localAPIsData.Contacts[0].WhatsAppNumber), 
                                    localAPIsData.TwilioAPI.TwilioSID, localAPIsData.TwilioAPI.AuthToken, localAPIsData.TwilioAPI.MessageServiceSID);
                                KeyValuePair<bool, String> twilioClientStatus = whatsAppClient.getStatusTwilioConn();
                                if (twilioClientStatus.Key) _Travis.SpeakAsync(twilioClientStatus.Value);
                                else {
                                    if (String.IsNullOrWhiteSpace(twilioClientStatus.Value)) _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                                    else {
                                        whatsAppClient.connectTwilio(); // Make a new TwilioClient connection
                                        _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[3]);
                                    }
                                }
                            } else _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[1]);
                            break;
                        case "WhatsApp disconnect client":
                            if (whatsAppClient is null) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                            else {
                                if (!whatsAppClient.getStatusTwilioConn().Key) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                                else {
                                    whatsAppClient.disconnectTwilio();
                                    whatsAppClient = null; // Return to initial state
                                    _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[4]);
                                }
                            }
                            break;
                    }
                } else {
                    switch (e.Result.Text) {
                        case "Pause your speech":
                            if (_Travis.State.Equals(SynthesizerState.Speaking)) {
                                _Travis.Pause(); // Pause the current async speech qeue
                            } else _Travis.SpeakAsync(SpeechChoices.defBotComms[34]);
                            break;
                        case "Resume your speech":
                            if (_Travis.State.Equals(SynthesizerState.Paused)) {
                                _Travis.Resume(); // Resume the current async speech qeue
                            } else _Travis.SpeakAsync(SpeechChoices.defBotComms[18]);
                            break;
                        case "Stop talking":
                            if (_Travis.State.Equals(SynthesizerState.Speaking)) {
                                _Travis.SpeakAsyncCancelAll();
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[10]);
                            } else _Travis.SpeakAsync(SpeechChoices.defBotComms[34]);
                            break;
                        case "Stop listening":
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[11]);
                            _speechEngine.RecognizeAsyncCancel();
                            _standSpeechEngine.RecognizeAsync(RecognizeMode.Multiple);
                            mainAppForm.Hide();
                            break;
                        case "Hello Travis":
                            if (seatReturn) _Travis.SpeakAsync($"Hello back { Environment.UserName }");
                            else _Travis.SpeakAsync($"Hello { Environment.UserName }");
                            break;
                        case "How do you know my name":
                            // Environment.OSVersion entry
                            String osNameEntry = (String)Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion")?.GetValue("productName");
                            _Travis.SpeakAsync($"Because is the name you have defined in your { osNameEntry }");
                            break;
                        case "Cancel the pending alarm":
                            if (timerAlarm.Enabled) {
                                timerAlarm.Stop();
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[26]);
                            } else _Travis.SpeakAsync(SpeechChoices.defBotComms[27]);
                            break;
                        case "How long have i been on the computer":
                            long duration = sittingTimer.ElapsedMilliseconds / 1000;
                            if (duration < 60) _Travis.SpeakAsync($"{ duration } seconds");
                            else _Travis.SpeakAsync($"{ TimeSpan.FromSeconds(duration).TotalMinutes.ToString("0.00") } minutes");
                            if (duration > 3600) _Travis.SpeakAsync(SpeechChoices.defBotComms[5]);
                            break;
                        case "Ok, i will take a break":
                            if (sittingTimer.IsRunning) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[12]);
                                sittingTimer.Reset(); // Stop and reset sittingTimer
                            }
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[13]);
                            break;
                        case "Minimize to system tray":
                            // Minimize application and show the icon on the system tray
                            mainAppForm.Hide();
                            notifyIcon.Visible = true;
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[14]);
                            break;
                        case "Show up from system tray":
                            // Maximize application and remove the icon from the system tray
                            mainAppForm.Show();
                            notifyIcon.Visible = false;
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[6]);
                            break;
                        case "Check if JSON file its valid":
                            _Travis.SpeakAsync(localFileData.isValidJson() ? SpeechChoices.defBotComms[15] : SpeechChoices.defBotComms[16]);
                            break;
                        case "Read me a text file":
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[32]);
                            StringBuilder strTextFileToRead = ManageData.readTextFile();
                            if(!String.IsNullOrWhiteSpace(strTextFileToRead.ToString())) _Travis.SpeakAsync(strTextFileToRead.ToString());
                            break;
                        case "Convert me a text file to an audio file":
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[43]);
                            StringBuilder strTextFileToConvert = ManageData.readTextFile();
                            if (!String.IsNullOrWhiteSpace(strTextFileToConvert.ToString())) {
                                // Get audio file path
                                String audioFile = ManageData.convertTextIntoAudio();
                                if(!String.IsNullOrWhiteSpace($@"{ audioFile }")) {
                                    // Create a new local SpeechSynthesizer to convert text into audio
                                    SpeechSynthesizer localSpeechSynth = new SpeechSynthesizer();
                                    localSpeechSynth.Rate = _Travis.Rate;
                                    localSpeechSynth.Volume = _Travis.Volume;
                                    // Configure the audio output
                                    localSpeechSynth.SetOutputToWaveFile($@"{ audioFile }", new SpeechAudioFormatInfo(32000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
                                    // Create a SoundPlayer instance to play output audio file
                                    SoundPlayer soundPlayer = new SoundPlayer($@"{ audioFile }");
                                    // Speak the text file
                                    localSpeechSynth.Speak(strTextFileToConvert.ToString());
                                    soundPlayer.Play();
                                    localSpeechSynth.SetOutputToNull();
                                }
                            }
                            break;
                        case "Export speech commands examples":
                            ManageData.exportBotCommands();
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[31]);
                            break;
                        case "Reload all bot modes":
                            int oldCount = GrammarData.listBotVoices.Count;
                            Grammar cBotVoice = null;
                            foreach (Grammar grammar in _speechEngine.Grammars) {
                                if (grammar.Name.Equals("cBotVoice")) cBotVoice = grammar;
                            }
                            // Reload all bot voices options (installed + enabled)
                            _speechEngine.UnloadGrammar(cBotVoice);
                            OSData.getBotVoiceData();
                            _speechEngine.LoadGrammarAsync(GrammarData.changeBotVoice(currentCulture, new Choices(GrammarData.listBotVoices.Keys.ToArray())));
                            int newCount = GrammarData.listBotVoices.Count;
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[23]);
                            _Travis.SpeakAsync(newCount.Equals(oldCount) ? SpeechChoices.defBotComms[24] : $"From { oldCount } to { newCount }");
                            break;
                        case "Reload all grammar data":
                            if(discordClient != null) discordClient.disconnectDiscordBot();
                            discordClient = null; // Return to initial state
                            spotifyClient = null; // Return to initial state
                            if (whatsAppClient != null) whatsAppClient.disconnectTwilio();
                            whatsAppClient = null; // Return to initial state
                            _speechEngine.UnloadAllGrammars();
                            localAPIsData = GrammarData.loadGrammarData(_speechEngine, pathJSONSchema, pathJSONData, currentCulture);
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[17]);
                            break;
                        case "List me the sound input devices":
                            foreach (DirectSoundDeviceInfo soundDeviceInfo in DirectSoundDevices.GetCaptureDevices()) 
                                _Travis.SpeakAsync($"{ soundDeviceInfo.Description } is connected");
                            break;
                        case "List me the voices you have available":
                            foreach (var botVoice in GrammarData.listBotVoices)
                                _Travis.SpeakAsync($"{ botVoice.Key } with a { botVoice.Value.Split('-')[1] } culture");
                            break;
                        case "List me what you can do":
                            foreach (String str in SpeechChoices.strWhatICanDo) _Travis.SpeakAsync(str);
                            break;
                        case "Why do you need so many APIs":
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[30]);
                            break;
                        case "Travis Restart":
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[22]);
                            // Restart app
                            Thread.Sleep(2000);
                            Application.Restart();
                            Environment.Exit(0);
                            break;
                        case "Travis Shutdown":
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[19]);
                            Thread.Sleep(2000);
                            // Exit entire app
                            Application.Exit();
                            break;
                    }
                }

                // Change Bot Mode Speech
                /* ***************************************************************** */
                // Change the bots voice to a mode that is available (installed and enabled)
                if (e.Result.Semantics.ContainsKey("cBotVoice")) {
                    String tmpVoice = e.Result.Semantics["cBotVoice"].Value.ToString();
                    _Travis.SelectVoice(tmpVoice);
                    _Travis.SpeakAsync($"Hello { Environment.UserName }");
                }
                /* ***************************************************************** */

                // Bot Data Speechs
                /* ***************************************************************** */
                // Enable bot data of given category
                if (e.Result.Semantics.ContainsKey("enableBotData")) {
                    String tmpData = e.Result.Semantics["enableBotData"].Value.ToString();
                    if (botDataStatus.ContainsKey(tmpData)) {
                        if (botDataStatus[tmpData]) _Travis.SpeakAsync($"{ tmpData } data its already enabled");
                        else {
                            botDataStatus[tmpData] = true;
                            _Travis.SpeakAsync($"{ tmpData } data enabled");
                        }
                    } else _Travis.SpeakAsync($"{ tmpData } is not defined");
                }
                // Disable bot data of given category
                if (e.Result.Semantics.ContainsKey("disableBotData")) {
                    String tmpData = e.Result.Semantics["disableBotData"].Value.ToString();
                    if (botDataStatus.ContainsKey(tmpData)) {
                        if (!botDataStatus[tmpData]) _Travis.SpeakAsync($"{ tmpData } data its already disabled");
                        else {
                            botDataStatus[tmpData] = false;
                            _Travis.SpeakAsync($"{ tmpData } data disabled");
                        }
                    } else _Travis.SpeakAsync($"{ tmpData } is not defined");
                }
                // Status bot data of given category
                if (e.Result.Semantics.ContainsKey("sttBotData")) {
                    String tmpData = e.Result.Semantics["sttBotData"].Value.ToString();
                    if (botDataStatus.ContainsKey(tmpData)) {
                        String dataStatus = botDataStatus[tmpData] ? "enabled" : "disabled";
                        _Travis.SpeakAsync($"{ tmpData } data is { dataStatus }");
                    } else _Travis.SpeakAsync($"{ tmpData } is not defined");
                }
                // Describe the procedure for a given option
                if (e.Result.Semantics.ContainsKey("botProcedure")) {
                    String tmpData = e.Result.Semantics["botProcedure"].Value.ToString();
                    if (botDataStatus.ContainsKey(tmpData)) {
                        foreach (String str in GrammarData.strBotData[tmpData]) _Travis.SpeakAsync(str);
                    } else _Travis.SpeakAsync($"{ tmpData } is not defined");
                }
                /* ***************************************************************** */

                // Applications Speechs
                /* ***************************************************************** */
                // Check if they are opened
                if (e.Result.Semantics.ContainsKey("stateApp")) {
                    if (botDataStatus[GrammarData.strBotData.Keys.ToList()[0]]) {
                        String tmpApp = e.Result.Semantics["stateApp"].Value.ToString();
                        if (Process.GetProcessesByName(tmpApp).Length > 0) {
                            _Travis.SpeakAsync($"{ tmpApp } its running");
                        } else _Travis.SpeakAsync($"{ tmpApp } its not running");
                    } else _Travis.SpeakAsync($"Must enable { GrammarData.strBotData.Keys.ToList()[0] } data first");
                }
                // Close them
                if (e.Result.Semantics.ContainsKey("closeApp")) {
                    if (botDataStatus[GrammarData.strBotData.Keys.ToList()[0]]) {
                        String tmpApp = e.Result.Semantics["closeApp"].Value.ToString();
                        if (Process.GetProcessesByName(tmpApp).Length > 0) {
                            try {
                                foreach (Process proc in Process.GetProcessesByName(tmpApp)) proc.Kill();
                                applicationsLog.LogWrite(tmpApp, "Close"); // New app. action log
                                _Travis.SpeakAsync($"{ tmpApp } closed");
                            } catch (InvalidOperationException) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[45]);
                            } catch (Win32Exception) {
                                _Travis.SpeakAsync($"I couldn't close { tmpApp }");
                            }
                        } else _Travis.SpeakAsync($"{ tmpApp } its not running");
                    } else _Travis.SpeakAsync($"Must enable { GrammarData.strBotData.Keys.ToList()[0] } data first");
                }
                // Send data to apps (ex. discord webhook)
                if (e.Result.Semantics.ContainsKey("sendApp")) {
                    if (botDataStatus[GrammarData.strBotData.Keys.ToList()[0]]) {
                        String tmpApp = e.Result.Semantics["sendApp"].Value.ToString();
                        switch (tmpApp) {
                            case "Discord":
                                if(discordClient is null) _Travis.SpeakAsync(SpeechChoices.DiscordData.botComms[3]);
                                else {
                                    KeyValuePair<bool, String> discBotReadyToSendMssg =
                                        discordClient.discBotReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[3]], false, LoginState.LoggedIn, ConnectionState.Connected);
                                    if (discBotReadyToSendMssg.Key) {
                                        String _tmpFullResult = e.Result.Text;
                                        String _discMessage = _tmpFullResult.Remove(0, _tmpFullResult.IndexOf(tmpApp) + tmpApp.Length).Trim();
                                        _Travis.SpeakAsync(discordClient.sendTextToChannel(localAPIsData.DiscordServerIDs.ServerID.Value, localAPIsData.DiscordServerIDs.ChannelID.Value, 
                                            false, _discMessage.Trim()).Value);
                                    } else _Travis.SpeakAsync(discBotReadyToSendMssg.Value);
                                }
                                break;
                        }
                    } else _Travis.SpeakAsync($"Must enable { GrammarData.strBotData.Keys.ToList()[0] } data first");
                }
                /* ***************************************************************** */

                // Services Speechs
                /* ***************************************************************** */
                // Check service status
                if (e.Result.Semantics.ContainsKey("stateService")) {
                    if (botDataStatus[GrammarData.strBotData.Keys.ToList()[1]]) {
                        String tmpService = e.Result.Semantics["stateService"].Value.ToString();
                        _Travis.SpeakAsync($"{ tmpService } is { OSData.serviceStatus(new ServiceController(GrammarData.cServicesList[tmpService])) }");
                    } else _Travis.SpeakAsync($"Must enable { GrammarData.strBotData.Keys.ToList()[1] } data first");
                }
                // Stop service
                if (e.Result.Semantics.ContainsKey("stopService")) {
                    if (botDataStatus[GrammarData.strBotData.Keys.ToList()[1]]) {
                        String tmpService = e.Result.Semantics["stopService"].Value.ToString();
                        ServiceController tmp_StopService = new ServiceController(GrammarData.cServicesList[tmpService]);
                        if (tmp_StopService.Status == ServiceControllerStatus.Running) {
                            try {
                                // Stop the service, and wait until its status is "Stopped"
                                tmp_StopService.Stop();
                                tmp_StopService.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 5));
                                // New service action log
                                KeyValuePair<String, String> serviceLog = new KeyValuePair<String, String>(tmpService, GrammarData.cServicesList[tmpService]);
                                servicesLog.LogWrite(serviceLog, "Close");
                                _Travis.SpeakAsync($"{ tmpService } is stopped");
                            } catch (InvalidOperationException) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[46]);
                            }
                        } else _Travis.SpeakAsync($"{ tmpService } its not running");
                    } else _Travis.SpeakAsync($"Must enable { GrammarData.strBotData.Keys.ToList()[1] } data first");
                }
                /* ***************************************************************** */

                // Cryptos Speechs
                /* ***************************************************************** */
                // Check cryptos prices
                if (e.Result.Semantics.ContainsKey("checkCrypto")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpCrypto = e.Result.Semantics["checkCrypto"].Value.ToString();
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpCrypto), tmpCrypto.Length);
                        try {
                            // Get JSON data of the given crypto name from API
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getCryptoData(localAPIsData.RapidAPIKey, strParse.Trim()));
                            // Crypto token object
                            JToken token = ((JObject)jsonData)["symbol"];
                            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                            } else {
                                // Store crypto data (name, price and change on 24h)
                                JToken fullName = ((JObject)jsonData)["name"];
                                JToken currentPrice = ((JObject)jsonData)["market_data"]["current_price"]["usd"];
                                JToken last24Hours = ((JObject)jsonData)["market_data"]["price_change_percentage_24h"];
                                // Build out the reply
                                String upOrDown = Convert.ToDouble(last24Hours.ToString()) > 0 ? "up" : "down";
                                _Travis.SpeakAsync($"{ (String)fullName } it's { Convert.ToDouble(currentPrice.ToString()).ToString("0.00") } and it's { upOrDown } " +
                                    $"{ Convert.ToDouble(last24Hours.ToString()).ToString("0.0") } percent for the last 24 hours");
                            }
                        } catch(NullReferenceException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentNullException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                // Get total in crypto from fiat amount
                if (e.Result.Semantics.ContainsKey("fiatToCrypto")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String tmpCrypto = e.Result.Semantics["fiatToCrypto"].Value.ToString();
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpCrypto), tmpCrypto.Length);
                        String tmpCryptoCoin = strParse.Remove(strParse.IndexOf("with"));
                        try {
                            // Get JSON data of the given crypto name from API
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getCryptoData(localAPIsData.RapidAPIKey, tmpCryptoCoin.Trim()));
                            // Crypto token object
                            JToken token = ((JObject)jsonData)["symbol"];
                            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                            } else {
                                // Store crypto data (name, price and change on 24h)
                                JToken fullName = ((JObject)jsonData)["name"];
                                JToken currentPrice = ((JObject)jsonData)["market_data"]["current_price"]["usd"];
                                String valUSD = tmpFullResult.Remove(0, tmpFullResult.IndexOf("with") + 5).Replace("dollars", String.Empty).Trim();
                                double resCrypto = new StrNumberToInt(valUSD.Trim()).getIntFromString() / Convert.ToDouble(currentPrice.ToString());
                                _Travis.SpeakAsync($"You will get { resCrypto.ToString("0.0000") } { (String)fullName } for { valUSD.Trim() } dollars");
                            }
                        } catch (ApplicationException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                        } catch (NullReferenceException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentNullException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                /* ***************************************************************** */

                // Cities Speechs
                /* ***************************************************************** */
                // Check cities timezone
                if (e.Result.Semantics.ContainsKey("timeCity")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpCity = e.Result.Semantics["timeCity"].Value.ToString();
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpCity), tmpCity.Length);
                        try {
                            // Retrieve API data
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getTimeZoneData(localAPIsData.RapidAPIKey, strParse.Trim()));
                            JToken localTime = ((JObject)jsonData)["location"]["localtime"];
                            if (String.IsNullOrWhiteSpace((String)localTime)) _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                            else {
                                DateTime localTimeParsed;
                                if (DateTime.TryParse((String)localTime, out localTimeParsed)) {
                                    _Travis.SpeakAsync($"It's { localTimeParsed.ToString("HH:mm") } on { strParse.Trim() }");
                                }
                            }
                        } catch(WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentOutOfRangeException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                // Check weather conditions per city
                if (e.Result.Semantics.ContainsKey("weathCity")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpCity = e.Result.Semantics["weathCity"].Value.ToString();
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpCity), tmpCity.Length);
                        try {
                            // Retrieve API data
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getWeatherData(localAPIsData.RapidAPIKey, strParse.Trim()));
                            JToken weatherCondition = ((JObject)jsonData)["current"]["condition"]["text"];
                            if (String.IsNullOrWhiteSpace((String)weatherCondition)) _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                            else _Travis.SpeakAsync($"In { strParse.Trim() } seems to be { (String)weatherCondition }");
                        } catch(WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentOutOfRangeException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                // Check temperature conditions per city
                if (e.Result.Semantics.ContainsKey("tempCity")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpCity = e.Result.Semantics["tempCity"].Value.ToString();
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpCity), tmpCity.Length);
                        try {
                            // Retrieve API data
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getWeatherData(localAPIsData.RapidAPIKey, strParse.Trim()));
                            JToken weatherTempC = ((JObject)jsonData)["current"]["temp_c"];
                            if (String.IsNullOrWhiteSpace((String)weatherTempC)) _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                            else _Travis.SpeakAsync($"In { strParse.Trim() } it's { (String)weatherTempC } degrees");
                        } catch(WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentOutOfRangeException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                /* ***************************************************************** */

                // Football Teams Speechs
                /* ***************************************************************** */
                // Get team members of given team
                if (e.Result.Semantics.ContainsKey("teamMembers")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpTeam = e.Result.Semantics["teamMembers"].Value.ToString();
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String strParse = tmpFullResult.Remove(0, tmpFullResult.IndexOf(tmpTeam) + tmpTeam.Length);
                        try {
                            // Get JSON data of the given team next matches from API
                            object jsonData_Team = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 4, String.Empty, null, null, null, strParse.Trim()));
                            // Store team data (id)
                            JToken teamID = ((JObject)jsonData_Team)["api"]["teams"][0]["team_id"];
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 1, (String)teamID, null, null, DateTime.Now.Year, null));
                            foreach (var player in ((JObject)jsonData)["api"]["players"])
                                _Travis.SpeakAsync($"{ (String)((JObject)player)["player_name"] }, ({ (String)((JObject)player)["position"] })");
                        } catch (NullReferenceException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                        } catch(WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentOutOfRangeException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                // Getting next games of given team
                if (e.Result.Semantics.ContainsKey("teamGames")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpFullResult = e.Result.Text;
                        String tmpTeam = e.Result.Semantics["teamGames"].Value.ToString();
                        String strParse = tmpFullResult.Remove(0, tmpFullResult.IndexOf(tmpTeam) + 4);
                        try {
                            // Get JSON data of the given team next matches from API
                            object jsonData_Team = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 4, String.Empty, null, null, null, strParse.Trim()));
                            // Store team data (id)
                            JToken teamID = ((JObject)jsonData_Team)["api"]["teams"][0]["team_id"];
                            int _tmpNumGames = new StrNumberToInt(Regex.Replace(tmpFullResult.Remove(0, 17).Trim().Split()[0], @"[^0-9a-zA-Z\ ]+", "").Trim()).getIntFromString();
                            object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 2, (String)teamID, _tmpNumGames, null, null, null));
                            foreach (var fixtures in ((JObject)jsonData)["api"]["fixtures"]) {
                                String _tmpAgainst = ((JObject)fixtures)["homeTeam"]["team_id"].ToString() == (String)teamID ?
                                    ((JObject)fixtures)["awayTeam"]["team_name"].ToString() : ((JObject)fixtures)["homeTeam"]["team_name"].ToString();
                                String onStadium = !String.IsNullOrWhiteSpace(((JObject)fixtures)["venue"].ToString()) ? $"on { (String)((JObject)fixtures)["venue"] }" : String.Empty;
                                _Travis.SpeakAsync($"For { (String)((JObject)fixtures)["league"]["name"] } with { _tmpAgainst } { onStadium }");
                            }
                        } catch (ApplicationException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                        } catch (NullReferenceException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                        } catch (WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentOutOfRangeException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                // Getting next games of given team
                if (e.Result.Semantics.ContainsKey("predictNGame")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.RapidAPIKey)) {
                        String tmpFullResult = e.Result.Text;
                        String tmpTeam = e.Result.Semantics["predictNGame"].Value.ToString();
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpTeam), tmpTeam.Length);
                        try {
                            // Get JSON data of the given team next matches from API
                            object jsonData_Team = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 4, String.Empty, null, null, null, strParse.Trim()));
                            // Store team data (id)
                            JToken teamID = ((JObject)jsonData_Team)["api"]["teams"][0]["team_id"];
                            object jsonData_Game = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 2, (String)teamID, 1, null, null, null));
                            int _tmpMatchID = Convert.ToInt32(((JObject)jsonData_Game)["api"]["fixtures"][0]["fixture_id"].ToString());
                            String _tmpAgainst = ((JObject)jsonData_Game)["api"]["fixtures"][0]["homeTeam"]["team_id"].ToString() == (String)teamID ?
                                    ((JObject)jsonData_Game)["api"]["fixtures"][0]["awayTeam"]["team_name"].ToString() :
                                    ((JObject)jsonData_Game)["api"]["fixtures"][0]["homeTeam"]["team_name"].ToString();
                            object jsonData_Predict = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(localAPIsData.RapidAPIKey, 3, null, null, _tmpMatchID, null, null));
                            String _tmpPredict = ((JObject)jsonData_Predict)["api"]["predictions"][0]["advice"].ToString();
                            _Travis.SpeakAsync($"On the game of { strParse.Trim() } against { _tmpAgainst } the prediction is { _tmpPredict }");
                        } catch (NullReferenceException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                        } catch (WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        } catch (ArgumentOutOfRangeException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                /* ***************************************************************** */

                // New Timer Speechs
                /* ***************************************************************** */
                // Set a new timer value within the given number
                if (e.Result.Semantics.ContainsKey("newTimer")) {
                    String tmpFullResult = e.Result.Text;
                    String tmpTimer = e.Result.Semantics["newTimer"].Value.ToString();
                    // New alarm set option
                    if (GrammarData.cTimersList.FindIndex(str => str.Equals(tmpTimer)).Equals(0)) {
                        if (botDataStatus[GrammarData.strBotData.Keys.ToList()[GrammarData.strBotData.Keys.ToList().FindIndex(str => str.Equals("Alarm"))]]) {
                            if (timerAlarm.Enabled) _Travis.SpeakAsync(SpeechChoices.defBotComms[25]);
                            else {
                                try {
                                    String strParse_1 = tmpFullResult.Remove(0, 20);
                                    String strParse_2 = strParse_1.Substring(0, strParse_1.LastIndexOf(" ") < 0 ? 0 : strParse_1.LastIndexOf(" "));
                                    alarmTime = new StrNumberToInt(strParse_2.Trim()).getIntFromString() * 60; // in minutes
                                    timerAlarm.Start();
                                    _Travis.SpeakAsync($"New alarm set to { strParse_2.Trim() } minutes");
                                } catch (ApplicationException) {
                                    _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                                }
                            }
                        } else _Travis.SpeakAsync($"Must enable { GrammarData.strBotData.Keys.ToList()[GrammarData.strBotData.Keys.ToList().FindIndex(str => str.Equals("Alarm"))] } data first");
                    } else if (GrammarData.cTimersList.FindIndex(str => str.Equals(tmpTimer)).Equals(1)) {
                        // New seated timer set option
                        try {
                            String strParse_1 = tmpFullResult.Remove(0, 27);
                            String strParse_2 = strParse_1.Substring(0, strParse_1.LastIndexOf(" ") < 0 ? 0 : strParse_1.LastIndexOf(" "));
                            seatTime = new StrNumberToInt(strParse_2.Trim()).getIntFromString() * 60; // in minutes
                            _Travis.SpeakAsync($"Seated timer set to { strParse_2.Trim() } minutes");
                        } catch (ApplicationException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                }
                /* ***************************************************************** */

                // Users Steam Games Speech
                /* ***************************************************************** */
                // Get a list of friends that are currently playing the provided game
                if (e.Result.Semantics.ContainsKey("uSteamGames")) {
                    if (!String.IsNullOrWhiteSpace(localAPIsData.SteamKey.APIKey) && localAPIsData.SteamKey.SteamID64 != new long()) {
                        String tmpGame = e.Result.Semantics["uSteamGames"].Value.ToString();
                        // Get full speech request
                        String tmpFullResult = e.Result.Text;
                        String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpGame), tmpGame.Length);
                        try {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[21]);
                            // Retrieve api data
                            Dictionary<String, String> resultData = APIsFunctions.getAPISteamData(localAPIsData.SteamKey.APIKey, localAPIsData.SteamKey.SteamID64.ToString(), strParse.Trim()).Result;
                            if (resultData.Count.Equals(0)) {
                                _Travis.SpeakAsync($"No one in yours friends list is playing { strParse.Trim() } at the moment");
                            } else {
                                _Travis.SpeakAsync($"There is { resultData.Count } people playing { strParse.Trim() }");
                                foreach (var user in resultData) {
                                    if (user.Key.Equals(resultData.Last().Key)) _Travis.SpeakAsync(user.Key);
                                    else {
                                        _Travis.SpeakAsync(user.Key);
                                        _Travis.SpeakAsync(" and ");
                                    }
                                }
                            }
                        } catch (WebException) {
                            _Travis.SpeakAsync(SpeechChoices.defBotComms[28]);
                        }
                    } else _Travis.SpeakAsync(SpeechChoices.defBotComms[29]);
                }
                /* ***************************************************************** */

                // Search Engines Speechs
                /* ***************************************************************** */
                // Search something on the provided engine and show it on browser
                if (e.Result.Semantics.ContainsKey("searchEngines")) {
                    String tmpFullResult = e.Result.Text;
                    String tmpSearchEngine = e.Result.Semantics["searchEngines"].Value.ToString();
                    try {
                        String strParse_1 = tmpFullResult.Remove(0, 9);
                        String strParse_2 = strParse_1.Remove(strParse_1.IndexOf(tmpSearchEngine) - 4, tmpSearchEngine.Length + 4);
                        // Open result url on default browser
                        Process.Start($"{ GrammarData.cSearchEnginesList[tmpSearchEngine] }{ strParse_2.Trim() }");
                        _Travis.SpeakAsync($"Searching for { strParse_2.Trim() } on { tmpSearchEngine }");
                    } catch (Win32Exception excep) {
                        _Travis.SpeakAsyncCancelAll();
                        _Travis.SpeakAsync(excep.Message);
                    } catch (PlatformNotSupportedException) {
                        _Travis.SpeakAsync(SpeechChoices.defBotComms[44]);
                    }
                }
                /* ***************************************************************** */

                // Spotify Speechs
                /* ***************************************************************** */
                // Define a new volume for current playback Spotify
                if (e.Result.Semantics.ContainsKey("volumeSpotify")) {
                    if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                    else {
                        String tmpFullResult = e.Result.Text;
                        String tmpSpotify = e.Result.Semantics["volumeSpotify"].Value.ToString();
                        KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                        if (spotifyReadyTo.Key) {
                            try {
                                String strParse = tmpFullResult.Remove(tmpFullResult.IndexOf(tmpSpotify), tmpSpotify.Length + 15);
                                // Parse new value from speech
                                int _newValue = new StrNumberToInt(strParse.Trim()).getIntFromString();
                                spotifyClient.setVolumeSpotifyClient(_newValue);
                            } catch (ApplicationException) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                            }
                        } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                    }
                }
                // Get the top artists and tracks
                if (e.Result.Semantics.ContainsKey("dataSpotify")) {
                    if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                    else {
                        String tmpFullResult = e.Result.Text;
                        String tmpSpotify = e.Result.Semantics["dataSpotify"].Value.ToString();
                        KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], null);
                        if (spotifyReadyTo.Key) {
                            try {
                                String strParse_1 = tmpFullResult.Remove(0, GrammarData.strBotData.Keys.ToList()[4].Length + 15);
                                String strParse_2 = strParse_1.Remove(strParse_1.IndexOf(tmpSpotify));
                                // Parse new value from speech
                                int _newValue = new StrNumberToInt(strParse_2.Trim()).getIntFromString();
                                // Check for max. possible results
                                if(_newValue > 20) _Travis.SpeakAsync(SpeechChoices.defBotComms[40]);
                                else {
                                    if (tmpSpotify.Equals(GrammarData.cSpotifyTopDataList[0])) {
                                        // Instance and access the list of all artists
                                        List<String> topArtists = spotifyClient.getSpotifyTopArtists();
                                        for (int i = 0; i < _newValue; i++) {
                                            if (i.Equals(_newValue - 1)) _Travis.SpeakAsync($" and { topArtists[i] }");
                                            else _Travis.SpeakAsync(topArtists[i]);
                                        }
                                    } else if (tmpSpotify.Equals(GrammarData.cSpotifyTopDataList[1])) {
                                        // Instance and access the list of all tracks
                                        List<String> topTracks = spotifyClient.getSpotifyTopTracks();
                                        for (int i = 0; i < _newValue; i++) {
                                            if (i.Equals(_newValue - 1)) _Travis.SpeakAsync($" and { topTracks[i] }");
                                            else _Travis.SpeakAsync(topTracks[i]);
                                        }
                                    }
                                }
                            } catch (ApplicationException) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                            }
                        } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                    }
                }
                // New playlist on Spotify and add tracks from a random trending playlist to it
                if (e.Result.Semantics.ContainsKey("playlistSpotify")) {
                    if (spotifyClient is null) _Travis.SpeakAsync(SpeechChoices.SpotifyData.botComms[3]);
                    else {
                        String tmpFullResult = e.Result.Text;
                        String tmpSpotify = e.Result.Semantics["playlistSpotify"].Value.ToString();
                        KeyValuePair<bool, String> spotifyReadyTo = spotifyClient.spotifyReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[4]], GrammarData.strBotData.Keys.ToList()[4]);
                        if (spotifyReadyTo.Key) {
                            try {
                                String strParse_1 = tmpFullResult.Remove(0, GrammarData.strBotData.Keys.ToList()[4].Length + 24);
                                String strParse_2 = strParse_1.Remove(0, strParse_1.IndexOf("add") + 4);
                                String strParse_3 = strParse_2.Remove(0, strParse_2.IndexOf("trending") + 9);
                                // Playlist name
                                String playlistName = strParse_1.Remove(strParse_1.IndexOf("and"));
                                // Tracks number from speech
                                String numTracks = strParse_2.Remove(strParse_2.IndexOf("tracks"));
                                // Parse new value from speech
                                int _newValue = new StrNumberToInt(numTracks.Trim()).getIntFromString();
                                // Playlist category
                                String playlistCategory = strParse_3.Remove(strParse_3.IndexOf("playlist"));
                                KeyValuePair<bool, String> newPlaylistResult = spotifyClient.newPlaylistWithTracks(playlistName.Trim(), _newValue, playlistCategory.Trim());
                                if (newPlaylistResult.Key) _Travis.SpeakAsync($"New playlist created with the name of { newPlaylistResult.Value }");
                                else _Travis.SpeakAsync(newPlaylistResult.Value);
                            } catch (ApplicationException) {
                                _Travis.SpeakAsync(SpeechChoices.defBotComms[20]);
                            }
                        } else _Travis.SpeakAsync(spotifyReadyTo.Value);
                    }
                }
                /* ***************************************************************** */

                // Contacts Speechs
                /* ***************************************************************** */
                // Send an SMS message
                if (e.Result.Semantics.ContainsKey("contactSMS")) {
                    if (whatsAppClient is null) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                    else {
                        _Travis.SpeakAsync(SpeechChoices.defBotComms[21]);
                        _Travis.SpeakAsync(whatsAppClient.sendSMSMessage(botDataStatus[GrammarData.strBotData.Keys.ToList()[5]],
                            e.Result.Text, e.Result.Semantics["contactSMS"].Value.ToString()));
                    }
                }
                // Send a WhatsApp message
                if (e.Result.Semantics.ContainsKey("contactWhatsApp")) {
                    if (whatsAppClient is null) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                    else {
                        _Travis.SpeakAsync(SpeechChoices.defBotComms[21]);
                        _Travis.SpeakAsync(whatsAppClient.sendWhatsAppMessage(botDataStatus[GrammarData.strBotData.Keys.ToList()[5]],
                            e.Result.Text, e.Result.Semantics["contactWhatsApp"].Value.ToString()));
                    }
                }
                // Send a WhatsApp message (attached image(s))
                if (e.Result.Semantics.ContainsKey("imgsWhatsApp")) {
                    if (whatsAppClient is null) _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[2]);
                    else {
                        String tmpWhatsApp = e.Result.Semantics["imgsWhatsApp"].Value.ToString();
                        KeyValuePair<bool, String> whatsAppReadyToSend = whatsAppClient.whatsAppReadyToAction(botDataStatus[GrammarData.strBotData.Keys.ToList()[5]], tmpWhatsApp);
                        if (whatsAppReadyToSend.Key) {
                            _Travis.SpeakAsync(SpeechChoices.WhatsAppData.botComms[6]);
                            List<MessageResource.StatusEnum> msgResult = whatsAppClient.sendWhatsAppImgsTo(localAPIsData.ImgbbAPI, GrammarData.cContactsList[tmpWhatsApp]);
                            _Travis.SpeakAsync($"Yours images are { msgResult[0] }");
                        } else _Travis.SpeakAsync(whatsAppReadyToSend.Value);
                    }
                }
                /* ***************************************************************** */
            } else _Travis.SpeakAsync(SpeechChoices.defBotComms[42]);
        }

        private void recog_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e) {
            // Gets the best possible match for input to the speech recognizer
            List<RecognizedPhrase> sortedList = e.Result.Alternates.OrderByDescending(index => index.Confidence).ToList();
            if(!String.IsNullOrWhiteSpace(sortedList[0].Text)) _Travis.SpeakAsync($"I think what you meant was { sortedList[0].Text }");
        }
    }
}