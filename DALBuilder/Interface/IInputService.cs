using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.Interface
{
    interface IInputService
    {
        public string Prompt(string message);

        public bool PromptForBool(string message);

        public string GetConnString();
    }
}
