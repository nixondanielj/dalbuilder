using DALBuilder.DBModel.Models;
using DALBuilder.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.DBModel.Concrete
{
    class SQLServerQueryService:IQueryService, IDisposable
    {
        SqlConnection connection;

        SQLServerQueryService(IInputService input)
        {
            string cString = input.GetSQLServerConnString();
            connection = new SqlConnection(cString);
        }

        public void PopulateRawTables(Database db)
        {
            var tables = new List<Table>();
            using (var cmd = GetCommand("SELECT * FROM sys.tables"))
            {

            }
        }

        public void PopulateColumns(Table model)
        {
            throw new NotImplementedException();
        }

        public Column GetPrimaryColumn(Models.Table table)
        {
            throw new NotImplementedException();
        }

        private SqlCommand GetCommand(string text)
        {
            return new SqlCommand(text, connection);
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
