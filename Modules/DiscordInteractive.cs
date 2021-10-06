using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speech_Recognition.Data;
using Speech_Recognition.Data.Functions;

namespace Speech_Recognition.Modules
{
    public class DiscordInteractive : InteractiveBase
    {
        private readonly InteractiveService _interactiveService;
        private String RapidAPIKey;
        public DiscordInteractive(String _RapidAPIKey) { this.RapidAPIKey = _RapidAPIKey; }

        [Command("convert", RunMode = RunMode.Async)]
        [Summary("💰 Get the value in the given cryptocurrency for the indicated dollar value")]
        public async Task convCryptoPrice([Remainder][Summary("Crypto coin")] String _coin) {
            // Initialize empty string builder for reply
            var strBuilder = new StringBuilder();
            // Embed layout reply
            var replyEmbed = new EmbedBuilder();
            // Add some options to the embed (like color and title)
            replyEmbed.WithColor(new Color(33, 150, 243));
            // Trigger typing state on current channel
            await Context.Channel.TriggerTypingAsync();
            try {
                // Waiting for an input of a valid numeric value
                await ReplyAsync("Amount of dollars ?");
                int dollars;
                var response = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
                if (response != null) {
                    if (int.TryParse(response.Content.Trim(), out dollars)) {
                        // Valid emoji reaction
                        var emoji = new Emoji("👍");
                        await response.AddReactionAsync(emoji);
                        // Trigger typing state on current channel
                        await Context.Channel.TriggerTypingAsync();
                        // Get JSON data of the given crypto name from API
                        object jsonData = JsonConvert.DeserializeObject<Object>(APIsFunctions.getCryptoData(RapidAPIKey, _coin));
                        // Crypto token object
                        JToken token = ((JObject)jsonData)["symbol"];
                        if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) {
                            replyEmbed.Description = "I think this doesn't match any cryptocurrency name...";
                        } else {
                            // Store crypto data (name, price)
                            JToken fullName = ((JObject)jsonData)["name"];
                            JToken currentPrice = ((JObject)jsonData)["market_data"]["current_price"]["usd"];
                            // Build out the reply
                            replyEmbed.Title = $"Dollars to { (String)fullName }";
                            double convCrypto = dollars / Convert.ToDouble(currentPrice.ToString());
                            strBuilder.AppendLine($"🔄{ new String(' ', 3) }You will get **{ convCrypto.ToString("0.0000") }** { (String)fullName } ({ (String)token }) for { dollars } dollars");
                            replyEmbed.Description = strBuilder.ToString();
                        }
                    } else {
                        // Invalid emoji reaction
                        var emoji = new Emoji("👎");
                        await response.AddReactionAsync(emoji);
                        replyEmbed.Description = SpeechChoices.defBotComms[41];
                    }
                } else await ReplyAsync($"{ Context.User.Mention } you didnt reply before the timeout"); // response timeout
            } catch(NullReferenceException) {
                replyEmbed.Description = "My apologies, but it looks like there are invalid parameter(s) or an invalid API key";
            } catch (ArgumentNullException) {
                replyEmbed.Description = "I think this doesn't match any cryptocurrency name...";
            } catch (JsonReaderException) {
                replyEmbed.Description = "I couldn't get results for this command";
            }
            // Reply with the embed
            await ReplyAsync(null, false, replyEmbed.Build());
        }

