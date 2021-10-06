using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speech_Recognition.Data;
using Speech_Recognition.Data.Functions;

namespace Speech_Recognition.Modules
{
    public class DiscordComms : ModuleBase<SocketCommandContext>
    {
        // Getting all commands through constructor param with AddSingleton()
        private readonly CommandService commandService;
        private String RapidAPIKey;
        public DiscordComms(CommandService _commandService, String _RapidAPIKey) { 
            commandService = _commandService;
            RapidAPIKey = _RapidAPIKey;
        }

        [Command("help")]
        public async Task getHelp() {
            // List of all available commands
            List<CommandInfo> allCommands = commandService.Commands.ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();
            foreach (CommandInfo command in allCommands) {
                if (!command.Name.Equals("help")) {
                    // Get the command Summary attribute information
                    string embedFieldText = command.Summary ?? "No description available\n";
                    embedBuilder.AddField(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.Name.ToLower()), embedFieldText);
                }
            }
            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("connection")]
        [Alias("conn", "status")]
        [Summary("🔗 Get current status of bot connection")]
        public async Task getConnection() {
            // Retrieve bot connection status
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = $"{ Context.Client.CurrentUser.Username } connection status";
            ConnectionState currentConn = Context.Client.ConnectionState;
            embedBuilder.AddField("Login", Context.Client.LoginState, true);
            embedBuilder.AddField("Connection", currentConn, true);
            if (currentConn.Equals(ConnectionState.Connected))
                embedBuilder.AddField("Latency", $"{ Context.Client.Latency } ms", true);
            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("price")]
        [Summary("🚀 Obtain current data related to the price of the given cryptocurrency")]
        public async Task getCryptoPrices([Remainder][Summary("Crypto coin")] String _coin) {
            // Initialize empty string builder for reply
            var strBuilder = new StringBuilder();
            // Embed layout reply
            var replyEmbed = new EmbedBuilder();
            // Add some options to the embed (like color and title)
            replyEmbed.WithColor(new Color(33, 150, 243));
            // Trigger typing state on current channel
            await Context.Channel.TriggerTypingAsync();
            try {
                // Get JSON data of the given crypto name from API
                object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getCryptoData(RapidAPIKey, _coin.Trim().ToLower()));
                // Crypto token object
                JToken token = ((JObject)jsonData)["symbol"];
                if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) {
                    replyEmbed.Description = "I think this doesn't match any cryptocurrency name...";
                } else {
                    // Store crypto data (name, price, change on 24h and 7d)
                    JToken fullName = ((JObject)jsonData)["name"];
                    JToken currentPrice = ((JObject)jsonData)["market_data"]["current_price"]["usd"];
                    JToken last24Hours = ((JObject)jsonData)["market_data"]["price_change_percentage_24h"];
                    JToken last7Days = ((JObject)jsonData)["market_data"]["price_change_percentage_7d"];
                    // Build out the reply
                    replyEmbed.Title = $"Price of { (String)fullName }";
                    strBuilder.AppendLine($"💵{ new String(' ', 3) }Current Price: **{ Convert.ToDouble(currentPrice.ToString()).ToString("0.00") } $**");
                    strBuilder.AppendLine();
                    double tmpLast24Hours = Convert.ToDouble(last24Hours.ToString());
                    String icon24Hours = tmpLast24Hours >= 0 ? "📈" : "📉";
                    strBuilder.AppendLine($"{ icon24Hours }{ new String(' ', 3) }24H Change: { tmpLast24Hours.ToString("0.00") } %");
                    strBuilder.AppendLine();
                    double tmpLast7Days = Convert.ToDouble(last7Days.ToString());
                    String icon7Days = tmpLast7Days >= 0 ? "📈" : "📉";
                    strBuilder.AppendLine($"{ icon7Days }{ new String(' ', 3) }7D Change: { tmpLast7Days.ToString("0.00") } %");
                    replyEmbed.Description = strBuilder.ToString();
                }
            } catch (NullReferenceException) {
                replyEmbed.Description = "My apologies, but it looks like there are invalid parameter(s) or an invalid API key";
            } catch (ArgumentNullException) {
                replyEmbed.Description = "I think this doesn't match any cryptocurrency name...";
            } catch (JsonReaderException) {
                replyEmbed.Description = "I couldn't get results for this command";
            }
            // Reply with the embed
            await ReplyAsync(null, false, replyEmbed.Build());
        }

        [Command("city")]
        [Alias("weather", "temperature")]
        [Summary("🌆 Get the timezone and weather conditions of the given city")]
        public async Task getCityData([Remainder][Summary("City name")] String _city) {
            // Initialize empty string builder for reply
            var strBuilder = new StringBuilder();
            // Embed layout reply
            var replyEmbed = new EmbedBuilder();
            // Add some options to the embed (like color and title)
            replyEmbed.WithColor(new Color(0, 150, 136));
            // Trigger typing state on current channel
            await Context.Channel.TriggerTypingAsync();
            // Get JSON data of the given city from APIs
            object tmpTimeZoneData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getTimeZoneData(RapidAPIKey, _city.Trim().ToLower()));
            object tmpWeatherData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getWeatherData(RapidAPIKey, _city.Trim().ToLower()));
            try {
                // Build out the reply
                replyEmbed.Title = $"Now in { CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_city.ToLower()) }...";
                JToken localTime = ((JObject)tmpTimeZoneData)["location"]["localtime"];
                DateTime localTimeParsed;
                if (DateTime.TryParse((String)localTime, out localTimeParsed)) {
                    strBuilder.AppendLine($"⏲{ new String(' ', 3) }Current Time: **{ localTimeParsed.ToString("HH:mm") }** hours");
                    strBuilder.AppendLine();
                }
                JToken weatherCondition = ((JObject)tmpWeatherData)["current"]["condition"]["text"];
                strBuilder.AppendLine($"⛅{ new String(' ', 3) }Current Weather: **{ (String)weatherCondition }**");
                strBuilder.AppendLine();
                JToken weatherTempC = ((JObject)tmpWeatherData)["current"]["temp_c"];
                strBuilder.AppendLine($"🌡{ new String(' ', 3) }Current Temperature: **{ (String)weatherTempC }**c");
                replyEmbed.Description = strBuilder.ToString();
            } catch (NullReferenceException) {
                replyEmbed.Description = "My apologies, but it looks like there are invalid parameter(s) or an invalid API key";
            } catch (WebException) {
                replyEmbed.Description = "Sorry boss, I think this is not a city...";
            } catch (JsonReaderException) {
                replyEmbed.Description = "I couldn't get results for this command";
            }
            // Reply with the embed
            await ReplyAsync(null, false, replyEmbed.Build());
        }

        [Command("team")]
        [Alias("squad")]
        [Summary("👨‍ Get all team members from a particular football team")]
        public async Task getTeamMembers([Remainder][Summary("Team name")] String _teamName) {
            // Embed layout reply
            var replyEmbed = new EmbedBuilder();
            // Add some options to the embed (like color and title)
            replyEmbed.WithColor(new Color(139, 195, 74));
            // Trigger typing state on current channel
            await Context.Channel.TriggerTypingAsync();
            // Get JSON data of the given team from API
            object jsonData_Team = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 4, String.Empty, null, null, null, _teamName.Trim().ToLower()));
            try {
                // Store team data (id, name and logo)
                JToken teamID = ((JObject)jsonData_Team)["api"]["teams"][0]["team_id"];
                JToken teamName = ((JObject)jsonData_Team)["api"]["teams"][0]["name"];
                JToken teamLogo = ((JObject)jsonData_Team)["api"]["teams"][0]["logo"];
                object jsonData_Squad = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 1, (String)teamID, null, null, DateTime.Now.Year, null));
                // Fill all variables within JSON data
                StringBuilder allGoalkeepers = new StringBuilder(),
                    allDefenders = new StringBuilder(),
                    allMidfielders = new StringBuilder(),
                    allAttackers = new StringBuilder();
                // Loop through all team members
                foreach (var player in ((JObject)jsonData_Squad)["api"]["players"]) {
                    JToken playerName = ((JObject)player)["player_name"];
                    JToken playerPosition = ((JObject)player)["position"];
                    // Fill sb's within players positions
                    switch ((String)playerPosition) {
                        case "Goalkeeper":
                            allGoalkeepers.AppendLine((String)playerName);
                            break;
                        case "Defender":
                            allDefenders.AppendLine((String)playerName);
                            break;
                        case "Midfielder":
                            allMidfielders.AppendLine((String)playerName);
                            break;
                        case "Attacker":
                            allAttackers.AppendLine((String)playerName);
                            break;
                    }
                }
                // Build out the reply
                replyEmbed.Title = (String)teamName;
                replyEmbed.ThumbnailUrl = (String)teamLogo;
                replyEmbed.AddField("🙌​​​​ Goalkeepers", allGoalkeepers.ToString(), true);
                replyEmbed.AddField("​🦶​​ Defenders", allDefenders.ToString(), true);
                replyEmbed.AddField("​🔥​​ ​Midfielders", allMidfielders.ToString(), true);
                replyEmbed.AddField("​👟​​​ Attackers", allAttackers.ToString(), true);
            } catch (NullReferenceException) {
                replyEmbed.Description = "My apologies, but it looks like there are invalid parameter(s) or an invalid API key";
            } catch (WebException) {
                replyEmbed.Description = "My apologies, I honestly don't know this team...";
            } catch (JsonReaderException) {
                replyEmbed.Description = "I couldn't get results for this command";
            }
            // Reply with the embed
            await ReplyAsync(null, false, replyEmbed.Build());
        }

        [Command("poll")]
        [Summary("❓ Create a new poll with, custom or not, answers using emojis")]
        public async Task newPoll([Remainder] String _fullArgs) {
            if(!(Context.Message.Channel is IDMChannel)) {
                EmbedBuilder pollEmbed = new EmbedBuilder();
                // Verify if exists question and answers
                String strQuestion = _fullArgs.Split('|')[0].Trim();
                if (_fullArgs.Contains('|')) {
                    String strAnswers = _fullArgs.Split('|')[1].Trim();
                    // Define embed title with question
                    pollEmbed.Title = strQuestion.ToUpper();
                    if (String.IsNullOrWhiteSpace(strAnswers)) {
                        // Default Yes/No poll
                        List<IEmote> emojiCodes = new List<IEmote>() { new Emoji("👍"), new Emoji("👎") };
                        // Get the poll option text
                        pollEmbed.AddField("Yes", emojiCodes[0], true);
                        pollEmbed.AddField("No", emojiCodes[1], true);
                        // Create the poll in the channel
                        IUserMessage sent = await ReplyAsync(null, false, pollEmbed.Build());
                        // Add reactions to the poll.
                        await sent.AddReactionsAsync(emojiCodes.ToArray());
                    } else {
                        try {
                            // Poll with custom options
                            String[] arrayAnswers = strAnswers.Split(',');
                            List<IEmote> emojiCodes = new List<IEmote>();
                            foreach (String option in arrayAnswers) {
                                // Get the poll option reaction
                                String emojiCode = option.Split('(', ')')[1].Trim();
                                // Get the poll option text (with fields)
                                pollEmbed.AddField(option.Split('(')[0].Trim(), emojiCode, true);
                                if (emojiCode.Contains(':')) {
                                    // If the reaction is a custom emoji, get the emoji code from the Guild
                                    String customEmojiName = emojiCode.Split(':')[1];
                                    emojiCodes.Add(Context.Guild.Emotes.First(x => x.Name == customEmojiName));
                                } else emojiCodes.Add(new Emoji(emojiCode));
                            }
                            // Create the poll in the channel
                            IUserMessage sent = await ReplyAsync(null, false, pollEmbed.Build());
                            // Add reactions to the poll.
                            await sent.AddReactionsAsync(emojiCodes.ToArray());
                        } catch(HttpException excep) {
                            pollEmbed.Description = $"{ excep.HttpCode }: { excep.Message }";
                            await ReplyAsync(null, false, pollEmbed.Build());
                        } catch (IndexOutOfRangeException) {
                            pollEmbed.Description = SpeechChoices.defBotComms[28];
                            await ReplyAsync(null, false, pollEmbed.Build());
                        }
                    }
                } else {
                    // Default Yes/No poll
                    // Define embed title with question
                    pollEmbed.Title = strQuestion.ToUpper();
                    List<IEmote> emojiCodes = new List<IEmote>() { new Emoji("👍"), new Emoji("👎") };
                    // Get the poll option text
                    pollEmbed.AddField("Yes", emojiCodes[0], true);
                    pollEmbed.AddField("No", emojiCodes[1], true);
                    // Create the poll in the channel
                    IUserMessage sent = await ReplyAsync(null, false, pollEmbed.Build());
                    // Add reactions to the poll.
                    await sent.AddReactionsAsync(emojiCodes.ToArray());
                }
            }
        }
    }
}