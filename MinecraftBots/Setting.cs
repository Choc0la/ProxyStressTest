using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;

namespace MinecraftBots
{
    public class Setting
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
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
                threads = int.Parse(IniReadValue("Control", "threads"));
                cooldown = int.Parse(IniReadValue("Control", "cooldown"));
                protocol = int.Parse(IniReadValue("Control", "protocol"));
                if(IniReadValue("Bot", "motdSend") == "true")
                {
                    MotdSend = true;
                }
                else
                {
                    MotdSend = false;
                }
                if(IniReadValue("Bot", "tabComplete") == "true")
                {
                    TabComplete = true;
                }
                else
                {
                    TabComplete = false;
                }
                if (IniReadValue("Bot", "reJoin") == "true")
                {
                    ReJoin = true;
                }
                else
                {
                    ReJoin = false;
                }
                chatlist = IniReadValue("Info", "chats");
            }
            else
            {
                File.WriteAllText(InIpath, Properties.Resources.DefaultConfig);
            }
        }
        public static string InIpath = Environment.CurrentDirectory + "\\config.ini";

        public static string name { get; private set; }
        public static int threads { get; private set; }
        public static int cooldown { get; private set; }
        public static int protocol { get; private set; }
        public static bool MotdSend { get; private set; }
        public static bool TabComplete { get; private set; }
        public static string chatlist = "chatlist.txt";
        internal static string tok="17273-10492";

        public static bool ReJoin { get; private set; }
    }
}
