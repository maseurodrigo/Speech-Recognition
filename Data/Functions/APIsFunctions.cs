using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Speech_Recognition.Data.Functions
{
    public class APIsFunctions 
    {
        public static String getTimeZoneData(String _WeatherAPIKey, String _city) {
            // Get and return timezones data (given city name)
            RestClient restClient = new RestClient($"https://weatherapi-com.p.rapidapi.com/timezone.json?q={ _city }");
            RestRequest reqst = new RestRequest(Method.GET);
            reqst.AddHeader("x-rapidapi-host", "weatherapi-com.p.rapidapi.com");
            reqst.AddHeader("x-rapidapi-key", _WeatherAPIKey);
            IRestResponse response = restClient.Execute(reqst);
            return response.Content;
        }

        public static String getWeatherData(String _WeatherAPIKey, String _city) {
            // Get and return weather data (given city name)
            RestClient restClient = new RestClient($"https://weatherapi-com.p.rapidapi.com/current.json?q={ _city }");
            RestRequest reqst = new RestRequest(Method.GET);
            reqst.AddHeader("x-rapidapi-host", "weatherapi-com.p.rapidapi.com");
            reqst.AddHeader("x-rapidapi-key", _WeatherAPIKey);
            IRestResponse response = restClient.Execute(reqst);
            return response.Content;
        }

        public static String getCryptoData(String _CoinGeckoKey, String _crypto) {
            // Get and return crypto data by given coin name
            RestClient restClient = new RestClient($"https://coingecko.p.rapidapi.com/coins/{ _crypto }?market_data=true");
            RestRequest reqst = new RestRequest(Method.GET);
            reqst.AddHeader("x-rapidapi-host", "coingecko.p.rapidapi.com");
            reqst.AddHeader("x-rapidapi-key", _CoinGeckoKey);
            IRestResponse dynamJSON = restClient.Execute(reqst);
            return dynamJSON.Content;
        }

        public static String getAPIFootballData(String _RapidAPIKey, int _method, String _idTeam, int? _numGames, int? _idMatch, int? _season, String _teamName) {
            RestClient restClient = new RestClient();
            switch (_method) {
                case 1:
                    // Get and return team members of given team
                    restClient = new RestClient($"https://api-football-v1.p.rapidapi.com/v2/players/squad/{ _idTeam }/{ _season }");
                    break;
                case 2:
                    // Get and return next games of given team
                    restClient = new RestClient($"https://api-football-v1.p.rapidapi.com/v2/fixtures/team/{ _idTeam }/next/{ _numGames }");
                    break;
                case 3:
                    // Get and return next game predict of given team
                    restClient = new RestClient($"https://api-football-v1.p.rapidapi.com/v2/predictions/{ _idMatch }");
                    break;
                case 4:
                    // Get and return data of a team through its name
                    restClient = new RestClient($"https://api-football-v1.p.rapidapi.com/v2/teams/search/{ _teamName }");
                    break;
                case 5:
                    // Get and return last games of given team
                    restClient = new RestClient($"https://api-football-v1.p.rapidapi.com/v2/fixtures/team/{ _idTeam }/last/{ _numGames }");
                    break;
            }
            RestRequest reqst = new RestRequest(Method.GET);
            reqst.AddHeader("x-rapidapi-key", _RapidAPIKey);
            reqst.AddHeader("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
            IRestResponse dynamJSON = restClient.Execute(reqst);
            return dynamJSON.Content;
        }

        public static Task<Dictionary<String, String>> getAPISteamData(String _SteamAPIKey, String _SteamMyID, String _game) {
            return Task.Factory.StartNew(() => {
                WebClient webClient = new WebClient();
                Dictionary<String, String> rFriendsGame = new Dictionary<String, String>();
                // Retrieve all steam friends list
                String friendsURL = "https://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key=";
                dynamic allFriends = JsonConvert.DeserializeObject(webClient.DownloadString($"{ friendsURL }{ _SteamAPIKey }&steamid={ _SteamMyID }&relationship=friend"));
                foreach (var friend in allFriends.friendslist.friends) {
                    String friendURL = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=";
                    object tmpFriend = JsonConvert.DeserializeObject<Object>(webClient.DownloadString($"{ friendURL }{ _SteamAPIKey }&steamids={ friend.steamid }"));
                    JToken userNameToken = ((JObject)tmpFriend)["response"]["players"][0]["personaname"];
                    JToken idGameToken = ((JObject)tmpFriend)["response"]["players"][0]["gameextrainfo"];
                    // Check if JSON response have gameextrainfo param
                    if (idGameToken != null) {
                        // Removes all "-" characters from games name and convert both strings to uppercase
                        if (idGameToken.ToString().Replace("-"," ").ToUpper().Contains(_game.ToUpper())) 
                            rFriendsGame.Add(userNameToken.ToString(), idGameToken.ToString());
                    }
                } return rFriendsGame;
            });
        }

        public static String uploadImgbbFile(String _ImgbbAPI, String _file) {
            // Upload each selected image to imgbb and return URL
            using (Image image = Image.FromFile(_file)) {
                using (MemoryStream m = new MemoryStream()) {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();
                    // Convert byte[] to Base64 String
                    String base64String = Convert.ToBase64String(imageBytes);
                    // POST method call with RestClient
                    String baseURL = "https://api.imgbb.com/1/upload";
                    RestClient restClient = new RestClient(baseURL);
                    RestRequest reqst = new RestRequest(Method.POST);
                    reqst.AddParameter("key", _ImgbbAPI);
                    reqst.AddParameter("expiration", "60");
                    reqst.AddParameter("image", base64String);
                    IRestResponse response = restClient.Post(reqst);
                    var respContent = response.Content;
                    // Convert response into JSON object
                    object tmpJSONImg = JsonConvert.DeserializeObject<Object>(respContent);
                    JToken imgURL = ((JObject)tmpJSONImg)["data"]["medium"]["url"];
                    return imgURL.ToString();
                }
            }
        }
    }
}