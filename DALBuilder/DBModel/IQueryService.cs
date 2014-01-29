using DALBuilder.DBModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.DBModel
{
    interface IQueryService
    {
        List<Table> GetRawTables();

        void PopulateColumns(Table model);

        Column GetPrimaryColumn(Table table);
    }
}
