using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Xml;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using HtmlAgilityPack;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WNews.Pages
{
    [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any, NoStore = false)]
    public class IndexModel : PageModel
    {
        private IWebHostEnvironment _env;
        private readonly IMemoryCache _MemoryCache;

        public string strHTML = "";
        AppConfig _appconfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IWebHostEnvironment env, IMemoryCache MemoryCache, AppConfig appconfig, IHttpClientFactory httpClientFactory)
        {
            _env = env;
            _appconfig = appconfig;
            _MemoryCache = MemoryCache;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<string> getRSS(string uri)
        {
            string _strXML;

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                _strXML = await response.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {

                Console.Write(ex.Message);
                throw;
            }

            return _strXML;

        }

        private (bool isMedia, string iconClass) IsPlayableMediaContent(SyndicationItem item)
        {
            bool isMedia = false;
            string iconClass = "fa-link";

            foreach (SyndicationElementExtension extension in item.ElementExtensions)
            {
                if (extension.OuterName == "content" && extension.OuterNamespace == "http://search.yahoo.com/mrss/")
                {
                    XElement mediaElement = extension.GetObject<XElement>();
                    string mediaType = mediaElement.Attribute("type")?.Value ?? string.Empty;
                    string mediaUrl = mediaElement.Attribute("url")?.Value ?? string.Empty;
                    if (mediaType.StartsWith("video") || mediaType.StartsWith("audio") || mediaUrl.Contains("youtube.com") || mediaUrl.Contains("youtu.be"))
                    {
                        isMedia = true;
                        iconClass = "fa-videoplay"; // Media icon
                        break;
                    }
                }
            }

            if (!isMedia)
            {
                foreach (SyndicationLink link in item.Links)
                {
                    if (link.RelationshipType == "enclosure" && (link.MediaType.StartsWith("video") || link.MediaType.StartsWith("audio") || link.Uri.ToString().Contains("youtube.com") || link.Uri.ToString().Contains("youtu.be")))
                    {
                        isMedia = true;
                        iconClass = "fa-videoplay"; // Media icon
                        break;
                    }
                }
            }

            if (!isMedia)
            {
                foreach (SyndicationLink link in item.Links)
                {
                    if (link.Uri.ToString().Contains("youtube.com") || link.Uri.ToString().Contains("youtu.be"))
                    {
                        isMedia = true;
                        iconClass = "fa-videoplay"; // Media icon
                        break;
                    }
                }
            }

            return (isMedia, iconClass);
        }
        private string GetCustomField(SyndicationItem item, string fieldName)
        {
            foreach (SyndicationElementExtension extension in item.ElementExtensions)
            {
                XElement element = extension.GetObject<XElement>();
                if (element.Name.LocalName == fieldName)
                {
                    return element.Value;
                }
            }
            return string.Empty;
        }

        private string doAddLink(string strLink, string strTitle, string strIcon)
        {
            StringBuilder htmlLinkBuilder = new StringBuilder();
            htmlLinkBuilder.Append("<div class='linktablerow'>");
            htmlLinkBuilder.Append($"<div onclick='window.open(\"{strLink}\", \"_blank\"); return false;' class='linktablearticlecell'>");
            htmlLinkBuilder.Append($"&nbsp;&nbsp;<i class='icon-fa-{strIcon} iconfap icon-black_fa-{strIcon}' ></i>{strTitle}");
            htmlLinkBuilder.Append("</div></div>");
            return htmlLinkBuilder.ToString();
        }

        private string doLinks()
        {

            StringBuilder htmlLinksBuilder = new StringBuilder();

            htmlLinksBuilder.Append("<div class='linktable'><div class='linktablebody'>");
            htmlLinksBuilder.Append("<div class='linktablerow'><div class='linktableheadercell'><strong>Links</strong></div></div>");

            htmlLinksBuilder.Append(doAddLink("http://watfordfc.com", "Watford FC", "link"));
            htmlLinksBuilder.Append(doAddLink("https://twitter.com/search?f=tweets&q=%23watfordfc", "X Twitter", "link"));
            htmlLinksBuilder.Append(doAddLink("http://www.newsnow.co.uk/h/Sport/Football/Championship/Watford", "NewsNow", "link"));
            htmlLinksBuilder.Append(doAddLink("http://www.facebook.com/watfordfc", "FB Watford", "link"));
            htmlLinksBuilder.Append(doAddLink("http://www.bbc.co.uk/sport/football/teams/watford", "BBC Watford", "link"));
            htmlLinksBuilder.Append(doAddLink("http://www.wfcforums.com/index.php?forums/the-hornets-nest-watford-chat.128/", "WFC&nbsp;Forum", "link"));

            htmlLinksBuilder.Append("<br /><div class='linktablerow'><div class='linktableheadercell'><strong>Podcasts</strong></div></div>");

            htmlLinksBuilder.Append(doAddLink("https://podfollow.com/do-not-scratch-your-eyes/view", "Do Not Scratch Your Eyes", "videoplay"));
            htmlLinksBuilder.Append(doAddLink("https://shows.acast.com/watfordpodcast", "From the Rookery End", "videoplay"));
            htmlLinksBuilder.Append(doAddLink("https://www.youtube.com/c/WD18WatfordFanChannel", "WD18 Watford Fan Channel", "videoplay"));
            htmlLinksBuilder.Append(doAddLink("https://shows.acast.com/64ec9a14fcef6500114566b6", "Hornet Heaven", "videoplay"));
            htmlLinksBuilder.Append(doAddLink("https://shows.acast.com/the-watford-fc-buzz-podcast", "The Watford FC Buzz Podcast", "videoplay"));
            htmlLinksBuilder.Append(doAddLink("https://www.youtube.com/channel/UCrBIFqxzun2zrM3AYVovbmw", "The Voices Of The Vic", "videoplay"));
            htmlLinksBuilder.Append(doAddLink("https://www.youtube.com/@TaylorMadeArmyTV", "Taylor Made Army TV", "videoplay"));

            htmlLinksBuilder.Append("<br /><div class='linktablerow'><div class='linktableheadercell'><strong>History</strong></div></div>");

            htmlLinksBuilder.Append(doAddLink("https://watford.fcdb.info/", "WFCdb Match Database", "link"));
            htmlLinksBuilder.Append(doAddLink("https://www.watfordfcarchive.co.uk/", "Watford FC Archive", "link"));
            htmlLinksBuilder.Append(doAddLink("https://flic.kr/s/aHskH3DQm2", "Vicarage Road history", "link"));

            htmlLinksBuilder.Append("<br /><div class='linktablerow'><div class='linktableheadercell'><strong>Shop</strong></div></div>");
            htmlLinksBuilder.Append(doAddLink("https://www.thehornetsshop.co.uk/", "The Hornets Shop", "link"));
            htmlLinksBuilder.Append(doAddLink("http://bit.ly/watfordfcnewsamz", "Amazon Watford", "link"));

            htmlLinksBuilder.Append("<br /><div class='linktablerow'><div class='linktableheadercell'><strong>Other</strong></div></div>");
            htmlLinksBuilder.Append(doAddLink("https://fixtur.es/en/team/watford-fc", "Fixtures import", "link"));
            htmlLinksBuilder.Append(doAddLink("https://feeds.feedburner.com/WatfordFC", "Watford.Football RSS", "rss"));

            htmlLinksBuilder.Append("</div></div>");

            return htmlLinksBuilder.ToString();

        }

        private async Task doPage()
        {
            string RSSFeedURL = _appconfig.FeedUrl;

            string rssContent = await getRSS(RSSFeedURL);

            using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(rssContent)))
            {
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                StringBuilder htmlBuilder = new StringBuilder();
                string timeNow = DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                htmlBuilder.Append("<div class='linktable'><div class='linktablebody'>");
                htmlBuilder.Append("<div class='linktablerow'>");
                htmlBuilder.Append("<div class='linktabledaycell'></div>");
                htmlBuilder.Append("<div class='linktableheadercell'><strong>Latest&nbsp;News&nbsp;-&nbsp;" + timeNow + "</strong></div>");
                htmlBuilder.Append("</div>");

                StringBuilder htmlImageBuilder = new StringBuilder();
                List<string> imageList = new List<string>();

                string? previousDay = null;

                int imageCount = 0;
                foreach (SyndicationItem item in feed.Items)
                {
                    string strTitle = item.Title.Text;
                    string strLink = item.Links[0].Uri.ToString();
                    string currentDay = item.PublishDate.ToString("ddd");

                    if (imageCount < 6)
                    {
                        string cardimageValue = GetCustomField(item, "cardimage");
                        if (cardimageValue.StartsWith("http"))
                        {
                            imageCount++;
                            imageList.Add(cardimageValue);
                        }
                    }

                    if (previousDay != null && currentDay != previousDay)
                    {
                        htmlBuilder.Append("<div class='linktablerow'><div class='linktabledaycell'></div><div class='linktableseparatorcell'><hr /></div></div>");
                    }

                    htmlBuilder.Append("<div class='linktablerow'><div class='linktabledaycell'>");
                    if (previousDay == null || currentDay != previousDay)
                    {
                        htmlBuilder.Append($"{currentDay}:&nbsp;");
                    }
                    htmlBuilder.Append("</div>");

                    var (isMedia, strIcon) = IsPlayableMediaContent(item);

                    htmlBuilder.Append($"<div onclick='window.open(\"{strLink}\", \"_blank\"); return false;' class='linktablearticlecell'>");
                    htmlBuilder.Append($"<i class='icon-{strIcon} icon-white_{strIcon} iconfap'></i>{strTitle}");
                    htmlBuilder.Append("</div></div>");

                    previousDay = currentDay;

                }

                htmlBuilder.Append("<div class='linktablerow'><div class='linktabledaycell'></div><div class='linktableseparatorcell'><hr /></div></div>");
                htmlBuilder.Append("</div></div>");

                if (imageList.Count > 0)
                {
                    htmlImageBuilder.Append("<div class='divimagetable'>");

                    for (int i = 0; i < imageList.Count; i++)
                    {
                        string imageUrl = imageList[i];
                        if (i < imageList.Count)
                        {
                            htmlImageBuilder.Append($"<img class='divimage' src='https://images.weserv.nl/?url={imageUrl}&amp;w=280&h=140&fit=cover&a=attention'>");   
                        }
                    }
                    htmlImageBuilder.Append("</div><br />");

                    strHTML += htmlImageBuilder.ToString();

                }

                strHTML += "<div class='row'><div class='col-md-6 col-lg-5'>" + htmlBuilder.ToString() + 
                           "</div><div class='col-md-6 col-lg-7'>" + doLinks() + 
                           "</div></div>";

            }
        }


        public async Task OnGetAsync()
        {
            strHTML = string.Empty;
            if (_MemoryCache.TryGetValue("Page", out string? cachedHTML))
            {
                strHTML = cachedHTML ?? string.Empty;
                return;
            }

            await doPage();

            var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

            _MemoryCache.Set("Page", strHTML, options);

        }
    }
}


