using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Net;
using Wesley.Crawler.SimpleCrawler.Models;

namespace Wesley.Crawler.SimpleCrawler
{
    class Program
    {
        static List<string> titles = new List<string>();
        const string fileName = "MyNotes";//日记文件名
        static void Main(string[] args)
        {
            //读取已下载的主题
            ReadDownedTitles();
            CityCrawler();

            //2.抓取酒店
            //HotelCrawler();

            //3.并发抓取示例
            //ConcurrentCrawler();

            Console.ReadKey();
        }

        private static void ReadDownedTitles()
        {
            var c = File.ReadAllLines(fileName);
            foreach (var line in c)
            {
                if (line.StartsWith("[Title]"))
                {
                    titles.Add(line.Replace("[Title]", ""));
                }
            }
        }


        /// <summary>
        /// 抓取豆瓣指定用户
        /// </summary>
        public static void CityCrawler()
        {

            var url = "https://www.douban.com/people/85362438/notes?start=20&type=note&_i=6055391O1Afa0-";
            //"https://www.douban.com/people/85362438/notes?_i=5851926O1Afa0-";//定义爬虫入口URL
            var cityCrawler = new SimpleCrawler();//调用刚才写的爬虫程序
            cityCrawler.OnStart += (s, e) =>
            {
                Console.WriteLine("爬虫开始抓取地址：" + e.Uri.ToString());
            };
            cityCrawler.OnError += (s, e) =>
            {
                Console.WriteLine("爬虫抓取出现错误：" + e.Uri.ToString() + "，异常消息：" + e.Exception.Message);
            };

            cityCrawler.OnCompleted += (s, e) =>
            {
                //使用正则表达式清洗网页源代码中的数据
                var links = Regex.Matches(e.PageSource, @"<a title="".*?</a>", RegexOptions.IgnoreCase);
                foreach (Match match in links)
                {
                    var notes = new Notes
                    {
                        Title = GetTitle(match.Groups[0].Value),
                        Uri = new Uri(GetUrl(match.Groups[0].Value))
                    };
                    if (titles.Contains(notes.Title))
                    {
                        continue;
                    }
                    //获取网页内容
                    var webContent = cityCrawler.HttpRequest(notes.Uri);
                    //获取内容
                    notes.Content = ParseContent(webContent);
                    //Save
                    //1行[Title]标题 1行[Content]内容+空行结尾
                    Console.WriteLine(notes.Title);
                    SaveTxt(notes);
                    System.Threading.Thread.Sleep(6000);
                }
                Console.WriteLine("===============================================");
                Console.WriteLine("爬虫抓取任务完成！合计 ");
                Console.WriteLine("耗时：" + e.Milliseconds + "毫秒");
                Console.WriteLine("线程：" + e.ThreadId);
                Console.WriteLine("地址：" + e.Uri.ToString());
                Console.WriteLine("处理的匹配数:" + links.Count);
            };
            cityCrawler.Start(new Uri(url)).Wait();
        }

        //保存到txt文件
        private static void SaveTxt(Notes notes)
        {
            YFPos.Utils.FileOperHelper.WriteFile(fileName, $"[Title]{notes.Title}");
            YFPos.Utils.FileOperHelper.WriteFile(fileName, $"[Content]{notes.Content}");
        }

        //解析出需要的内容
        private static string ParseContent(string webContent)
        {
            var retStr = string.Empty;
            //使用正则表达式清洗网页源代码中的数据
            var links = Regex.Matches(webContent, @"<div class=""note"">([\w\W\s\S\n\r]*)<div id=""link", RegexOptions.IgnoreCase);
            if (links.Count > 0)
            {
                retStr = links[0].Groups[0].Value.Replace("<div class=\"note\">", "")
                    .Replace("<p data-align=\"\">", "")
                    .Replace("</p>", Environment.NewLine).Replace("<p>", "")
                    .Replace("</div>", "")
                    .Replace("<div id=\"link", "").TrimEnd('\n');//去掉html中的文本标签
            }

            return retStr.TrimEnd();
        }

