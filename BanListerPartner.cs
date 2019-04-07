using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Ban Lister", "Slut", "1.1.0")]
    [Description("Shared Ban System (banlister.com)")]
    internal class BanListerPartner : CovalencePlugin
    {
        public static BanListerPartner Instance;
        private const string BaseGetUrl = "http://api.banlister.com/retrieve.php?i=1&api_key={0}&steamid={1}";
        private const string BasePostUrl = "http://api.banlister.com/insert_rust.php";
        private const string AdminPermission = "banlister.admin";

        private class BanData
        {
            [JsonProperty("steamid")]
            public string SteamID { get; set; }
            [JsonProperty("reason")]
            public string Reason { get; set; }
            [JsonProperty("length")]
            public string Length { get; set; } = "0";
            [JsonProperty("game_id")]
            public string GameID { get; set; } = Instance.covalence.ClientAppId.ToString();
            public class Post : BanData
            {
                [JsonProperty("key")]
                public string Key { get; set; } = Instance.config.Key;
            }
            public class Get : BanData
            {
                [JsonProperty("insert_time")]
                public DateTime TimeStamp { get; set; }
            }
            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public Configuration config;
        public class Configuration
        {
            [JsonProperty("API Key")]
            public string Key { get; set; } = "";
            [JsonProperty("Ban Threshold")]
            public int MaxBans { get; set; } = 5;
            [JsonProperty("Kick player from server if bans exceed the threshold")]
            public bool KickOnMaxBans { get; set; } = true;

            public static Configuration LoadDefaults()
            {
                return new Configuration();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
        }
        protected override void LoadDefaultConfig()
        {
            config = Configuration.LoadDefaults();
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BanReason"] = "Sorry {0}, but this server is protected by Ban Lister, ensuring a safe community!",
                ["AdminMessage"] = "[Ban Lister] <color=#ff4c4c>{0}</color> <color=silver>has <color=lime>{1}</color> bans in the past month!</color>",
                ["BanLogged"] = "Successfully logged ban!",
                ["BanNotLogged"] = "Failed to log ban! {0}"
            }, this);
        }

        private void OnServerInitialized()
        {
            Instance = this;
            permission.RegisterPermission(AdminPermission, this);
            players.Connected.ToList().ForEach(OnUserConnected);
        }
        private void OnUserConnected(IPlayer player)
        {
            webrequest.Enqueue(GetUrl(player.Id), null, (code, raw_response) =>
            {
                if (code == 200 && !string.IsNullOrEmpty(raw_response))
                {
                    raw_response = raw_response.Trim();
                    if (raw_response.StartsWith("[") || raw_response.StartsWith("{"))
                    {
                        BanData.Get[] list = JsonConvert.DeserializeObject<BanData.Get[]>(raw_response);
                        BanData.Get[] pastmonth = list.Where(x => (DateTime.Now - x.TimeStamp).Days <= 31).ToArray();
                        if (list.Length >= config.MaxBans && config.KickOnMaxBans)
                        {
                            player.Kick(GetLang("BanReason", player.Id, player.Name));
                        }
                        if (pastmonth.Any())
                        {
                            foreach (IPlayer admin in players.Connected.Where(x => player.HasPermission(AdminPermission)))
                            {
                                SendMessage(admin, "AdminMessage", player.Name, pastmonth.Length);
                            }
                        }
                    }
                }
                else
                {
                    PrintError("API RESPONDED WITH CODE {0}\n{1}", code, raw_response);
                }
            }, this, Core.Libraries.RequestMethod.GET);
        }
        private void OnUserBanned(string name, string steamid, string address, string reason)
        {
            AddBan(steamid, reason);
        }


        private void AddBan(string steamid, string reason)
        {
            webrequest.Enqueue(BasePostUrl, new BanData.Post
            {
                SteamID = steamid,
                Reason = reason
            }.ToJson(), (code, response) =>
            {
                if (code == 200)
                {
                    Puts(GetLang("BanLogged"));
                }
                else
                {
                    Puts(GetLang("BanNotLogged", response));
                }
            }, this, Core.Libraries.RequestMethod.POST);
        }
        private string GetUrl(string steamid)
        {
            return string.Format(BaseGetUrl, config.Key, steamid);
        }

        private string GetLang(string key, string id = null, params object[] args)
        {
            string message = lang.GetMessage(key, this, id);
            return args?.Length > 0 ? string.Format(message, args) : message;
        }
        private void SendMessage(IPlayer player, string key, params object[] args)
        {
            player.Reply(GetLang(key, player.Id, args));
        }
    }
}
