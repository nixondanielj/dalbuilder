using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace DALBuilder
{
    class Program
    {
        public static List<string> Log = new List<string>();

        private static string Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\DALBuilder\";

        private static string FileName = "settings.config";

        private static int LogIndex = 0;

        public static bool UseMemory = false;

        static void Main(string[] args)
        {
            LoadLog();
            if(Log.Count > 0)
            {
                UseMemory = CallResponse("Use saved Settings?").StartsWith("y");
            }
            var dbModel = new Builder(CallResponse("Enter the Connection String: ")).Build();
            new Writer(dbModel).Write();
            SaveLog();
        }

        public static string CallResponse(string message)
        {
            Console.WriteLine(message);
            string input;
            if (UseMemory)
            {
                input = Log[LogIndex];
                LogIndex++;
            }
            else
            {
                input = Console.ReadLine();
                Log.Add(input);
            }
            return input;
        }

        public static void SaveLog()
        {
            CheckFile();
            File.WriteAllLines(Path + FileName, Log);
        }

        public static void LoadLog()
        {
            CheckFile();
            Log = File.ReadAllLines(Path + FileName).ToList();
        }

        private static void CheckFile()
        {
            if (!File.Exists(Path + FileName))
            {
                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }
                File.Create(Path + FileName);
            }
            Thread.Sleep(1000);
        }
    }
}
