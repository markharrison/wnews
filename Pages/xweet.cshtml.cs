using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using OAuth;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WNews.Pages
{

    public class XweetModel : PageModel
    {
        private IWebHostEnvironment _env;
        AppConfig _appconfig;
        public string strResponse = "";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _MemoryCache;

        readonly string hearts = "\U0001F49B\U00002764\uFE0F\U0001F5A4";
        readonly string defautImage = "https://watford.football/images/default.jpg";

        public XweetModel(IWebHostEnvironment env, IMemoryCache MemoryCache, AppConfig appconfig, IHttpClientFactory httpClientFactory)
        {
            _env = env;
            _appconfig = appconfig;
            _httpClientFactory = httpClientFactory;
            _MemoryCache = MemoryCache;
        }

        private string GetCardImage(string link)
        {

            if (_MemoryCache.TryGetValue(link, out string? cardImage))
            {
                return System.Net.WebUtility.UrlDecode(cardImage) ?? defautImage;
            }

            return defautImage;
        }

        private async Task<(string token, string did)> BSkyGetAccessToken(string username, string password)
        {
            using var client = new HttpClient();
            var loginUrl = $"https://bsky.social/xrpc/com.atproto.server.createSession";

            var payload = new Dictionary<string, string>
            {
                { "identifier", username },
                { "password", password }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(loginUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Environment.Exit(-1);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonDocument>(jsonResponse);

            var token = data?.RootElement.GetProperty("accessJwt").GetString() ?? "";

            var did = data?.RootElement.GetProperty("did").GetString() ?? "";

            return (token, did);

        }

        private static string GetImageMimeType(string imageUrl)
        {
            var extension = Path.GetExtension(imageUrl).ToLowerInvariant();

            return extension switch
            {
                ".png" => "image/png",
                ".jpeg" => "image/jpeg",
                ".jpg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "image/jpeg",
            };
        }

        private async Task<string> BSkyUploadImage(string token, string imageUrl)
        {
            string imageMimeType = GetImageMimeType(imageUrl);

            // Download the image
            byte[] imageData;
            using (var httpClient = new HttpClient())
            {
                imageData = await httpClient.GetByteArrayAsync(imageUrl);
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Upload the image
            var uploadUrl = $"https://bsky.social/xrpc/com.atproto.repo.uploadBlob";
            using var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(imageMimeType);

            var response = await client.PostAsync(uploadUrl, imageContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error uploading image: {response.StatusCode}");
                Environment.Exit(-1);
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
            return responseObj?["blob"].ToString() ?? ""; // Retrieve the blob reference

        }

        private List<Dictionary<string, object>> BSkyCreateFacets(string tags, int offset)
        {
            var facets = new List<Dictionary<string, object>>();
            var hashtags = tags.Split(' ');

            int currentIndex = 0;
            foreach (var tag in hashtags)
            {
                if (tag.StartsWith("#"))
                {
                    int startIndex = currentIndex;
                    int endIndex = startIndex + tag.Length;

                    var facet = new Dictionary<string, object>
                    {
                        { "index", new Dictionary<string, int>
                            {
                                { "byteStart", startIndex + offset + 2 },
                                { "byteEnd", endIndex + offset + 2 }
                            }
                        },
                        { "features", new List<Dictionary<string, string>>
                            {
                                new Dictionary<string, string>
                                {
                                    { "$type", "app.bsky.richtext.facet#tag" },
                                    { "tag", tag.Substring(1) }
                                }
                            }
                        }
                    };

                    facets.Add(facet);
                    currentIndex = endIndex + 1; // Update currentIndex to the next position
                }
            }

            return facets;
        }

        private async Task<string> BSkyCreateRecord(string token, string did, string content, string url, string tags, string blobRef)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var postUrl = $"https://bsky.social/xrpc/com.atproto.repo.createRecord";

            var blobRef2 = JsonSerializer.Deserialize<object>(blobRef) ?? "";

            var facets = BSkyCreateFacets(tags, content.Length);

            var payload = new Dictionary<string, object>
                {
                    { "repo", did },
                    { "collection", "app.bsky.feed.post" },
                    { "record", new Dictionary<string, object>
                        {
                            { "$type", "app.bsky.feed.post" },
                            { "text", content + Environment.NewLine + tags + hearts },
                            { "facets", facets
                            },
                            { "createdAt", DateTime.UtcNow.ToString("o") },
                            { "embed", new Dictionary<string, object>
                                {
                                    { "$type", "app.bsky.embed.external" },
                                    { "external", new Dictionary<string, object>
                                        {
                                            { "uri", url },
                                            { "title", content },
                                            { "description", url },
                                            { "thumb", blobRef2 },
                                        }
                                    }
                                }
                            }

                        }
                    }
                };

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

            var contentData = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(postUrl, contentData);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string errRsp = $"BS Error: {response.StatusCode} - {errorContent}";
                Console.WriteLine(errRsp);
                return errRsp;
            }
            return "BSweet OK";

        }

        private async Task<string> BSkyPost(string postContent, string postLink, string postTags)
        {

            // Step 1: Authenticate 
            var (token, did) = await BSkyGetAccessToken(_appconfig.BSUsername, _appconfig.BSPassword);
            if (string.IsNullOrEmpty(token))
            {
                string errRsp = $"BS Failed to authenticate.";
                Console.WriteLine(errRsp);
                return errRsp;
            }

            // Step 2: Upload the image and get the reference
            string postImageUrl = GetCardImage(postLink);
            var blobRef = await BSkyUploadImage(token, postImageUrl);
            if (string.IsNullOrEmpty(blobRef))
            {
                string errRsp = $"BS Failed to upload the image.";
                Console.WriteLine(errRsp);
                return errRsp;
            }

            // Step 3: Post a status with the image
            string errRsp2 = await BSkyCreateRecord(token, did, postContent, postLink, postTags, blobRef);

            return errRsp2;

        }


        async Task<string> PostXweet(string postContent, string postLink, string postTags)
        {
            string tweetUrl = "https://api.twitter.com/2/tweets";

            string tweetText = postContent + " " + postLink + Environment.NewLine + postTags + " " + hearts;

            dynamic payload = new
            {
                text = tweetText
            };

            try
            {
   
                var tweetContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                OAuthRequest oAuthRequestTweet =
                        OAuthRequest.ForProtectedResource("POST", _appconfig.ConsumerKey, _appconfig.ConsumerSecret,
                 _appconfig.AccessToken, _appconfig.AccessTokenSecret);

                oAuthRequestTweet.RequestUrl = tweetUrl;

                string oAuthHeaderValueTweet = oAuthRequestTweet.GetAuthorizationHeader();

                var httpClientTweet = _httpClientFactory.CreateClient();
                httpClientTweet.DefaultRequestHeaders.Add("Authorization", oAuthHeaderValueTweet);
                httpClientTweet.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var tweetResponse = await httpClientTweet.PostAsync(tweetUrl, tweetContent);
                if (tweetResponse.IsSuccessStatusCode)
                {
                    var tweetJsonResponse = await tweetResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    string errRsp = $"X Error posting tweet:{tweetResponse.StatusCode}";
                    Console.WriteLine(errRsp);
                    return errRsp;
                }

            }
            catch (Exception ex)
            {
                string errText = $"X Error {ex.Message}";
                Console.WriteLine(errText);
                return errText;
            }

            return "Xweet OK";
        }


        public async Task OnGetAsync()
        {

            string strStatusX = "OK";
            string strStatusBS = "OK";
            string strTitle = "";
            string strLink = "";
            string textTags = "#WatfordFC";
            bool bXTwitter = true;
            bool bBSky = true;

            var query = Request.Query.ToDictionary(k => k.Key.ToLower(),
                v => v.Value.ToString());

            if (query.ContainsKey("link"))
            {
                strLink = query["link"].Trim();
            }
            if (query.ContainsKey("title"))
            {
                strTitle = query["title"].Trim().Replace("?", "").Replace("&", "");
            }
            if (query.ContainsKey("flags"))
            {
                var strFlags = query["flags"].Trim().ToLower();
                bXTwitter = strFlags.Contains('x');
                bBSky = strFlags.Contains('b');
            }

            if (!string.IsNullOrEmpty(strLink) && !string.IsNullOrEmpty(strTitle))
            {
                if (bXTwitter)
                {
                    strStatusX = await PostXweet(strTitle, strLink, textTags);
                }

                if (bBSky)
                {
                    strStatusBS = await BSkyPost(strTitle, strLink, textTags);
                }
            }

            var response = new
            {
                status = strStatusX + " | " + strStatusBS,
                date = DateTime.UtcNow.ToString("ddd',' d MMM yyyy HH':'mm':'ss"),
                title = strTitle,
                link = strLink
            };

            strResponse = JsonSerializer.Serialize(response);

            _appconfig.AddPost(response.status + " : " + strLink);

            await Task.Run(() => { });

        }
    }
}