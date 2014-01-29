using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace DALBuilder
{
    class DAL : IDisposable
    {
        SqlConnection connection;

        
        public DAL(string cstring)
        {
            connection = new SqlConnection(cstring);
            connection.Open();
        }

        public List<TableModel> GetTables()
        {
            var tables = new List<TableModel>();
            using (var cmd = GetCommand("SELECT * FROM sys.tables"))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(new TableModel()
                        {
                            Name = reader["name"].ToString(),
                            object_id = int.Parse(reader["object_id"].ToString())
                        });
                    }
                }
            }
            return tables;
        }

        public List<Tuple<string, string>> GetRawAssociations()
        {
            var associations = new List<Tuple<string, string>>();
            using (var cmd = GetCommand(GA_COMMAND))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        associations.Add(Tuple.Create(reader["ParentTable"].ToString(), reader["ReferencedTable"].ToString()));
                    }
                }
            }
            return associations;
        }

        internal List<ColumnModel> GetColumns(TableModel table)
        {
            var columns = new List<ColumnModel>();
            using (var cmd = GetCommand("SELECT c.name AS ColumnName, t.name AS Type, c.is_nullable FROM sys.columns c JOIN sys.types t ON c.system_type_id = t.system_type_id WHERE object_id = " + table.object_id))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var column = new ColumnModel()
                        {
                            Name = reader["ColumnName"].ToString(),
                            Type = ConvertType(reader["Type"].ToString()),
                            Nullable = bool.Parse(reader["is_nullable"].ToString())
                        };
                        if(column.Type != "ignore")
                        {
                            columns.Add(column);
                        }
                    }
                }
            }
            return columns;
        }

        internal string GetPrimaryColumnName(TableModel table)
        {
            using (var cmd = GetCommand(string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC ON KCU.CONSTRAINT_NAME = TC.CONSTRAINT_NAME WHERE KCU.TABLE_NAME = '{0}' AND CONSTRAINT_TYPE = 'PRIMARY KEY'", table.Name)))
            {
                var result = cmd.ExecuteScalar();
                if (result != null)
                    return result.ToString();
                else
                    throw new Exception("All tables must have a primary key for this tool to work. Exception in table " + table.Name);
            }
        }

        internal string GetPrimaryColumnName(AssociationModel model)
        {
            using(var cmd = GetCommand(string.Format(GPCN_COMMAND, model.ForeignTable.Name, model.PrimaryTable.Name)))
            {
                return cmd.ExecuteScalar().ToString();
            }
        }

        internal string GetForeignColumnName(AssociationModel model)
        {
            using (var cmd = GetCommand(string.Format(GFCN_COMMAND, model.ForeignTable.Name, model.PrimaryTable.Name)))
            {
                return cmd.ExecuteScalar().ToString();
            }
        }

        internal string GetForeignColumnName(TableModel primary, TableModel foreign)
        {
            using (var cmd = GetCommand(string.Format(GFCN_COMMAND, foreign.Name, primary.Name)))
            {
                return cmd.ExecuteScalar().ToString();
            }
        }

        private SqlCommand GetCommand(string text)
        {
            return new SqlCommand(text, connection);
        }

        private string ConvertType(string input)
        {
            switch (input)
            {
                case "text":
                    return "string";
                case "date":
                    return "DateTime";
                case "time":
                    return "TimeSpan";
                case "tinyint":
                    return "int";
                case "smallint":
                    return "int";
                case "smalldatetime":
                    return "DateTime";
                case "int":
                    return "int";
                case "money":
                    return "decimal";
                case "datetime":
                    return "DateTime";
                case "float":
                    return "float";
                case "bit":
                    return "bool";
                case "decimal":
                    return "decimal";
                case "bigint":
                    return "long";
                case "varchar":
                    return "string";
                case "nvarchar":
                    return "string";
                case "char":
                    return "string";
                case "nchar":
                    return "string";
                case "xml":
                    return "string";
                default:
                    return "ignore";
            }
        }

        public const string GA_COMMAND = @"
SELECT pt.name AS ParentTable, pct.name AS ParentColumn, rt.name AS ReferencedTable, rct.name AS ReferencedColumn
FROM sys.foreign_key_columns fk 
	JOIN sys.tables pt 
		ON fk.parent_object_id = pt.object_id 
	JOIN sys.columns pct
		ON fk.parent_column_id = pct.column_id AND pt.object_id = pct.object_id
	JOIN sys.tables rt 
		ON fk.referenced_object_id = rt.object_id
	JOIN sys.columns rct
		ON rct.column_id = fk.referenced_column_id AND rt.object_id = rct.object_id";
        public const string GFCN_COMMAND = @"
SELECT  KCU1.COLUMN_NAME AS FKColumn
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC 

LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1 
    ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG  
    AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
    AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 

LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2 
    ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG  
    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA 
    AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME 
    AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION 
WHERE KCU1.TABLE_NAME = '{0}' AND KCU2.TABLE_NAME = '{1}'";

        public const string GPCN_COMMAND = @"
SELECT  KCU2.COLUMN_NAME AS PKColumn
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC 

LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1 
    ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG  
    AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
    AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 

LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2 
    ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG  
    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA 
    AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME 
    AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION 
WHERE KCU1.TABLE_NAME = '{0}' AND KCU2.TABLE_NAME = '{1}'
";

        public void Dispose()
        {
            connection.Dispose();
        }

        
    }
}