        private static string GetUrl(string value)
        {
            if (value?.Length > 3)
            {
                var iStart = value.IndexOf("href=\"");
                var iEnd = value.LastIndexOf("\"");

                return value.Substring(iStart + 6, iEnd - iStart - 6);
            }

            return string.Empty;
        }

        private static string GetTitle(string value)
        {
            if (value?.Length > 3)
            {
                var iStart = value.IndexOf("<a title=");
                var iEnd = value.IndexOf("\"", iStart + 1);

                return value.Substring(iStart + 10, iEnd - iStart);
            }

            return string.Empty;
        }



        /// <summary>
        /// 抓取酒店列表
        /// </summary>
        public static void HotelCrawler()
        {
            var hotelUrl = "http://hotels.ctrip.com/hotel/zunyi558";
            var hotelList = new List<Hotel>();
            var hotelCrawler = new SimpleCrawler();
            hotelCrawler.OnStart += (s, e) =>
            {
                Console.WriteLine("爬虫开始抓取地址：" + e.Uri.ToString());
            };
            hotelCrawler.OnError += (s, e) =>
            {
                Console.WriteLine("爬虫抓取出现错误：" + e.Uri.ToString() + "，异常消息：" + e.Exception.Message);
            };
            hotelCrawler.OnCompleted += (s, e) =>
            {
                var links = Regex.Matches(e.PageSource, @"""><a[^>]+href=""*(?<href>/hotel/[^>\s]+)""\s*data-dopost[^>]*><span[^>]+>.*?</span>(?<text>.*?)</a>", RegexOptions.IgnoreCase);
                foreach (Match match in links)
                {
                    var hotel = new Hotel
                    {
                        HotelName = match.Groups["text"].Value,
                        Uri = new Uri("http://hotels.ctrip.com" + match.Groups["href"].Value
                    )
                    };
                    if (!hotelList.Contains(hotel)) hotelList.Add(hotel);//将数据加入到泛型列表
                    Console.WriteLine(hotel.HotelName + "|" + hotel.Uri);//将酒店名称及详细页URL显示到控制台
                }

                Console.WriteLine();
                Console.WriteLine("===============================================");
                Console.WriteLine("爬虫抓取任务完成！合计 " + links.Count + " 个酒店。");
                Console.WriteLine("耗时：" + e.Milliseconds + "毫秒");
                Console.WriteLine("线程：" + e.ThreadId);
                Console.WriteLine("地址：" + e.Uri.ToString());
            };
            hotelCrawler.Start(new Uri(hotelUrl)).Wait();//没被封锁就别使用代理：60.221.50.118:8090
        }


        /// <summary>
        /// 并发抓取示例
        /// </summary>
        public static void ConcurrentCrawler()
        {
            var hotelList = new List<Hotel>() {
                new Hotel { HotelName="遵义浙商酒店", Uri=new Uri("http://hotels.ctrip.com/hotel/4983680.html?isFull=F") },
                new Hotel { HotelName="遵义森林大酒店", Uri=new Uri("http://hotels.ctrip.com/hotel/1665124.html?isFull=F") },
            };
            var hotelCrawler = new SimpleCrawler();
            hotelCrawler.OnStart += (s, e) =>
            {
                Console.WriteLine("爬虫开始抓取地址：" + e.Uri.ToString());
            };
            hotelCrawler.OnError += (s, e) =>
            {
                Console.WriteLine("爬虫抓取出现错误：" + e.Uri.ToString() + "，异常消息：" + e.Exception.Message);
            };
            hotelCrawler.OnCompleted += (s, e) =>
            {
                Console.WriteLine();
                Console.WriteLine("===============================================");
                Console.WriteLine("爬虫抓取任务完成！");
                Console.WriteLine("耗时：" + e.Milliseconds + "毫秒");
                Console.WriteLine("线程：" + e.ThreadId);
                Console.WriteLine("地址：" + e.Uri.ToString());
            };
            Parallel.For(0, 2, (i) =>
            {
                var hotel = hotelList[i];
                hotelCrawler.Start(hotel.Uri);
            });
        }
    }












}


