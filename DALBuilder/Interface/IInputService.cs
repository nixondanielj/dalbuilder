using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.Interface
{
    interface IInputService
    {
        string Prompt(string message);

        bool PromptForBool(string message);

        string GetSQLServerConnString();
    }
}
