using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Commands;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Speech_Recognition.Data.Applications
{
    public class DiscordData 
    {
        // Local SpeechSynthesizer
        private SpeechSynthesizer travisSon;
        // Discord client (REST)
        private DiscordSocketClient discordClient;
        // Discord default vars.
        private CommandService discordCommands;
        private InteractiveService discordInteractive;
        private IServiceProvider discordService;
        private APIsData localAPIsData;
        private bool[] iDiscordStates;

        // DiscordData constructor
        public DiscordData(SpeechSynthesizer _travis, APIsData _APIsData) {
            this.travisSon = _travis; // Access main instance SpeechSynthesizer
            this.discordClient = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Info });
            this.discordCommands = new CommandService(new CommandServiceConfig() { LogLevel = LogSeverity.Info, CaseSensitiveCommands = false });
            this.discordInteractive = new InteractiveService(discordClient);
            this.localAPIsData = _APIsData; // Access APIs data saved in the main instance
            this.discordService = new ServiceCollection()
                .AddSingleton(discordClient)
                .AddSingleton(discordCommands)
                .AddSingleton(discordInteractive)
                .AddSingleton(localAPIsData.RapidAPIKey)
                .BuildServiceProvider();
            this.discordCommands.AddModulesAsync(Assembly.GetEntryAssembly(), discordService);
            this.iDiscordStates = new bool[3];
        }

        public async void connectDiscordBot() {
            try {
                // Create connection between bot and discord server (API)
                await discordClient.LoginAsync(TokenType.Bot, localAPIsData.DiscordBotToken);
                await discordClient.StartAsync();
                await discordClient.SetStatusAsync(UserStatus.Online); // Setting online status
                // Setting listening status with application name
                await discordClient.SetGameAsync(Assembly.GetEntryAssembly().GetName().Name, null, ActivityType.Listening);
                changeDiscordFeedback(3, true); // Enable all bot commands interactions
            } catch (HttpException) {
                travisSon.SpeakAsync(SpeechChoices.DiscordData.botComms[6]);
            } catch (Exception) {
                travisSon.SpeakAsync(SpeechChoices.DiscordData.botComms[1]);
            }
        }

        public async void disconnectDiscordBot() {
            // Disconnection bot from the discord server (API)
            await discordClient.StopAsync();
            await discordClient.LogoutAsync();
        }

        public KeyValuePair<String, String> getDiscordBotConn() {
            // Get Discord bot login and connection status
            return new KeyValuePair<String, String>(discordClient.LoginState.ToString(), discordClient.ConnectionState.ToString());
        }
        public bool checkDiscordBotConn(LoginState _loginState, ConnectionState _connState) {
            return discordClient.LoginState.Equals(_loginState) && discordClient.ConnectionState.Equals(_connState) ? true : false;
        }

        public KeyValuePair<bool, String> changeDiscordFeedback(int _method, bool _enable) {
            if (_enable && !iDiscordStates[_method-1]) {
                switch (_method) {
                    case 1:
                        // Enable discord bot feedback regarding voice channel moves
                        discordClient.UserVoiceStateUpdated += client_UserVoiceStateUpdated;
                        iDiscordStates[_method - 1] = true;
                        break;
                    case 2:
                        // Enable discord bot feedback regarding user mentions in text channels
                        discordClient.MessageReceived += client_NewTagReceived;
                        iDiscordStates[_method - 1] = true;
                        break;
                    case 3:
                        // Enable discord bot feedback regarding bot commands in text channels
                        discordClient.MessageReceived += client_NewCommandReceived;
                        iDiscordStates[_method - 1] = true;
                        break;
                }
                return new KeyValuePair<bool, String>(true, SpeechChoices.defBotComms[36]);
            } else if(!_enable && iDiscordStates[_method - 1]) {
                switch (_method) {
                    case 1:
                        // Disable discord bot feedback regarding voice channel moves
                        discordClient.UserVoiceStateUpdated -= client_UserVoiceStateUpdated;
                        iDiscordStates[_method - 1] = false;
                        break;
                    case 2:
                        // Disable discord bot feedback regarding user mentions in text channels
                        discordClient.MessageReceived -= client_NewTagReceived;
                        iDiscordStates[_method - 1] = false;
                        break;
                    case 3:
                        // Disable discord bot feedback regarding bot commands in text channels
                        discordClient.MessageReceived -= client_NewCommandReceived;
                        iDiscordStates[_method - 1] = false;
                        break;
                }
                return new KeyValuePair<bool, String>(true, SpeechChoices.defBotComms[37]);
            } else if (_enable && iDiscordStates[_method - 1]) {
                return new KeyValuePair<bool, String>(false, SpeechChoices.defBotComms[38]);
            } else return new KeyValuePair<bool, String>(false, SpeechChoices.defBotComms[39]);
        }

        // Method for getting input and output activity on server voice channels
        private Task client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldChannel, SocketVoiceState newChannel) {
            // Get activity on discord voice channels
            if(oldChannel.VoiceChannel == null) {
                if (travisSon.State.Equals(SynthesizerState.Ready))
                    travisSon.SpeakAsync($"Someone joined { newChannel.VoiceChannel.Name }");
            } else if (newChannel.VoiceChannel == null) {
                if (travisSon.State.Equals(SynthesizerState.Ready))
                    travisSon.SpeakAsync($"Someone left { oldChannel.VoiceChannel.Name }");
            } return Task.CompletedTask; // Perform a completed task
        }

        // Method for getting user tags activity in text channels
        private Task client_NewTagReceived(SocketMessage message) {
            if (!message.Author.IsBot) {
                int argPos = 0; // For Discord bot commands
                SocketUserMessage discMessage = message as SocketUserMessage;
                // Detect whether the entered text will be associated with a command
                if (!discMessage.HasStringPrefix("!", ref argPos)) {
                    if (travisSon.State.Equals(SynthesizerState.Speaking)) travisSon.SpeakAsync(SpeechChoices.DiscordData.botComms[5]);
                    if (message.MentionedEveryone) travisSon.SpeakAsync($"{ message.Author.Username } is calling for everyone");
                    else {
                        foreach (var menUser in message.MentionedUsers) {
                            if (menUser.Id.Equals(localAPIsData.DiscordMyID)) 
                                travisSon.SpeakAsync($"{ message.Author.Username } tagged you on discord");
                        }
                    }
                }
            } return Task.CompletedTask;
        }

        // Method for getting command related activity for the bot
        private async Task client_NewCommandReceived(SocketMessage message) {
            // React to the webhook bot message
            if (message.Author.IsBot && !message.Author.Id.Equals(discordClient.CurrentUser.Id)) await message.AddReactionAsync(new Emoji("🤙"));
            else {
                int argPos = 0; // For Discord bot commands
                SocketUserMessage discMessage = message as SocketUserMessage;
                // Detect whether the entered text will be associated with a command
                if (discMessage.HasStringPrefix("!", ref argPos)) {
                    SocketCommandContext mssgContext = new SocketCommandContext(discordClient, discMessage);
                    await discordCommands.ExecuteAsync(mssgContext, argPos, discordService);
                }
            }
        }

        // Check if everything is ready to perform Discord bot actions
        public KeyValuePair<bool, String> discBotReadyToAction(bool _dataBool, bool _toConnect, LoginState _logStatus, ConnectionState _connStatus) {
            // Data status
            if (_dataBool) {
                // Check if token string its set
                if(String.IsNullOrWhiteSpace(localAPIsData.DiscordBotToken)) return new KeyValuePair<bool, String>(false, SpeechChoices.defBotComms[29]);
                // Discord bot connection status
                if (checkDiscordBotConn(_logStatus, _connStatus)) {
                    return new KeyValuePair<bool, String>(true, String.Empty);
                } else return new KeyValuePair<bool, String>(false, _toConnect ? SpeechChoices.DiscordData.botComms[2] : SpeechChoices.DiscordData.botComms[3]);
            } else return new KeyValuePair<bool, String>(false, $"Must enable { GrammarData.strBotData.Keys.ToList()[3] } data first");
        }

        public KeyValuePair<bool, String> getUsersInVoiceChannels(ulong _serverID) {
            if (!_serverID.Equals(0)) {
                // Get total of users in voice channels
                int countUserVChannels = 0;
                foreach (var vChannels in discordClient.GetGuild(_serverID).VoiceChannels)
                    countUserVChannels += vChannels.Users.Count;
                return new KeyValuePair<bool, String>(true, $"There are { countUserVChannels } people on voice channels");
            } else return new KeyValuePair<bool, String>(false, SpeechChoices.defBotComms[29]);
        }

        public KeyValuePair<bool, String> sendTextToChannel(ulong _serverID, ulong _channelID, bool _tagAll, String _text) {
            if(_serverID.Equals(0) || String.IsNullOrWhiteSpace(_text)) 
                return new KeyValuePair<bool, String>(false, SpeechChoices.defBotComms[29]);
            try {
                // Write some text on defined channel of a discord server
                SocketTextChannel channelToSend;
                if (_channelID.Equals(0)) channelToSend = discordClient.GetGuild(_serverID).DefaultChannel;
                else channelToSend = discordClient.GetGuild(_serverID).GetTextChannel(_channelID);
                Discord.Rest.RestUserMessage postText = channelToSend.SendMessageAsync(_text).Result;
                return new KeyValuePair<bool, String>(true, _tagAll ? SpeechChoices.DiscordData.botComms[0] : SpeechChoices.DiscordData.botComms[7]);
            } catch (NullReferenceException) {
                return new KeyValuePair<bool, String>(false, SpeechChoices.DiscordData.botComms[4]);
            }
        }
    }
}