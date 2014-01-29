using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace DALBuilder.Interface.Concrete
{
    class DefaultLogService:ILogService
    {
        private const string EXTENSION = ".log";
        private const string LOG_FOLDER = "logs";
        private const string LOG_DELIM = "__~~__";

        DefaultLogService()
        {
            Log = new Dictionary<string, string>();
            FilePath = Path.Combine(Directory.GetCurrentDirectory(), LOG_FOLDER);
            FileName = DateTime.Now.Ticks + EXTENSION;
            FSInit();
        }

        private void FSInit()
        {
            if (Directory.Exists(FilePath))
            {
                try
                {
                    LoadLastLog();
                }
                catch (Exception e)
                {
                    Log.Clear();
                }
            }
            else
            {
                Directory.CreateDirectory(FilePath);
            }
            File.Create(FullPath);
        }

        private string FileName { get; set; }
        private string FilePath { get; set; }
        private string FullPath
        {
            get
            {
                return Path.Combine(FilePath, FileName);
            }
        }

        private Dictionary<string, string> Log { get; set; }

        public void LogExchange(string call, string response)
        {
            Log[call] = response;
        }

        public string GetPriorResponse(string call)
        {
            string response = null;
            Log.TryGetValue(call, out response);
            return response;
        }

        private void LoadLastLog()
        {
            string file = (from f in Directory.EnumerateFiles(FilePath)
                           where f.EndsWith(".log")
                           select f).LastOrDefault();
            if (file != null)
            {
                foreach(var line in File.ReadAllLines(Path.Combine(FilePath, file)))
                {
                    // splits line into 2 entries separated by the delim, removing empty entries
                    string[] parsedLine = line.Split(new string[] { LOG_DELIM }, 2, StringSplitOptions.RemoveEmptyEntries);
                    LogExchange(parsedLine[0], parsedLine[1]);
                }
            }
        }

        private void SaveLog()
        {
            List<string> lines = new List<string>();
            foreach (var pair in Log)
            {
                string line = string.Format("{0}{1}{2}", pair.Key, LOG_DELIM, pair.Value);
                lines.Add(line);
            }
            File.WriteAllLines(FullPath, lines);
        }
    }
}
