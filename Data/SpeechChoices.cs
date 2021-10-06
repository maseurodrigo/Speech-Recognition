using System;
using System.Collections.Generic;

namespace Speech_Recognition.Data
{
    public class SpeechChoices
    {
        public class DiscordData {
            // Default discord responses
            [ContextStatic]
            public static String[] botComms = new String[] {
                "All tagged",
                "An error occurred while connecting the bot to discord",
                "Discord bot is already authenticated and connected",
                "Discord bot is not yet fully connected",
                "Chances are you haven't added the bot to the defined discord server",
                "While you were listening to me",
                "The service is currently unavailable",
                "Sent"
            };
            // User's speech default interactions
            [ContextStatic]
            public static Dictionary<String, bool> botChoices = new Dictionary<String, bool> {
                { "Discord connect bot", true },
                { "Discord disconnect bot", true },
                { "Discord bot connection status", true },
                { "Discord enable voice channels feedback", true },
                { "Discord disable voice channels feedback", true },
                { "Discord enable tags feedback", true },
                { "Discord disable tags feedback", true },
                { "Discord enable bot commands", true },
                { "Discord disable bot commands", true },
                { "Discord users in voice channels", true },
                { "Discord tag everyone", true }
            };
        }

        public class SpotifyData {
            // Default spotify responses
            [ContextStatic]
            public static String[] botComms = new String[] {
                "I need you to manually authorize my connection",
                "There is already a connection with spotify",
                "Your connection to spotify its done",
                "You haven't connected to spotify yet",
                "After completing your authentication, please make a new connection",
                "You have completed the authentication, now make a new connection",
                "I couldn't get the song name",
                "I can't find the playlist category you said"
            };
            // User's speech default interactions
            [ContextStatic]
            public static Dictionary<String, bool> botChoices = new Dictionary<String, bool> {
                { "Spotify connection status", true },
                { "Spotify connect me", true },
                { "Spotify play", true },
                { "Spotify pause", true },
                { "Spotify next", true },
                { "Spotify previous", true },
                { "Spotify mute", true },
                { "Spotify enable shuffle", true },
                { "Spotify disable shuffle", true },
                { "Spotify name of the current music", true },
                { "Spotify repeat the current music", true },
                { "Spotify list me some top categories", true }
            };
        }

        public class WhatsAppData {
            // Default whatsapp responses
            [ContextStatic]
            public static String[] botComms = new String[] {
                "Twilio client its connected",
                "Twilio client its already connected",
                "Twilio client its not connected yet",
                "Twilio client its connecting",
                "Twilio client its disconnecting",
                "You cannot send messages to yourself",
                "Select the images you want to send"
            };
            // User's speech default interactions
            [ContextStatic]
            public static Dictionary<String, bool> botChoices = new Dictionary<String, bool> {
                { "WhatsApp client connection status", true },
                { "WhatsApp connect client", true },
                { "WhatsApp disconnect client", true }
            };
        }

        // Bot default responses
        [ContextStatic]
        public static String[] defBotComms = new String[] {
            "Some APIs doesnt have data, ​​so its possible that some features dont return expected results",
            "System is up and ready!",
            "Data file is invalid, couldnt load data",
            "Its necessary to close JSON data file so i can load them",
            "Retrying in 5 seconds",
            "You should take a break Sir",
            "Maximized",
            "The time for the alarm you set has ended",
            "Do you need something ?",
            "Yes, I am here!",
            "I am sorry, I will be quiet",
            "If you need me just ask",
            "That's a good idea",
            "If you want me to stop listening just say",
            "Minimized",
            "Yes, its valid",
            "No, its not valid",
            "All grammars have been reloaded",
            "I have nothing more to say",
            "Okay, bye!",
            "I couldn't understand the numerical value",
            "Working on it",
            "Restarting",
            "Reload completed",
            "There were no changes",
            "There is already a pending alarm",
            "The pending alarm has been canceled",
            "There is no alarm set",
            "I'm sorry, but i don't know that identifier",
            "Invalid API key, therefore unable to finish request",
            "I don't need the APIs if you don't want to use their functions",
            "Speech commands exported",
            "Select the file you want me to read",
            "I can't read the text file you selected",
            "I wasn't talking",
            "The time for the alarm you set has ended!",
            "Enabled",
            "Disabled",
            "Already enabled",
            "Already disabled",
            "I don't have enough data for this amount",
            "Invalid numeric value",
            "I didn't realize what you said",
            "Select the file you want me to convert and then save the audio file",
            "This operation is exclusive to windows operating systems",
            "There is no process associated with this application",
            "The service was not found"
        };

        // User's speech default interactions
        [ContextStatic]
        public static Dictionary<String, bool> mainBotChoices = new Dictionary<String, bool> {
            { "Hello Travis", false },
            { "Travis", false },
            { "Wake up", false },
            { "Pause your speech", false },
            { "Resume your speech", false },
            { "Stop talking", false },
            { "Stop listening", false },
            { "Travis Shutdown", false },
            { "Travis Restart", false },
            { "How do you know my name", true },
            { "List me what you can do", false },
            { "List me the voices you have available", true },
            { "List me the sound input devices", true },
            { "Cancel the pending alarm", true },
            { "How long have i been on the computer", true },
            { "Ok, i will take a break", true },
            { "Minimize to system tray", true },
            { "Show up from system tray", true },
            { "Check if JSON file its valid", true },
            { "Reload all bot modes", true },
            { "Reload all grammar data", true },
            { "Export speech commands examples", false },
            { "Why do you need so many APIs", false },
            { "Read me a text file", true },
            { "Convert me a text file to an audio file", true }
        };

        // Bot operations list
        [ContextStatic]
        public static String[] strWhatICanDo = new String[] {
            "Get some of your operative system data, including sound input devices",
            "Get the status of applications and services running on the operating system, and close them if desired",
            "Get the timezone and current weather conditions for multiple locations",
            "Get some informations about a given football team, including his team members, his next matches and a prediction for his next match",
            "Get the price of some cryptocurrencies and their status",
            "Get a list of friends on steam who are currently playing a particular game",
            "Make a search for something in the given search engine, like google, youtube or twitch",
            "Connect and disconnect a discord bot and get his connection status",
            "With a discord bot i can get some informations in real time about a discord server, including the user number in voice channels and a user tags in text ones",
            "Set a new alarm, depending on the given time, and an alert for too much time at the computer",
            "Connect to spotify, and, according to the commands, control the playback on your spotify clients and spotify connected devices",
            "Connect a Twilio client, which allows you to send SMS or WhatsApp messages, including images, to a defined contact with the message you want",
            "I can read a text file and turn it into an audio file",
            "And, in simple words, change my voice"
        };
    }
}