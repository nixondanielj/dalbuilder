using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.Interface.Concrete
{
    class DefaultInputService : IInputService
    {
        public string Prompt(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }

        public bool PromptForBool(string message)
        {
            message = string.Format("{0} (y/n)", message);
            char response = 'a';
            while(response != 'y' && response != 'n')
            {
                Console.WriteLine(message);
                response = char.ToLower(Console.ReadKey().KeyChar);
            }
            return response == 'y';
        }

        public string GetSQLServerConnString()
        {
            string format = "Server={0}; Database={1}; ";
            Console.WriteLine("I will now ask some questions to help me understand how to connect to your database.");
            Console.WriteLine("DISCLAIMER: I currently only work on SQL Server databases");
            Console.WriteLine("Your database either uses windows authentication or a custom username and password.");
            format = GetServerAndDB(format);
            bool getCreds = PromptForBool("Did you set up your database with a custom username and password?");
            if (getCreds)
            {
                format +="User Id={0}; Password={1};";
                format = string.Format(format, Prompt("Enter your User Id"), Prompt("Enter your password"));
            }
            else
            {
                format += "Trusted_Connection=true;";
            }
            return format;
        }

        private string GetServerAndDB(string format)
        {
            string server = Prompt("Enter the database server in the format Server[\\Instance]");
            string database = Prompt("Enter the name of your database");
            return string.Format(format, server, database);
        }
    }
}
