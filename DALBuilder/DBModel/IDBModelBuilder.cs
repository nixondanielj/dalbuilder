using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DALBuilder.DBModel.Models;

namespace DALBuilder.DBModel
{
    interface IDBModelBuilder
    {
        Database Build();
    }
}
