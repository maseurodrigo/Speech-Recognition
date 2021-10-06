using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Speech_Recognition.Data
{
    public struct APIsData {
        // Main data of application APIs
        [JsonProperty("RapidAPIKey")] public String RapidAPIKey { get; set; }
        [JsonProperty("DiscordBotToken")] public String DiscordBotToken { get; set; }
        [JsonProperty("DiscordMyID")] public ulong? DiscordMyID { get; set; }
        [JsonProperty("DiscordServerIDs")] public DiscordServerIDs DiscordServerIDs { get; set; }
        [JsonProperty("SteamKey")] public SteamKey SteamKey { get; set; }
        [JsonProperty("SpotifyAPI")] public SpotifyAPI SpotifyAPI { get; set; }
        [JsonProperty("TwilioAPI")] public TwilioAPI TwilioAPI { get; set; }
        [JsonProperty("imgbbAPI")] public String ImgbbAPI { get; set; }
        [JsonProperty("Applications")] public List<SystemApps> Applications { get; set; }
        [JsonProperty("Services")] public List<Services> Services { get; set; }
        [JsonProperty("SearchEngines")] public List<SearchEngines> SearchEngines { get; set; }
        [JsonProperty("Contacts")] public List<Contacts> Contacts { get; set; }
    }

    public class DiscordServerIDs {
        [JsonProperty("ServerID")] public ulong? ServerID { get; set; }
        [JsonProperty("ChannelID")] public ulong? ChannelID { get; set; }
    }

    public class SteamKey {
        [JsonProperty("APIKey")] public String APIKey { get; set; }
        [JsonProperty("SteamID64")] public ulong? SteamID64 { get; set; }
    }

    public class SpotifyAPI {
        [JsonProperty("ClientID")] public String ClientID { get; set; }
        [JsonProperty("ClientSecret")] public String ClientSecret { get; set; }
    }

    public class TwilioAPI {
        [JsonProperty("AuthToken")] public String AuthToken { get; set; }
        [JsonProperty("TwilioSID")] public String TwilioSID { get; set; }
        [JsonProperty("MessageServiceSID")] public String MessageServiceSID { get; set; }
    }

    public class SystemApps {
        [JsonProperty("Name")] public String ApplicationName { get; set; } 
    }

    public class Services {
        [JsonProperty("Name")] public String ServiceName { get; set; }
        [JsonProperty("Service")] public String Service { get; set; }
    }

    public class SearchEngines {
        [JsonProperty("Name")] public String SearchEngineName { get; set; }
        [JsonProperty("Url")] public String SearchEngineUrl { get; set; }
    }

    public class Contacts {
        [JsonProperty("Name")] public String WhatsAppName { get; set; }
        [JsonProperty("Number")] public String WhatsAppNumber { get; set; }
    }
}