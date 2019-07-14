using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;
using Starksoft.Net.Proxy;

namespace MinecraftBots
{
    public class Setting
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        public static void IniWriteValue(string Section, string Key,string value)
        {
            WritePrivateProfileString(Section, Key, value, InIpath);
        }
        private static string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, InIpath);
            return temp.ToString();
        }
        public static void LoadDefault()
        {
            if (File.Exists(InIpath))
            {
                name = IniReadValue("Control","name");
                threads = int.Parse(IniReadValue("Control", "maxbots"));
                cooldown = int.Parse(IniReadValue("Control", "cooldown"));
                protocol = int.Parse(IniReadValue("Control", "protocol"));
                t_clear= int.Parse(IniReadValue("Control", "tclear"));
                proxy_url = IniReadValue("Proxy", "url");
                proxy_cookie = IniReadValue("Proxy", "cookie");
                proxy_regex = IniReadValue("Proxy", "regex");
                sendmotd=strToBool(IniReadValue("Bot", "motdSend"));
                t_tabcomplete = int.Parse(IniReadValue("Bot", "tabComplete"));
                t_rejoin = int.Parse(IniReadValue("Bot", "reJoin"));
                sendsetting = strToBool(IniReadValue("Bot", "ClientSettingSend"));
                wlogs=strToBool(IniReadValue("Info", "Wlogs"));
                chatlist = IniReadValue("Info", "chats");
            }
            else
            {
                File.WriteAllText(InIpath, Properties.Resources.DefaultConfig);
                LoadDefault();
            }
        }
        public static string InIpath = Environment.CurrentDirectory + "\\config.ini";

        public static string name { get; private set; }
        public static int threads { get; private set; }
        public static int cooldown { get; private set; }
        public static int protocol { get; private set; }
        public static bool sendmotd { get; private set; }
        public static bool sendsetting { get; private set; }
        public static int t_clear { get; private set; }
        public static int t_tabcomplete { get; private set; }
        public static string chatlist = "chatlist.txt";
        public static bool wlogs = true;
        internal static string tok= "0000000000000000";

        public static string proxy_url { get; private set; }

        public static string proxy_cookie { get; set; }
        public static string proxy_regex { get; private set; }

        public static int t_rejoin { get; private set; }
        private static bool strToBool(string format)
        {
            if (format.Trim().ToLower() == "true")
                return true;
            else
                return false;
        }
    }
}
