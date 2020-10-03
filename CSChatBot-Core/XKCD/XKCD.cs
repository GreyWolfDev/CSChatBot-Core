﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DB;
using DB.Models;
using HtmlAgilityPack;
using ModuleFramework;
using Newtonsoft.Json;
using Telegram.Bot;
#pragma warning disable IDE0044 // Add readonly modifier
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
namespace XKCD
{
    /// <summary>
    /// A sample module for CSChatBot
    /// </summary>
    [ModuleFramework.Module(Author = "parabola949", Name = "XKCD", Version = "1.0")]

    public class XKCD
    {
        internal static Random R = new Random();
        private TelegramBotClient _bot;
        public XKCD(Instance db, Setting settings, TelegramBotClient bot)
        {
            _bot = bot;
        }

        [ChatCommand(Triggers = new[] { "xkcd" }, HelpText = "Gets a random xkcd, or searches", Parameters = new[] { "none - random", "<search query>", "'new' - latest" })]
        public static CommandResponse GetXkcd(CommandEventArgs args)
        { 
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons
            XkcdPost chosen;
            var current = JsonConvert.DeserializeObject<XkcdPost>(
                    new WebClient().DownloadString("https://xkcd.com/info.0.json"));
            if (String.IsNullOrEmpty(args.Parameters))
            {
                //get the current XKCD

                chosen = JsonConvert.DeserializeObject<XkcdPost>(
                    new WebClient().DownloadString($"https://xkcd.com/{R.Next(current.num)}/info.0.json"));


            }
            else
            {
                if (int.TryParse(args.Parameters, out var num))
                {
                    chosen =
                        JsonConvert.DeserializeObject<XkcdPost>(
                            new WebClient().DownloadString($"https://xkcd.com/{Math.Min(current.num, num)}/info.0.json"));
                }
                else
                {
                    if (String.Equals(args.Parameters, "new", StringComparison.InvariantCultureIgnoreCase))
                    {
                        chosen = current;
                    }
                    else
                    {
                        var web = new HtmlWeb();


                        //search
                        var url = $"https://www.google.com/search?q={args.Parameters} inurl:https://xkcd.com";
                        var doc = web.Load(url);

                        //var results = doc.DocumentNode.SelectNodes("//div[@class='g']");
                        //var top = results[0];
                        var page = doc.DocumentNode.SelectSingleNode("//div[@class='g']/div/h3/a").Attributes["href"].Value;


                        //var page = new WebClient { UseDefaultCredentials = true }.DownloadString(url);

                        //page = page.Substring(page.IndexOf("<div id=\"search\">"));
                        //page = page.Substring(page.IndexOf("q=") + 2);
                        //page = page.Substring(0, page.IndexOf("/&amp")).Replace("https://xkcd.com/", "");

                        chosen =
                        JsonConvert.DeserializeObject<XkcdPost>(
                            new WebClient().DownloadString($"{page}/info.0.json"));

                    }
                }
            }
            return new CommandResponse($"{chosen.title}\n{chosen.alt}\n{chosen.img}")
            {
                ImageCaption = chosen.alt,
                ImageUrl = chosen.img,
                ImageDescription = chosen.alt,
                ImageTitle = chosen.title
            };
        }
    }


    public class XkcdPost
    {
        public string month { get; set; }
        public int num { get; set; }
        public string link { get; set; }
        public string year { get; set; }
        public string news { get; set; }
        public string safe_title { get; set; }
        public string transcript { get; set; }
        public string alt { get; set; }
        public string img { get; set; }
        public string title { get; set; }
        public string day { get; set; }
    }

}
