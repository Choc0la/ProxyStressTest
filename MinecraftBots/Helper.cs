using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MinecraftBots.Protocol.Server;

namespace MinecraftBots
{
    class Helper
    {
        public static string ParseMsg(string json, List<string> links = null)
        {
            return JSONData2String(Json.ParseJson(json), links);
        }
        private static string JSONData2String(Json.JSONData data, List<string> links)
        {
            string extra_result = "";
            switch (data.Type)
            {
                case Json.JSONData.DataType.Object:
                    if (data.Properties.ContainsKey("clickEvent") && links != null)
                    {
                        Json.JSONData clickEvent = data.Properties["clickEvent"];
                        if (clickEvent.Properties.ContainsKey("action")
                            && clickEvent.Properties.ContainsKey("value")
                            && clickEvent.Properties["action"].StringValue == "open_url"
                            && !String.IsNullOrEmpty(clickEvent.Properties["value"].StringValue))
                        {
                            links.Add(clickEvent.Properties["value"].StringValue);
                        }
                    }
                    if (data.Properties.ContainsKey("extra"))
                    {
                        Json.JSONData[] extras = data.Properties["extra"].DataArray.ToArray();
                        foreach (Json.JSONData item in extras)
                            extra_result = extra_result + JSONData2String(item, links);
                    }
                    if (data.Properties.ContainsKey("text"))
                    {
                        return JSONData2String(data.Properties["text"], links) + extra_result;
                    }
                    else return extra_result;

                case Json.JSONData.DataType.Array:
                    string result = "";
                    foreach (Json.JSONData item in data.DataArray)
                    {
                        result += JSONData2String(item, links);
                    }
                    return result;

                case Json.JSONData.DataType.String:
                    return data.StringValue;
            }

            return "";
        }

        public static void WriteLogs(string host,int port,string version,int threads)
        {
            if (Setting.wlogs)
            {
                string logspath = "logs.txt";
                string txt = "";
                string newLog = string.Format("[{0}]\r\nTarget: {1}:{2}\r\nVersion: {3}\r\nThreads: {4}\r\n\r\n", DateTime.Now.ToString("HH 24,MM dd,yyyy"), host, port, version, threads);
                if (File.Exists(logspath))
                    txt = File.ReadAllText(logspath);
                File.WriteAllText(logspath, txt + newLog);
            }
        }
    }
}
