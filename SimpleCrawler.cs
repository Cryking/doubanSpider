using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Wesley.Crawler.SimpleCrawler.Events;

namespace Wesley.Crawler.SimpleCrawler
{
    public class SimpleCrawler : ICrawler
    {
        public event EventHandler<OnStartEventArgs> OnStart;//爬虫启动事件

        public event EventHandler<OnCompletedEventArgs> OnCompleted;//爬虫完成事件

        public event EventHandler<OnErrorEventArgs> OnError;//爬虫出错事件

        public CookieContainer CookiesContainer { get; set; }//定义Cookie容器

        public SimpleCrawler() { }


        /// <summary>
        /// 异步创建爬虫
        /// </summary>
        /// <param name="uri">爬虫URL地址</param>
        /// <param name="proxy">代理服务器</param>
        /// <returns>网页源代码</returns>
        public async Task<string> Start(Uri uri, string proxy = null)
        {
            return await Task.Run(() =>
            {
                var pageSource = string.Empty;
                try
                {
                    if (this.OnStart != null) this.OnStart(this, new OnStartEventArgs(uri));
                    var watch = new Stopwatch();
                    watch.Start();
                    pageSource = HttpRequest(uri, proxy);
                    watch.Stop();
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;//获取当前任务线程ID
                    var milliseconds = watch.ElapsedMilliseconds;//获取请求执行时间
                    if (this.OnCompleted != null) this.OnCompleted(this, new OnCompletedEventArgs(uri, threadId, milliseconds, pageSource));
                }
                catch (Exception ex)
                {
                    if (this.OnError != null) this.OnError(this, new OnErrorEventArgs(uri, ex));
                }
                return pageSource;
            });
        }

        public string HttpRequest(Uri uri, string proxy = null)
        {
            string pageSource;
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Accept = "*/*";
            request.ServicePoint.Expect100Continue = false;//加快载入速度
            request.ServicePoint.UseNagleAlgorithm = false;//禁止Nagle算法加快载入速度
            request.AllowWriteStreamBuffering = false;//禁止缓冲加快载入速度
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");//定义gzip压缩页面支持
            request.ContentType = "application/x-www-form-urlencoded";
            request.AllowAutoRedirect = true;//自动跳转
            request.Headers.Add(HttpRequestHeader.Cookie, @"ll=""118267""; bid=O1Afa0-PTVM; gr_user_id=e15918f7-76a9-4f0e-8024-841449ec5dbd; __gads=ID=5c80ea762ff33c78-22cfb1b231d00095:T=1642942293:RT=1642942293:S=ALNI_MYXofVml59__PW0BmFpIiSEvdx-Cg; push_noty_num=0; push_doumail_num=0; __utmv=30149280.8536; douban-fav-remind=1; __yadk_uid=DaFYz563QMHICf0Id5UaOzsLn1ao6FDz; __utmz=30149280.1645319655.3.2.utmcsr=baidu|utmccn=(organic)|utmcmd=organic; dbcl2=""85362438:K1bX8j7VqN0""; _pk_ref.100001.8cb4=%5B%22%22%2C%22%22%2C1645851441%2C%22https%3A%2F%2Fwww.baidu.com%2Flink%3Furl%3DMHyDoQqKXq0OPf-g6IoY_yA1ivcnQ13s9yTWCwIKVnNwFYnkCum4Re1YpR4s3GX49_yo7UCXsbzJ8B_b2zt6M_%26wd%3D%26eqid%3Dccde91b60002c6320000000662119260%22%5D; _pk_ses.100001.8cb4=*; ap_v=0,6.0; __utma=30149280.7976528.1642942251.1645538204.1645851447.7; __utmt=1; ck=HsPC; _pk_id.100001.8cb4=89e956761a76825a.1642942248.6.1645851929.1645539945.; __utmc=30149280; __utmb=30149280.10.10.1645851447");
            //设置User-Agent，伪装成Google Chrome浏览器
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36";
            request.Timeout = 30000;//定义请求超时时间为10秒
            request.KeepAlive = true;//启用长连接
            request.Method = "GET";//定义请求方式为GET              
            if (proxy != null) request.Proxy = new WebProxy(proxy);//设置代理服务器IP，伪装请求地址
            //request.CookieContainer = this.CookiesContainer;//附加Cookie容器
            request.ServicePoint.ConnectionLimit = int.MaxValue;//定义最大连接数

            using (var response = (HttpWebResponse)request.GetResponse())
            {//获取请求响应

                foreach (Cookie cookie in response.Cookies)
                    this.CookiesContainer.Add(cookie);//将Cookie加入容器，保存登录状态

                if (response.ContentEncoding.ToLower().Contains("gzip"))//解压
                {
                    using (GZipStream stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            pageSource = reader.ReadToEnd();
                        }
                    }
                }
                else if (response.ContentEncoding.ToLower().Contains("deflate"))//解压
                {
                    using (DeflateStream stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            pageSource = reader.ReadToEnd();
                        }

                    }
                }
                else
                {
                    using (Stream stream = response.GetResponseStream())//原始
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {

                            pageSource = reader.ReadToEnd();
                        }
                    }
                }
            }
            request.Abort();

            return pageSource;
        }
    }


}
