namespace WNews
{
    public class AppConfig
    {
        private string _FeedUrlVal;
        private string _FeedMUrlVal;
        private string _AdminPWVal;
        private string _GoogleIdVal;
        private string _ConsumerKeyVal;
        private string _ConsumerSecretVal;
        private string _AccessTokenVal;
        private string _AccessTokenSecretVal;
        private string _BSUsername;
        private string _BSPassword;
        private string[] _RSSFeeds;

        private readonly Queue<string> _Posts;
        private readonly int _maxPosts;

        public AppConfig(IConfiguration _config)
        {
            _FeedUrlVal = _config.GetValue<string>("FeedUrl") ?? "";
            _FeedMUrlVal = _config.GetValue<string>("FeedMUrl") ?? "";
            _AdminPWVal = _config.GetValue<string>("AdminPW") ?? "";
            _GoogleIdVal = _config.GetValue<string>("GoogleId") ?? "";
            _ConsumerKeyVal = _config.GetValue<string>("ConsumerKey") ?? "";
            _ConsumerSecretVal = _config.GetValue<string>("ConsumerSecret") ?? "";
            _AccessTokenVal = _config.GetValue<string>("AccessToken") ?? "";
            _AccessTokenSecretVal = _config.GetValue<string>("AccessTokenSecret") ?? "";
            _BSUsername = _config.GetValue<string>("BSUsername") ?? "";
            _BSPassword = _config.GetValue<string>("BSPassword") ?? "";
            _RSSFeeds = _config.GetSection("RssFeeds").Get<string[]>() ?? new string[0];

            //            _RSSFeeds = _config.GetValue<string>("RSSFeeds") ?? "";

            _Posts = new Queue<string>();
            _maxPosts = 30;
        }
        public string FeedUrl
        {
            get => this._FeedUrlVal;
            set => this._FeedUrlVal = value;
        }
        public string FeedMUrl
        {
            get => this._FeedMUrlVal;
            set => this._FeedMUrlVal = value;
        }
        public string AdminPW
        {
            get => this._AdminPWVal;
            set => this._AdminPWVal = value;
        }
        public string GoogleId
        {
            get => this._GoogleIdVal;
            set => this._GoogleIdVal = value;
        }
        public string ConsumerKey
        {
            get => this._ConsumerKeyVal;
            set => this._ConsumerKeyVal = value;
        }
        public string ConsumerSecret
        {
            get => this._ConsumerSecretVal;
            set => this._ConsumerSecretVal = value;
        }
        public string AccessToken
        {
            get => this._AccessTokenVal;
            set => this._AccessTokenVal = value;
        }
        public string AccessTokenSecret
        {
            get => this._AccessTokenSecretVal;
            set => this._AccessTokenSecretVal = value;
        }
        public string BSUsername
        {
            get => this._BSUsername;
            set => this._BSUsername = value;
        }
        public string BSPassword
        {
            get => this._BSPassword;
            set => this._BSPassword = value;
        }
        public string[] RSSFeeds
        {
            get => this._RSSFeeds;
            set => this._RSSFeeds = value;
        }

        public void AddPost(string post)
        {
            if (_Posts.Count >= _maxPosts)
            {
                _Posts.Dequeue();
            }
            _Posts.Enqueue(post);
        }

        public IEnumerable<string> GetPosts()
        {
            return _Posts.ToArray();
        }
    }
}
