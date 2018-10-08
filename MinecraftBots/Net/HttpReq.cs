using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace MinecraftBots.Net
{
    class HttpReq
    {
        public static void getips(ArrayList list)//66ipAPI Get
        {
            string api = "http://www.66ip.cn/mo.php?sxb=&tqsl=2000&port=&export=&ktip=&sxa=&submit=%CC%E1++%C8%A1&textarea=";
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(api);
                req.Method = "GET";
                using (WebResponse wr = req.GetResponse())
                {
                    StreamReader stream = new StreamReader(wr.GetResponseStream());
                    string[] get = subStr(stream.ReadToEnd(), "<br />", "</div>").Replace("\t", "").Replace(" ", "").Replace("<br/>\r\n", "@").Split('@');
                    foreach (string ip in get)
                    {
                        if (!list.Contains(ip))
                        {
                            list.Add(ip);
                        }
                    }
                    Console.WriteLine("#1_Get Success:" + get.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("#1_Failed\r\n" + e.ToString());
            }
        }
        public static void getips_(ArrayList list)//89ipAPI Get
        {
            string api = "http://www.89ip.cn/tqdl.html?api=1&num=2000&port=&address=&isp=";
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(api);
                req.Method = "GET";
                using (WebResponse wr = req.GetResponse())
                {
                    StreamReader stream = new StreamReader(wr.GetResponseStream());
                    string[] get = subStr(stream.ReadToEnd(), "</script>", "<br>高效").Replace("\t", "").Replace(" ", "").Replace("<br>", "@").Split('@');
                    foreach (string ip in get)
                    {
                        if (!list.Contains(ip))
                        {
                            list.Add(ip);
                        }
                    }
                    Console.WriteLine("#1_Get Success:" + get.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("#1_Failed\r\n" + e.ToString());
            }
        }

        public static void getips__(ArrayList list)//proxy.catlr.cn
        {
            string api = "http://proxy.catlr.cn/get_all/";
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(api);
                req.Method = "GET";
                using (WebResponse wr = req.GetResponse())
                {
                    StreamReader stream = new StreamReader(wr.GetResponseStream());
                    string[] get = subStr(stream.ReadToEnd(), "[", "]").Split(',');
                    foreach (string ip in get)
                    {
                        string result = subStr(ip, "\"", "\"");
                        if (!list.Contains(result))
                        {
                            list.Add(result);
                        }
                    }
                    Console.WriteLine("#1_Get Success:" + get.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("#1_Failed\r\n" + e.ToString());
            }
        }
        private static string subStr(string all, string pre, string suf)
        {
            int print = all.IndexOf(pre) + pre.Length;
            return all.Substring(print, all.Substring(print).IndexOf(suf));
        }
    }
}
