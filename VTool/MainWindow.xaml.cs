using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;

namespace VTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string cookiesXf_csrf = "xf_csrf";
        const string cookiesxf_session = "xf_session";
        const string cookiesxf_user = "xf_user";

        string cookiesXf_csrf_value = string.Empty;
        string cookiesxf_session_value = string.Empty;
        string cookiesxf_user_value = string.Empty;

        string userName = string.Empty;
        string password = string.Empty;

        string urlThread = string.Empty;
        int from = 1;
        int to = 0;
        string currDir = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            tbUserName.Text = Settings1.Default.Username;
            tbPassword.Text = Settings1.Default.Password;
            currDir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(currDir + "/Download"))
            {
                Directory.CreateDirectory(currDir + "/Download");
            }
        }

        async void Load()
        {
            Dispatcher.Invoke(()=>
            {
                urlThread = tbLink.Text;
                from = int.Parse(tbFrom.Text);
                to = int.Parse(tbTo.Text);
            });

            CookieContainer cookie = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookie;
            cookie.Add(new Uri("https://next.voz.vn"), new Cookie(cookiesXf_csrf, cookiesXf_csrf_value));
            cookie.Add(new Uri("https://next.voz.vn"), new Cookie(cookiesxf_session, cookiesxf_session_value));
            cookie.Add(new Uri("https://next.voz.vn"), new Cookie(cookiesxf_user, cookiesxf_user_value));
            HtmlDocument doc = new HtmlDocument();
            HttpClient client = new HttpClient(handler);

            //https://next.voz.vn/threads/no-sex-test-dang-anh-nextvoz.613/
            string folderName = urlThread.Trim()
                .Replace("https://", "")
                .Split('/')[2];
            string dirOfThread = currDir + "/Download/" + folderName;
            if(!Directory.Exists(dirOfThread))
                Directory.CreateDirectory(dirOfThread);
            FullImageOfThread = new List<string>();
            for (int curPage = from; curPage <= to; curPage++)
            {
                string dirOfPageOfThread = dirOfThread + "/Page" + curPage.ToString();
                if (!Directory.Exists(dirOfPageOfThread))
                    Directory.CreateDirectory(dirOfPageOfThread);
                string curUrl = string.Empty;
                Log("Đang tìm ảnh của trang: " + curPage.ToString());
                if (curPage > 1)
                {
                    curUrl = urlThread + string.Format("/page-{0}", curPage.ToString());
                }
                else
                {
                    curUrl = urlThread;
                }
                var htmlPage = await client.GetStringAsync(curUrl);
                doc.LoadHtml(htmlPage);
                var mainBody = doc.DocumentNode.Descendants("div")
                    .Where(x => x.GetAttributeValue("class", "") == "block-body js-replyNewMessageContainer")
                    .FirstOrDefault();
                List<HtmlNode> lstArticle = mainBody.Descendants("article")
                    .Where(x => x.GetAttributeValue("class", "") == "message-body js-selectToQuote")
                    .ToList();
                ListImageOfPage = new List<string>();
                foreach (var article in lstArticle)
                {
                    FindImageNode(article);
                }

                //var xxx = await client.GetAsync(ListImage[0]);

                Log("Tổng: " + ListImageOfPage.Count + " ảnh");
                int count = 1;
                foreach (var urlImage in ListImageOfPage)
                {
                    Log(string.Format("Đang tải [{0}/{1}] - {2}",count.ToString(), ListImageOfPage.Count.ToString(), urlImage));
                    Download(urlImage, dirOfPageOfThread + "/Pic_" + count.ToString() + ".jpg", urlImage.StartsWith("https://next.voz.vn") ? cookie : null);
                    count++;
                }

                int xxx = int.Parse((((double)curPage / (double)to) * 100).ToString());
                UpdateProgress(xxx);
            }

            Log("Xong cmnr! :sexy:");
            Log("Tieu Long Ha - VozForums");
            Dispatcher.Invoke(()=>
            {
                btnStart.IsEnabled = true;
                MessageBox.Show("Tieu Long Ha Said: Xong r thím :sexy:");
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            await Task.Run(() =>
            {
                Login();
                Load();
            });
        }

        List<string> ListImageOfPage = null;
        List<string> FullImageOfThread = null;
        private void FindImageNode(HtmlNode nodeFind)
        {
            if (nodeFind.Name == "img")
            {
                var vl = nodeFind.GetAttributeValue("src", string.Empty);
                if (!string.IsNullOrEmpty(vl)
                    && (vl.Contains("https://")
                    || vl.Contains("http://")
                    || vl.Contains("www."))
                    && !vl.StartsWith("/")
                    && !vl.StartsWith("data"))
                {
                    var vcl = HtmlEntity.DeEntitize(nodeFind.GetAttributeValue("src", string.Empty));
                    if (!FullImageOfThread.Contains(vcl))
                    {
                        ListImageOfPage.Add(vcl);
                        FullImageOfThread.Add(vcl);
                    }
                }
            }
            if (nodeFind.HasChildNodes)
            {
                var childNodeList = nodeFind.ChildNodes;
                foreach (HtmlNode node in childNodeList)
                {
                    FindImageNode(node);
                }
            }
        }

        void Download(string url, string fileName, CookieContainer cookie = null)
        {
            WebClient cc = cookie == null ? new WebClient() : new WebClientEx(cookie);
            cc.DownloadFile(url, fileName);
        }

        void Login()
        {
            Log("Đang đăng nhập....");
            Dispatcher.Invoke(() =>
            {
                userName = tbUserName.Text;
                password = tbPassword.Text;
            });

            string postData = "&login=" + userName
                + "&password=" + password
                + "&remember=1&_xfRedirect=https://next.voz.vn/";
            CookieContainer cookie = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookie;

            HttpClient client = new HttpClient(handler);

            using (HttpResponseMessage response = client.PostAsync("https://next.voz.vn/login/login", new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")).Result)
            {
                using (HttpResponseMessage responseMessage = client.GetAsync("https://next.voz.vn").Result)
                {
                    using (HttpContent content = responseMessage.Content)
                    {
                        var xxx = content.ReadAsStringAsync().Result;
                        IEnumerable<Cookie> responseCookies = cookie.GetCookies(new Uri("https://next.voz.vn")).Cast<Cookie>();
                        var listName = responseCookies.Select(n => n.Name).ToList();
                        if (listName.Contains(cookiesXf_csrf))
                        {
                            cookiesXf_csrf_value = responseCookies.Where(x => x.Name == cookiesXf_csrf).Select(n => n.Value).FirstOrDefault();
                            cookiesxf_session_value = responseCookies.Where(x => x.Name == cookiesxf_session).Select(n => n.Value).FirstOrDefault();
                            cookiesxf_user_value = responseCookies.Where(x => x.Name == cookiesxf_user).Select(n => n.Value).FirstOrDefault();
                            Log("Đăng nhập thành công");
                            Settings1.Default.Username = userName;
                            Settings1.Default.Password = password;
                            Settings1.Default.Save();
                        }
                        else
                        {
                            // Login that bai
                            Log("Đăng nhập lỗi cmnr :sadvcl:");
                        }
                    }
                }
            }
        }

        void Log(string content)
        {
            Dispatcher.Invoke(()=>
            {
                tbLog.Text = string.Format("\n-{0}", content) + tbLog.Text;
            });
        }
        void UpdateProgress(int value)
        {
            Dispatcher.Invoke(()=>
            {
                prgProgress.Value = value;
            });
        }
    }

    public class WebClientEx : WebClient
    {
        public WebClientEx(CookieContainer container)
        {
            this.container = container;
        }

        public CookieContainer CookieContainer
        {
            get { return container; }
            set { container = value; }
        }

        private CookieContainer container = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest r = base.GetWebRequest(address);
            var request = r as HttpWebRequest;
            if (request != null)
            {
                request.CookieContainer = container;
            }
            return r;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);
            ReadCookies(response);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            ReadCookies(response);
            return response;
        }

        private void ReadCookies(WebResponse r)
        {
            var response = r as HttpWebResponse;
            if (response != null)
            {
                CookieCollection cookies = response.Cookies;
                container.Add(cookies);
            }
        }
    }
}
