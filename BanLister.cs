using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Ban Lister", "Slut", "1.0.0")]
    class BanLister : CovalencePlugin
    {
        public static BanLister Instance;
        const string BaseGetUrl = "http://api.banlister.com/retrieve.php?i=1&api_key={0}&steamid={1}";
        const string BasePostUrl = "http://api.banlister.com/insert_rust.php";
        const string AdminPermission = "banlister.admin";

        class BanData
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
                ["AdminMessage"] = "[Ban Lister] <color=#ff4c4c>{0}</color> <color=silver>has <color=lime>{1}</color> bans in the past month!</color>"
            }, this);
        }
        void Loaded()
        {
            Instance = this;
            permission.RegisterPermission(AdminPermission, this);
            foreach (var player in covalence.Players.Connected)
            {
                OnUserConnected(player);
            }
        }
        private void OnUserConnected(IPlayer player)
        {
            webrequest.Enqueue(GetUrl(player.Id), null, (code, response) =>
            {
                if (code == 200)
                {
                    response = response.Trim();
                    if (response != "null")
                    {
                        BanData.Get[] list = JsonConvert.DeserializeObject<BanData.Get[]>(response);
                        BanData.Get[] pastmonth = list.Where(x => (DateTime.Now - x.TimeStamp).Days <= 31).ToArray();
                        if (list.Length >= config.MaxBans && config.KickOnMaxBans)
                        {
                            player.Kick(GetLang("BanReason", player.Name));
                        }
                        if (pastmonth.Any())
                        {
                            foreach (IPlayer admin in covalence.Players.Connected.Where(x => player.HasPermission(AdminPermission)))
                            {
                                SendMessage(admin, "AdminMessage", player.Name, pastmonth.Length);
                            }
                        }
                    }
                }
                else
                {
                    PrintError("API RESPONDED WITH CODE {0}\n{1}", code, response);
                }
            }, this, Core.Libraries.RequestMethod.GET);
        }
        private void OnUserBanned(string name, string steamid, string address, string reason)
        {
            webrequest.Enqueue(BasePostUrl, new BanData.Post
            {
                SteamID = steamid,
                Reason = reason
            }.ToJson(), (code, response) =>
            {
                if (code == 200)
                {
                    Puts("Succesfully logged ban!");
                }
                else
                {
                    Puts("Failed to log ban! {0}", response);
                }
            }, this, Core.Libraries.RequestMethod.POST);
        }
        private string GetUrl(string steamid) => string.Format(BaseGetUrl, config.Key, steamid);
        private string GetLang(string key, params object[] args)
        {
            if (args.Length > 0)
            {
                return string.Format(lang.GetMessage(key, this), args);
            }
            else
            {
                return lang.GetMessage(key, this);
            }
        }
        private void SendMessage(IPlayer player, string key, params object[] args)
        {
            if (args.Length > 0)
            {
                player.Reply(string.Format(lang.GetMessage(key, this, player.Id), args));
            }
            else
            {
                player.Reply(lang.GetMessage(key, this, player.Id));
            }
        }
    }
}
