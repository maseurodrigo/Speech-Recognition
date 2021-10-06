using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Speech_Recognition.Data.Applications
{
    public class SpotifyData
    {
        // Local SpeechSynthesizer
        private SpeechSynthesizer travisSon;
        // SpotifyClient
        private SpotifyClient spotifyClient;
        // Spotify default vars.
        private String urlURI, iClientID, iClientSecret, tokenAPISpotify;
        private HttpListener mHTTPListener;
        private Thread responseThread;

        // SpotifyData constructor
        public SpotifyData(SpeechSynthesizer _travis, String _urlURI, String _clientID, String _clientSecret) {
            this.travisSon = _travis; // Access main instance SpeechSynthesizer
            this.urlURI = _urlURI;
            this.iClientID = _clientID;
            this.iClientSecret = _clientSecret;
            this.mHTTPListener = new HttpListener();
            mHTTPListener.IgnoreWriteExceptions = true;
        }

        // Method to call a browser Spotify authorization
        // HttpListener and a user response thread
        public async void setupSpotifyAuth() { await setupWebServer(urlURI); }

        private Task setupWebServer(String _urlURI) {
            return Task.Factory.StartNew(() => {
                // Build spotify authorization url
                String baseURL = String.Concat("https://accounts.spotify.com/authorize?",
                    "client_id=", iClientID,
                    "&response_type=", "code",
                    "&redirect_uri=", _urlURI,
                    // Read and Write access to a user’s playback state
                    // Read access to a user’s currently playing content
                    // Read access to a user's top artists and tracks
                    // Write access to a user's private playlists
                    "&scope=", "user-modify-playback-state user-read-playback-state user-read-currently-playing user-top-read playlist-modify-private");
                // Run URL on default browser
                Process.Start(baseURL);
                // Check if the response Thread and the HttpListener are already active
                if (!mHTTPListener.IsListening && responseThread is null) {
                    mHTTPListener.Prefixes.Add(_urlURI);
                    mHTTPListener.Start();
                    responseThread = new Thread(webServerThread);
                    responseThread.Start(); // Start the response thread
                }
            });
        }

        private void webServerThread() {
            try {
                HttpListenerContext mHTTPContext = mHTTPListener.GetContext();
                HttpListenerResponse mHTTPResponse = mHTTPContext.Response;
                // Parse default URL with params
                Uri myUri = new Uri(mHTTPContext.Request.Url.AbsoluteUri);
                tokenAPISpotify = HttpUtility.ParseQueryString(myUri.Query).Get("code");
                String cBackError = HttpUtility.ParseQueryString(myUri.Query).Get("error");
                KeyValuePair<bool, String> reqResult = new KeyValuePair<bool, String>(
                    !String.IsNullOrWhiteSpace(tokenAPISpotify) ? true : false,
                    !String.IsNullOrWhiteSpace(tokenAPISpotify) ? tokenAPISpotify : cBackError);
                // Default localhost definitions
                // Get the bytes to response
                String strBackground = reqResult.Key ? "background-color: #c8e6c9;" : "background-color: #ffcdd2;";
                String pageHTML = $"<html><head><title>Speech Recognition - Spotify</title></head><body style='{ strBackground }'></body></html>";
                byte[] _responseArray = Encoding.UTF8.GetBytes(pageHTML);
                // Get a response stream and write the response to it
                mHTTPResponse.ContentLength64 = _responseArray.Length;
                Stream mHTTPOutput = mHTTPResponse.OutputStream;
                mHTTPOutput.Write(_responseArray, 0, _responseArray.Length);
                mHTTPResponse.KeepAlive = false;
                // Close context connection
                mHTTPOutput.Close();
                mHTTPListener.Close();
            } catch (ObjectDisposedException excep) {
                travisSon.SpeakAsyncCancelAll();
                travisSon.SpeakAsync(excep.Message);
            }
        }

        // To call after webserver response
        public KeyValuePair<bool, String> setSpotifyClient() {
            // Exchange authorization code for access code to spotify API
            if (String.IsNullOrWhiteSpace(tokenAPISpotify)) {
                return new KeyValuePair<bool, String>(false, SpeechChoices.SpotifyData.botComms[4]);
            } else {
                SpotifyClientConfig mainConfig = SpotifyClientConfig.CreateDefault();
                var authRequest = new AuthorizationCodeTokenRequest(iClientID, iClientSecret, tokenAPISpotify, new Uri(urlURI));
                var oAuthClient = new OAuthClient(mainConfig).RequestToken(authRequest);
                // Setup new SpotifyClient within user authorization data
                spotifyClient = new SpotifyClient(mainConfig.WithToken(oAuthClient.Result.AccessToken));
                return spotifyClient is null ? new KeyValuePair<bool, String>(false, SpeechChoices.SpotifyData.botComms[4]) : 
                    new KeyValuePair<bool, String>(true, SpeechChoices.SpotifyData.botComms[2]);
            }
        }

        public KeyValuePair<bool, bool?> checkSpotifyClient(bool _incClient) {
            // Check Spotify client connection status
            if (_incClient) {
                // All True: Ready to connect (disconnected)
                return new KeyValuePair<bool, bool?>(String.IsNullOrWhiteSpace(tokenAPISpotify) ? true : false, spotifyClient == null ? true : false);
            } else return new KeyValuePair<bool, bool?>(String.IsNullOrWhiteSpace(tokenAPISpotify) ? true : false, null);
        }

        // Check if everything is ready to perform Spotify connection
        public KeyValuePair<bool, String> spotifyReadyToConnect(bool _dataBool) {
            // Data status
            if (_dataBool) {
                // Client IDs data
                if (!String.IsNullOrWhiteSpace(iClientID) && !String.IsNullOrWhiteSpace(iClientSecret)) {
                    // Spotify client connection
                    KeyValuePair<bool, bool?> spotifyClientStatus = checkSpotifyClient(true);
                    if (!spotifyClientStatus.Key && spotifyClientStatus.Value.Value) {
                        return new KeyValuePair<bool, String>(true, SpeechChoices.SpotifyData.botComms[5]); // Already authenticated but client not connected
                    } else if (!spotifyClientStatus.Key && !spotifyClientStatus.Value.Value) {
                        return new KeyValuePair<bool, String>(false, SpeechChoices.SpotifyData.botComms[2]); // Already authenticated and client connected
                    } else return new KeyValuePair<bool, String>(true, String.Empty); // Not authenticated neither client connected
                } else return new KeyValuePair<bool, String>(false, SpeechChoices.defBotComms[29]);
            } else return new KeyValuePair<bool, String>(false, $"Must enable { GrammarData.strBotData.Keys.ToList()[4] } data first");
        }

        // Check if everything is ready to perform Spotify action
        public KeyValuePair<bool, String> spotifyReadyToAction(bool _dataBool, String _processName) {
            // Data status
            if (_dataBool) {
                // Spotify running
                if ((!String.IsNullOrWhiteSpace(_processName) && Process.GetProcessesByName(_processName).Length > 0) || String.IsNullOrWhiteSpace(_processName)) {
                    // Spotify client connection
                    KeyValuePair<bool, bool?> spotifyClientStatus = checkSpotifyClient(true);
                    if (!spotifyClientStatus.Key || !spotifyClientStatus.Value.Value) {
                        return new KeyValuePair<bool, String>(true, String.Empty);
                    } else return new KeyValuePair<bool, String>(false, SpeechChoices.SpotifyData.botComms[3]);
                } else return new KeyValuePair<bool, String>(false, $"{ GrammarData.strBotData.Keys.ToList()[4] } its not running");
            } else return new KeyValuePair<bool, String>(false, $"Must enable { GrammarData.strBotData.Keys.ToList()[4] } data first");
        }

        // Play command for Spotify
        public void playSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.ResumePlayback(); }
        // Pause command for Spotify
        public void pauseSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.PausePlayback(); }
        // Skip to next command for Spotify
        public void skipNextSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.SkipNext(); }
        // Skip to previous command for Spotify
        public void skipPreviousSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.SkipPrevious(); }
        // Enable shuffle command for Spotify
        public void enableSuffleSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.SetShuffle(new PlayerShuffleRequest(true)); }
        // Disable shuffle command for Spotify
        public void disableSuffleSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.SetShuffle(new PlayerShuffleRequest(false)); }
        // Mute command for Spotify
        public void muteSpotifyClient() { if (!(spotifyClient is null)) spotifyClient.Player.SetVolume(new PlayerVolumeRequest(0)); }
        // Set new volume command for Spotify
        public void setVolumeSpotifyClient(int _newValue) { if (!(spotifyClient is null)) spotifyClient.Player.SetVolume(new PlayerVolumeRequest(_newValue)); }
        // Replay the current music. Returning to position 0 ms
        public void repeatCurrentMusic() { if (!(spotifyClient is null)) spotifyClient.Player.SeekTo(new PlayerSeekToRequest(0)); }

        // Get name of the music thats currently playing on the playback
        public String getCurrentMusicName() {
            if (!(spotifyClient is null)) return ((FullTrack)spotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest()).Result.Item).Name;
            return String.Empty;
        }

        // Get a list of Spotify's top categories
        public List<String> getSpotifyTopCategories() {
            if (!(spotifyClient is null)) {
                List<String> rTopList = new List<String>();
                // Loop through all Spotify's categories results from Spotify API
                foreach (var category in spotifyClient.Browse.GetCategories().Result.Categories.Items) rTopList.Add(category.Name);
                return rTopList;
            } else return new List<String>();
        }

        // Get the most heard artists from current Spotify user
        public List<String> getSpotifyTopArtists() {
            if (!(spotifyClient is null)) {
                List<String> rTopList = new List<String>();
                // Loop through all GetTopArtists results from Spotify API
                foreach (var artist in spotifyClient.Personalization.GetTopArtists().Result.Items) rTopList.Add(artist.Name);
                return rTopList;
            } else return new List<String>();
        }

        // Get the most heard tracks from current Spotify user
        public List<String> getSpotifyTopTracks() {
            if (!(spotifyClient is null)) {
                List<String> rTopList = new List<String>();
                // Loop through all GetTopTracks results from Spotify API
                foreach (var track in spotifyClient.Personalization.GetTopTracks().Result.Items) rTopList.Add(track.Name);
                return rTopList;
            } else return new List<String>();
        }

        public KeyValuePair<bool, String> newPlaylistWithTracks(String _name, int _numTracks, String _category) {
            // Spotify trending playlists
            if (!(spotifyClient is null)) {
                // Store Spotify playlist ID from speech category
                KeyValuePair<bool, String> categPlaylist = new KeyValuePair<bool, String>(false, String.Empty);
                // Spotify playlist categories
                foreach (var category in spotifyClient.Browse.GetCategories().Result.Categories.Items) {
                    // If found, store a new category data
                    if (category.Name.ToUpper().Contains(_category.ToUpper())) categPlaylist = new KeyValuePair<bool, String>(true, category.Id);
                }
                if (categPlaylist.Key) {
                    int tmpTotal = spotifyClient.Browse.GetFeaturedPlaylists().Result.Playlists.Total.Value;
                    // Category from user speech
                    SimplePlaylist sortedPlaylist = spotifyClient.Browse.GetCategoryPlaylists(categPlaylist.Value).Result.Playlists.Items[new Random().Next(tmpTotal)];
                    List<String> uriList = new List<String>();
                    int totalTracks = (_numTracks > sortedPlaylist.Tracks.Total) ? sortedPlaylist.Tracks.Total.Value : _numTracks;
                    // Access sorted playlist by ID
                    var getPlaylist = spotifyClient.Playlists.Get(sortedPlaylist.Id);
                    for (int i = 0; i < totalTracks; i++) if (getPlaylist.Result.Tracks.Items[i].Track is FullTrack _track) uriList.Add(_track.Uri);
                    // Playlist request instance
                    PlaylistCreateRequest newPlaylist = new PlaylistCreateRequest(_name);
                    newPlaylist.Public = false; // Set it private
                    String newPlayID = spotifyClient.Playlists.Create(spotifyClient.UserProfile.Current().Result.Id, newPlaylist).Result.Id;
                    // Adding items (tracks) to created playlist
                    PlaylistAddItemsRequest playlistItems = new PlaylistAddItemsRequest(uriList);
                    spotifyClient.Playlists.AddItems(newPlayID, playlistItems);
                    return new KeyValuePair<bool, String>(true, newPlaylist.Name);
                } else return new KeyValuePair<bool, String>(false, SpeechChoices.SpotifyData.botComms[7]);
            } return new KeyValuePair<bool, String>(false, SpeechChoices.SpotifyData.botComms[3]);
        }
    }
}