using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MinecraftBots
{
    class ConsoleIO
    {
        private static List<string> MsgBox = new List<string>();
        private static Thread printer;
        public static void AddMsgSeq(string tex,string title=null)
        {
            if (title == null)
                MsgBox.Add(tex);
            else
                MsgBox.Add("[" + title + "] " + tex);
        }

        public static void StartPrintMsg()
        {
            printer = new Thread(new ThreadStart(() =>
              {
                  while (true)
                  {
                      while (MsgBox.Count > 0)
                      {
                          foreach(string tmp in MsgBox.ToArray())
                          {
                              /*if (tmp.Contains("§"))
                              {
                                  string t2 = tmp;
                                  if (!t2.StartsWith("§"))
                                  {
                                      t2 = "§f" + tmp;
                                  }
                                  WriteLineFormatted(t2);
                              }
                              else
                              {
                                  Console.WriteLine(tmp);
                              }*/
                              Console.WriteLine(tmp);
                              MsgBox.Remove(tmp);
                          }
                      }
                      Thread.Sleep(20);
                  }
              }));
            printer.Start();
        }
        public static void StopPrint()
        {
            if (printer != null)
            {
                printer.Abort();
            }
        }
        public static void WriteLineFormatted(string str, bool acceptnewlines = true)
        {
            if (!String.IsNullOrEmpty(str))
            {
                if (!acceptnewlines) { str = str.Replace('\n', ' '); }
                string[] subs = str.Split(new char[] { '§' });
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 0)
                    {
                        switch (subs[i][0])
                        {
                            case '0': Console.ForegroundColor = ConsoleColor.Gray; break; //Should be Black but Black is non-readable on a black background
                            case '1': Console.ForegroundColor = ConsoleColor.DarkBlue; break;
                            case '2': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                            case '3': Console.ForegroundColor = ConsoleColor.DarkCyan; break;
                            case '4': Console.ForegroundColor = ConsoleColor.DarkRed; break;
                            case '5': Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                            case '6': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                            case '7': Console.ForegroundColor = ConsoleColor.Gray; break;
                            case '8': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                            case '9': Console.ForegroundColor = ConsoleColor.Blue; break;
                            case 'a': Console.ForegroundColor = ConsoleColor.Green; break;
                            case 'b': Console.ForegroundColor = ConsoleColor.Cyan; break;
                            case 'c': Console.ForegroundColor = ConsoleColor.Red; break;
                            case 'd': Console.ForegroundColor = ConsoleColor.Magenta; break;
                            case 'e': Console.ForegroundColor = ConsoleColor.Yellow; break;
                            case 'f': Console.ForegroundColor = ConsoleColor.White; break;
                            case 'r': Console.ForegroundColor = ConsoleColor.Gray; break;
                        }

                        if (subs[i].Length > 1)
                        {
                            Console.Write(subs[i].Substring(1, subs[i].Length - 1));
                        }
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write('\n');
            }
        }
    }
}
