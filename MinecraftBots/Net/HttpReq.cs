using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Xml;

namespace MinecraftBots.Net
{
    class HttpReq
    {

        public static void getips_(List<string> list)//89ipAPI Get
        {
            string api = Setting.proxy_url;
            HttpWebResponse wr=null;
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(api);
            try
            {
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:63.0) Gecko/20100101 Firefox/63.0";
                req.CookieContainer = GetCookiesByHeader(Setting.proxy_cookie, ".89ip.cn");
                req.Method = "GET";
                wr = (HttpWebResponse)req.GetResponse();
                StreamReader stream = new StreamReader(wr.GetResponseStream());
                MatchCollection match = Regex.Matches(stream.ReadToEnd(), Setting.proxy_regex);
                foreach (Match ip in match)
                {
                    list.Add(ip.Value);
                }
                ConsoleIO.AddMsgSeq("#2_Get Success:" + match.Count, "Proxy");
            }
            catch (WebException e)
            {
                wr = (HttpWebResponse)e.Response;
                if (Convert.ToInt32(wr.StatusCode) == 521)
                {
                    ConsoleIO.AddMsgSeq("#2_SetCookie\r\n" + e.Message,"Proxy");
                    if (wr.Headers["Set-Cookie"] != null)
                        Setting.proxy_cookie = wr.Headers["Set-Cookie"];
                    Setting.IniWriteValue("Proxy", "cookie", Setting.proxy_cookie);
                }
                getips__(list);
            }catch(Exception e)
            {
                ConsoleIO.AddMsgSeq("#2_Failed\r\n" + e.Message,"Proxy");
            }
        }

        public static void getips__(List<string> list)//proxy.catlr.cn
        {
            string api = "http://proxy.catlr.cn/get_all/";
            HttpWebResponse wr=null;
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(api);
            try
            {
                req.Method = "GET";
                wr = (HttpWebResponse)req.GetResponse();
                StreamReader stream = new StreamReader(wr.GetResponseStream());
                string[] get = subStr(stream.ReadToEnd(), "[", "]").Split(',');
                foreach (string ip in get)
                {
                    string result = subStr(ip, "\"", "\"");
                    list.Add(result);
                }
                Console.WriteLine("#3_Get Success:" + get.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("#3_Failed\r\n" + e.Message);
            }
        }

        internal static bool DoHttpRequest(string url,string data)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                writer.Write(data);
                writer.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string result = reader.ReadToEnd();
                reader.Close();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
                XmlNode state = xmlDoc.SelectSingleNode("state");
                if (state.InnerText == "success")
                    return true;
                return false;
            }
            catch{ return false; }
        }
        private static string subStr(string all, string pre, string suf)
        {
            int print = all.IndexOf(pre) + pre.Length;
            return all.Substring(print, all.Substring(print).IndexOf(suf));
        }

        public static CookieContainer GetCookiesByHeader(string setCookie,string urn=".xxx.com")
        {
            CookieContainer cookieContainer = new CookieContainer();
            string[] cookieItem = setCookie.Split(';');
            Cookie cookie = new Cookie();
            cookie.Domain = urn;
            for (int index = 0; index < cookieItem.Length; index++)
            {
                var info = cookieItem[index];
                //第一个 默认 Cookie Name
                //判断键值对
                if (info.Contains("="))
                {
                    var indexK = info.IndexOf('=');
                    var name = info.Substring(0, indexK).Trim();
                    var val = info.Substring(indexK + 1);
                    if (index == 0)
                    {
                        cookie.Name = name;
                        cookie.Value = val;
                        continue;
                    }
                    if (name.Equals("Domain", StringComparison.OrdinalIgnoreCase))
                    {
                        cookie.Domain = val;
                    }
                    else if (name.Equals("Expires", StringComparison.OrdinalIgnoreCase))
                    {
                        DateTime expires;
                        DateTime.TryParse(val, out expires);
                        cookie.Expires = expires;
                    }
                    else if (name.Equals("Path", StringComparison.OrdinalIgnoreCase))
                    {
                        cookie.Path = val;
                    }
                    else if (name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                    {
                        cookie.Version = Convert.ToInt32(val);
                    }
                }
                else
                {
                    if (info.Trim().Equals("HttpOnly", StringComparison.OrdinalIgnoreCase))
                    {
                        cookie.HttpOnly = true;
                    }
                }
            }
            cookieContainer.Add(cookie);
            return cookieContainer;
        }
    }
}
