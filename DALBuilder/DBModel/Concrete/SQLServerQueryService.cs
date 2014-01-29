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
            using (var cmd = GetCommand("SELECT * FROM sys.tables"))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var table = new Table();
                        table.Name = reader["name"].ToString();
                        table.ObjectId = Convert.ToInt32(reader["object_id"]);
                        db.Tables.Add(table);
                    }
                }
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
