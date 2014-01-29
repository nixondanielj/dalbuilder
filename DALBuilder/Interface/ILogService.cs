using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.Interface
{
    interface ILogService
    {
        void LogExchange(string call, string response);
        string GetPriorResponse(string call);
    }
}
