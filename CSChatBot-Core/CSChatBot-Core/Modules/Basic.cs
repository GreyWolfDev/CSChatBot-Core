using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DB;
using DB.Extensions;
using DB.Models;
using HtmlAgilityPack;
//using HtmlAgilityPack;
using ModuleFramework;
using Telegram.Bot;
using Menu = ModuleFramework.Menu;

namespace CSChatBot.Modules
{
    [ModuleFramework.Module(Author = "parabola949", Name = "Basic", Version = "1.0")]
    class Basic
    {
        public Basic(Instance instance, Setting setting, TelegramBotClient bot)
        {

        }

        [ChatCommand(Triggers = new[] { "google", "g", "lmgtfy" }, DontSearchInline = true, Parameters = new[] { "<your search>" })]
        public static CommandResponse Google(CommandEventArgs args)
        {
            if (String.IsNullOrEmpty(args.Parameters)) return new CommandResponse("");
            try
            {
                var searchResults = new List<(string Text, string Url)>();
                var web = new HtmlWeb
                {
                    UserAgent = @"Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1667.0 Safari/537.36"
                };

                //search
                var url = $"https://www.google.com/search?q={args.Parameters}";
                var doc = web.Load(url);

                string quickanswer = "";
                var answerbox = doc.DocumentNode.SelectSingleNode("//div[@role='heading']/div[not(descendant::a)]");
                if (answerbox != null)
                {
                    quickanswer = "Quick answer: ";
                    if (answerbox.ChildNodes.Any(x => x.Name == "div"))
                    {
                        var divs = answerbox.SelectNodes("div");
                        quickanswer += divs[0].InnerText;
                        for (int i = 1; i < divs.Count; i++)
                            quickanswer += "\n" + divs[i].InnerText;
                    }
                    else quickanswer += answerbox.InnerText;
                    quickanswer += "\n\n";
                }

                var results = doc.DocumentNode.SelectNodes("//div[@class='rc']/div/a[descendant::h3]");

                var inline = args.Message == null;
                if (!inline)
                {

                    foreach (var result in results)
                    {
                        var link = result.Attributes["href"].Value;
                        var text = result.SelectSingleNode("h3").InnerText;
                        searchResults.Add((text, link));
                    }

                    var menu = new Menu();
                    foreach (var (Text, Url) in searchResults)
                        menu.Buttons.Add(new InlineButton(Text, url: Url));
                    var answer = quickanswer + "Here are the results for " + args.Parameters;
                    return new CommandResponse(answer, menu: menu);
                }
                var response = $"I googled that for you:\n{args.Parameters}\n\n{quickanswer}{results[0].SelectSingleNode("h3").InnerText}\n{results[0].Attributes["href"].Value}";
                return new CommandResponse(response);

            }
            catch (Exception e)
            {
                return new CommandResponse("Sorry, I wasn't able to pull the results.  Send this to Para: " + e.Message);
            }
        }
    }
}