        [Command("matches", RunMode = RunMode.Async)]
        [Summary("📅‍ Get all scheduled matches for a particular football team")]
        public async Task getTeamGames([Summary("Match at home or away")] String _typeMatch, [Remainder][Summary("Team name")] String _teamName) {
            // Initialize empty string builder for reply
            var strBuilder = new StringBuilder();
            // Embed layout reply
            var replyEmbed = new EmbedBuilder();
            // Add some options to the embed (like color and title)
            replyEmbed.WithColor(new Color(139, 195, 74));
            // Trigger typing state on current channel
            await Context.Channel.TriggerTypingAsync();
            // Get JSON data of the given team next matches from API
            object jsonData_Team = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 4, String.Empty, null, null, null, _teamName.Trim().ToLower()));
            try {
                List<String> typeOptions = new List<String>() { "NEXT", "LAST" };
                // Checking if "typeMatch" param its valid
                if (typeOptions.Contains(_typeMatch.ToUpper())) {
                    // Waiting for an input of a valid numeric value
                    await ReplyAsync("Number of matches ?");
                    int countMatches;
                    var response = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
                    if (response != null) {
                        if (int.TryParse(response.Content.Trim(), out countMatches)) {
                            if(countMatches > 25) {
                                // Invalid emoji reaction
                                var emoji = new Emoji("👎");
                                await response.AddReactionAsync(emoji);
                                replyEmbed.Description = SpeechChoices.defBotComms[41];
                            } else {
                                // Valid emoji reaction
                                var emoji = new Emoji("👍");
                                await response.AddReactionAsync(emoji);
                                // Trigger typing state on current channel
                                await Context.Channel.TriggerTypingAsync();
                                // Store team data (id, name and logo)
                                JToken teamID = ((JObject)jsonData_Team)["api"]["teams"][0]["team_id"];
                                JToken teamName = ((JObject)jsonData_Team)["api"]["teams"][0]["name"];
                                JToken teamLogo = ((JObject)jsonData_Team)["api"]["teams"][0]["logo"];
                                if (_typeMatch.ToUpper().Equals(typeOptions[0])) {
                                    // Next matches
                                    object jsonData_Matches = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 2, (String)teamID, countMatches, null, null, null));
                                    // Loop through all team matches
                                    foreach (var match in ((JObject)jsonData_Matches)["api"]["fixtures"]) {
                                        String _tmpAgainst = ((JObject)match)["homeTeam"]["team_id"].ToString() == (String)teamID ? 
                                            ((JObject)match)["awayTeam"]["team_name"].ToString() : ((JObject)match)["homeTeam"]["team_name"].ToString();
                                        JToken competition = ((JObject)match)["league"]["name"];
                                        JToken stadium = ((JObject)match)["venue"];
                                        // Fill StringBuilders with match data
                                        strBuilder.AppendLine($"🆚{ new String(' ', 2) }**{ _tmpAgainst }** on **{ (String)stadium }** for **{ (String)competition }**");
                                        strBuilder.AppendLine();
                                    }
                                    // Build out the reply
                                    replyEmbed.Title = $"Next matches of { (String)teamName }";
                                    replyEmbed.ThumbnailUrl = (String)teamLogo;
                                    replyEmbed.Description = strBuilder.ToString();
                                } else {
                                    // Last matches
                                    object jsonData_Matches = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 5, (String)teamID, countMatches, null, null, null));
                                    // Loop through all team matches
                                    foreach (var match in ((JObject)jsonData_Matches)["api"]["fixtures"]) {
                                        KeyValuePair<bool, String> _tmpAgainst = ((JObject)match)["homeTeam"]["team_id"].ToString() == (String)teamID ?
                                                        new KeyValuePair<bool, String>(true, ((JObject)match)["awayTeam"]["team_name"].ToString()) : 
                                                        new KeyValuePair<bool, String>(false, ((JObject)match)["homeTeam"]["team_name"].ToString());
                                        JToken competition = ((JObject)match)["league"]["name"];
                                        JToken goalsHomeT = ((JObject)match)["goalsHomeTeam"];
                                        JToken goalsAwayT = ((JObject)match)["goalsAwayTeam"];
                                        JToken fullTimeResult = ((JObject)match)["score"]["fulltime"];
                                        String resultState = String.Empty;
                                        // Different results possibilites
                                        if (_tmpAgainst.Key) {
                                            if (Convert.ToInt32((String)goalsHomeT) > Convert.ToInt32((String)goalsAwayT)) resultState = "win";
                                            else if (Convert.ToInt32((String)goalsHomeT) < Convert.ToInt32((String)goalsAwayT)) resultState = "lose";
                                            else resultState = "draw";
                                        } else {
                                            if (Convert.ToInt32((String)goalsHomeT) > Convert.ToInt32((String)goalsAwayT)) resultState = "lose";
                                            else if (Convert.ToInt32((String)goalsHomeT) < Convert.ToInt32((String)goalsAwayT)) resultState = "win";
                                            else resultState = "draw";
                                        }
                                        // Fill StringBuilders with match data
                                        strBuilder.AppendLine($"🆚{ new String(' ', 2) }**{ _tmpAgainst.Value }** with a **{ resultState }** of **{ (String)fullTimeResult }** for **{ (String)competition }**");
                                        strBuilder.AppendLine();
                                    }
                                    // Build out the reply
                                    replyEmbed.Title = $"Last matches of { (String)teamName }";
                                    replyEmbed.ThumbnailUrl = (String)teamLogo;
                                    replyEmbed.Description = strBuilder.ToString();
                                }
                            }
                        } else {
                            // Invalid emoji reaction
                            var emoji = new Emoji("👎");
                            await response.AddReactionAsync(emoji);
                            replyEmbed.Description = SpeechChoices.defBotComms[41];
                        }
                    } else await ReplyAsync($"{ Context.User.Mention } you didnt reply before the timeout"); // response timeout
                } else replyEmbed.Description = "Sorry, but I need a 'next' or 'last' parameter";
            } catch(NullReferenceException) {
                replyEmbed.Description = "My apologies, but it looks like there are invalid parameter(s) or an invalid API key";
            } catch (WebException) {
                replyEmbed.Description = "My apologies, I honestly don't know this team...";
            } catch (JsonReaderException) {
                replyEmbed.Description = "I couldn't get results for this command";
            }
            // Reply with the embed
            await ReplyAsync(null, false, replyEmbed.Build());
        }

        [Command("predict", RunMode = RunMode.Async)]
        [Alias("tip")]
        [Summary("🔮 Get predictions for the next scheduled matches of a particular football team")]
        public async Task getTeamGamesPredict([Remainder][Summary("Team name")] String _teamName) {
            // Initialize empty string builder for reply
            var strBuilder = new StringBuilder();
            // Embed layout reply
            var replyEmbed = new EmbedBuilder();
            // Add some options to the embed (like color and title)
            replyEmbed.WithColor(new Color(139, 195, 74));
            // Trigger typing state on current channel
            await Context.Channel.TriggerTypingAsync();
            // Get JSON data of the given team next matches from API
            object jsonData_Team = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 4, String.Empty, null, null, null, _teamName.Trim().ToLower()));
            try {
                // Waiting for an input of a valid numeric value
                await ReplyAsync("Number of matches ?");
                int countMatches;
                var response = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
                if (response != null) {
                    if (int.TryParse(response.Content.Trim(), out countMatches)) {
                        if(countMatches > 25) {
                            // Invalid emoji reaction
                            var emoji = new Emoji("👎");
                            await response.AddReactionAsync(emoji);
                            replyEmbed.Description = SpeechChoices.defBotComms[41];
                        } else {
                            // Valid emoji reaction
                            var emoji = new Emoji("👍");
                            await response.AddReactionAsync(emoji);
                            // Trigger typing state on current channel
                            await Context.Channel.TriggerTypingAsync();
                            // Store team data (id, name and logo)
                            JToken teamID = ((JObject)jsonData_Team)["api"]["teams"][0]["team_id"];
                            JToken teamName = ((JObject)jsonData_Team)["api"]["teams"][0]["name"];
                            JToken teamLogo = ((JObject)jsonData_Team)["api"]["teams"][0]["logo"];
                            object jsonData_Game = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 2, (String)teamID, countMatches, null, null, null));
                            // Loop through all team matches
                            foreach (var match in ((JObject)jsonData_Game)["api"]["fixtures"]) {
                                // Current match ID
                                int _tmpMatchID = Convert.ToInt32(((JObject)match)["fixture_id"].ToString());
                                // JSON data from current match
                                object jsonData_Predict = JsonConvert.DeserializeObject<Object>(APIsFunctions.getAPIFootballData(RapidAPIKey, 3, null, null, _tmpMatchID, null, String.Empty));
                                string _tmpAgainst = ((JObject)match)["homeTeam"]["team_id"].ToString() == (String)teamID ?
                                                ((JObject)match)["awayTeam"]["team_name"].ToString() : ((JObject)match)["homeTeam"]["team_name"].ToString();
                                string _tmpPredict = ((JObject)jsonData_Predict)["api"]["predictions"][0]["advice"].ToString();
                                // Fill StringBuilders with match data
                                strBuilder.AppendLine($"🆚{ new String(' ', 2) }**{ _tmpAgainst }** with a prediction of **{ _tmpPredict }**");
                                strBuilder.AppendLine();
                            }
                            // Build out the reply
                            replyEmbed.Title = $"Next matches of { (String)teamName }";
                            replyEmbed.ThumbnailUrl = (String)teamLogo;
                            replyEmbed.Description = strBuilder.ToString();
                        }
                    } else {
                        // Invalid emoji reaction
                        var emoji = new Emoji("👎");
                        await response.AddReactionAsync(emoji);
                        replyEmbed.Description = SpeechChoices.defBotComms[41];
                    }
                } else await ReplyAsync($"{ Context.User.Mention } you didnt reply before the timeout"); // response timeout
            } catch(NullReferenceException) {
                replyEmbed.Description = "My apologies, but it looks like there are invalid parameter(s) or an invalid API key";
            } catch (WebException) {
                replyEmbed.Description = "My apologies, I honestly don't know this team...";
            } catch (JsonReaderException) {
                replyEmbed.Description = "I couldn't get results for this command";
            }
            // Reply with the embed
            await ReplyAsync(null, false, replyEmbed.Build());
        }
    }
}