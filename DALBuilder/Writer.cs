using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;

namespace DALBuilder
{
    class Writer
    {
        DataBaseModel dbModel;
        List<string> lines;
        int tabs;
        public Writer(DataBaseModel model)
        {
            this.dbModel = model;
            lines = new List<string>();
            tabs = 0;
        }

        public void Write()
        {
            WriteModel();
            WriteDal();
        }

        public void WriteDal()
        {
            AddImports();
            Add("using DAL.Model;");
            AddNamespace("DAL");
            AddMappers();
            AddRepositories();
            AddConstants();
            AddUOW();
            Close();
            File.WriteAllLines(GetPath("DAL"), lines);
            lines.Clear();
        }

        private string GetPath(string ns)
        {
            string path = Program.CallResponse("Enter the path for " + ns + ": ");
            if (string.IsNullOrEmpty(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path += @"\Desktop\" + ns + ".cs";
            }
            return path;
        }

        public void WriteModel()
        {
            AddImports();
            AddNamespace("DAL.Model");
            AddModels();
            Close();
            string s = string.Join("\r\n", lines);
            File.WriteAllLines(GetPath("Model"), lines);
            lines.Clear();
        }

        private void Open()
        {
            Add("{");
            tabs++;
        }

        private void Close()
        {
            tabs--;
            Add("}");
            Add("");
        }

        #region Repositories

        private void AddRepositories()
        {
            foreach (var table in dbModel.Tables.Where(t => !t.JoinOnly))
            {
                var primaryKeyColumn = table.PrimaryKey;
                Add(string.Format("public partial class {0}Repository:Repository<{0}>", table.ModelName));
                Open();
                AddConstructor(table, primaryKeyColumn);
                AddCreateMethods(table);
                AddGetMethods(table);
                AddDeleteMethods(table, primaryKeyColumn);
                AddUpdateMethod(table);
                AddAssociationMethods(table);
                Close();
            }
        }

        private void AddCreateMethods(TableModel table)
        {
            Add(string.Format("public virtual void Create({0} model)", table.ModelName));
            Open();
            Add(string.Format("string text = \"INSERT INTO {0} ({1}) VALUES({2})\";", table.Name,
                string.Join(", ", table.Columns.Where(c => !c.Equals(table.PrimaryKey)).Select(c => string.Format("[{0}]", c.Name))),
                string.Join(", ", table.Columns.Where(c => !c.Equals(table.PrimaryKey)).Select(c => string.Format("@{0}", c.Name)))));
            Add(string.Format("ExecuteParamNonQuery(text, {0});", string.Join(", ", from c in table.Columns.Union(table.ForeignKeys)
                                                                                    where !c.Equals(table.PrimaryKey)
                                                                                    select c.Nullable || c.Type == "string" ? 
                                                                                    string.Format("new SqlParameter(\"@{0}\", model.{0} ?? (object)DBNull.Value)", c.Name)
                                                                                    : string.Format("new SqlParameter(\"@{0}\", model.{0})", c.Name))));
            Close();
        }

        private void AddConstructor(TableModel table, ColumnModel primaryKeyColumn)
        {
            Add(string.Format("internal {0}Repository(SqlConnection connection) : base(connection, new {0}Mapper())", table.ModelName));
            Add("{}");
        }

        private void AddUpdateMethod(TableModel table)
        {
            Add(string.Format("public virtual void Update({0} model)", table.ModelName));
            Open();
            var columns = table.Columns.Where(c=>!c.Equals(table.PrimaryKey));
            Add(string.Format("var sparams = new SqlParameter[{0}];", columns.Count() + 1));
            for(int i = 0; i < columns.Count(); i++)
            {
                if (columns.ElementAt(i).Nullable || columns.ElementAt(i).Type == "string")
                {
                    Add(string.Format("sparams[{0}] = new SqlParameter(\"{1}\", model.{1} ?? (object)DBNull.Value);", i, columns.ElementAt(i).Name));
                }
                else
                {
                    Add(string.Format("sparams[{0}] = new SqlParameter(\"{1}\", model.{1});", i, columns.ElementAt(i).Name));
                }
            }
            Add(string.Format("sparams[{0}] = new SqlParameter(\"@{1}\", model.{1});", columns.Count(), table.PrimaryKey.Name));
            Add(string.Format("ExecuteParamNonQuery(\"UPDATE {0} SET {2} WHERE [{3}] = @{3}\", sparams);", table.Name,
                "",
                BuildSetClause(table), table.PrimaryKey.Name));
            Close();
        }

        private string BuildSetClause(TableModel table)
        {
            var names = from c in table.Columns
                        where !c.Equals(table.PrimaryKey)
                        select string.Format("[{0}] = @{0}", c.Name);
            return string.Join(", ", names);
        }

        private void AddAssociationMethods(TableModel table)
        {
            foreach (var association in dbModel.Associations)
            {
                if (association.PrimaryTable.Equals(table) || association.ForeignTable.Equals(table))
                {
                    if (association.Type.Equals(AssociationType.ManyToMany))
                    {
                        AddManyToManyMethods(table, association);
                    }
                    else
                    {
                        AddOneToManyMethods(association, table);
                    }
                }
            }
        }

        private void AddManyToManyMethods(TableModel table, AssociationModel association)
        {
            //other table in the relationship
            TableModel other = association.PrimaryTable.Equals(table) ? association.ForeignTable : association.PrimaryTable;
            Add(string.Format("public virtual List<{1}> GetBy{0}({0} model)", other.ModelName, table.ModelName));
            Open();
            Add(string.Format("return ExecuteParamQuery(\"SELECT {0} FROM {1} t JOIN {2} j ON t.{3} = j.{4} WHERE j.{5} = @{6}\", new SqlParameter(\"@{6}\", model.{6})).ToList();",
                GetSQLColumns(table, "t"), //0
                table.Name, //1
                association.JoinTable.Name, //2
                table.PrimaryKey.Name, //3
                other.Equals(association.PrimaryTable) ? association.ForeignColumn.Name : association.PrimaryColumn.Name, //4
                new DAL(dbModel.ConnectionString).GetForeignColumnName(other, association.JoinTable), //5
                other.Equals(association.PrimaryTable) ? association.PrimaryTable.PrimaryKey.Name : association.ForeignTable.PrimaryKey.Name)); //6
            Close();
            Add(string.Format("public virtual void AddRelationship({0} primary, {1} foreign)", table.ModelName, other.ModelName));
            Open();
            Add(string.Format("ExecuteParamNonQuery(\"INSERT INTO {0} ({1}) VALUES (@Foreign, @Primary)\", new SqlParameter(\"@Foreign\", foreign.{2}), new SqlParameter(\"@Primary\", primary.{3}));", association.JoinTable.Name, 
                string.Join(", ", "[" + (association.PrimaryTable.Equals(table) ? association.ForeignColumn.Name : association.PrimaryColumn.Name) + "]", 
                "[" + (association.PrimaryTable.Equals(table) ? association.PrimaryColumn.Name : association.ForeignColumn.Name) + "]"), 
                association.ForeignTable.PrimaryKey.Name, association.PrimaryTable.PrimaryKey.Name));
            Close();
            Add(string.Format("public virtual void RemoveRelationship({0} primary, {1} foreign)", table.ModelName, other.ModelName));
            Open();
            Add(string.Format("ExecuteParamNonQuery(\"DELETE FROM {0} j WHERE j.{1} = @Primary AND j.{3} = @Foreign\", new SqlParameter(\"@Primary\", primary.{2}), new SqlParameter(\"@Foreign\", foreign.{4}));",
                association.JoinTable.Name, association.PrimaryColumn.Name, association.PrimaryTable.PrimaryKey.Name, 
                association.ForeignColumn.Name, association.ForeignTable.PrimaryKey.Name));
            Close();
        }

        private void AddOneToManyMethods(AssociationModel association, TableModel table)
        {
            if (association.PrimaryTable.Equals(table))
            {
                AddPrimaryAssociationMethods(association);
            }
            else
            {
                AddForeignAssociationMethods(association);
            }
        }

        private void AddForeignAssociationMethods(AssociationModel association)
        {
            Add(string.Format("public virtual {0} GetBy{1}({1} model)", 
                association.Type.Equals(AssociationType.OneToOne) ? association.ForeignTable.ModelName : string.Format("List<{0}>", association.ForeignTable.ModelName), 
                association.PrimaryTable.ModelName));
            Open();
            Add(string.Format("return ExecuteParamQuery(\"SELECT {0} FROM {1} p WHERE {2} = @{2}\", new SqlParameter(\"@{2}\", model.{3})).{4}();", 
                GetSQLColumns(association.ForeignTable, "p"), association.ForeignTable.Name, association.ForeignColumn.Name, association.PrimaryColumn.Name,
                association.Type.Equals(AssociationType.OneToOne) ? "SingleOrDefault" : "ToList"));
            Close();
            Add(string.Format("public virtual void SetRelationship({0} primary, {1} foreign)", association.PrimaryTable.ModelName, association.ForeignTable.ModelName));
            Open();
            Add(string.Format("ExecuteParamNonQuery(\"UPDATE {0} SET {1} = @{2} WHERE {3} = @{3}\", new SqlParameter(\"@{2}\", primary.{2}),  new SqlParameter(\"@{3}\", foreign.{3}));", 
                association.ForeignTable.Name, association.ForeignColumn.Name, association.PrimaryColumn.Name, association.ForeignTable.PrimaryKey.Name));
            Close();
        }

        private string GetSQLColumns(TableModel table, string alias)
        {
            var columns = new List<string>();
            Action<string> f = s => columns.Add(string.Format("{0}.[{1}]", alias, s));
            foreach (var column in table.Columns.Union(table.ForeignKeys))
            {
                f(column.Name);
            }
            return string.Join(", ", columns);
        }

        private void AddPrimaryAssociationMethods(AssociationModel association)
        {
            Add(string.Format("public virtual {0} GetBy{1}({1} model)", association.PrimaryTable.ModelName, association.ForeignTable.ModelName));
            Open();
            Add(string.Format("return ExecuteParamQuery(\"SELECT {0} FROM {1} p JOIN {2} f ON p.{3} = f.{4} WHERE f.{5} = @{5}\", new SqlParameter(\"@{5}\", model.{5})).SingleOrDefault();",
                GetSQLColumns(association.PrimaryTable, "p"), association.PrimaryTable.Name, association.ForeignTable.Name, association.PrimaryColumn.Name, 
                association.ForeignColumn.Name, association.ForeignTable.PrimaryKey.Name));
            Close();
        }

        private void AddGetMethods(TableModel table)
        {
            foreach (var column in table.Columns)
            {
                Add(string.Format("public virtual List<{0}> GetBy{1}({2} val)", table.ModelName, column.Name, column.Nullable && column.Type != "string" ? column.Type + "?" : column.Type));
                Open();
                Add(string.Format("IEnumerable<{0}> results;", table.ModelName));
                if (column.Nullable)
                {
                    Add("if(val == null)");
                    Open();
                    Add(string.Format("results = ExecuteQuery(\"SELECT {0} FROM {1} t WHERE t.{2} IS NULL\");", GetSQLColumns(table, "t"), table.Name, column.Name));
                    Close();
                    Add("else");
                    Open();
                }
                Add(string.Format("results = ExecuteParamQuery(\"SELECT {0} FROM {1} t WHERE t.{2} = @{2}\", new SqlParameter(\"@{2}\", val));", GetSQLColumns(table, "t"), table.Name, column.Name));
                if (column.Nullable)
                    Close();
                Add("return results.ToList();");
                Close();
            }
            Add(string.Format("public virtual List<{0}> CustomGet(string where)", table.ModelName));
            Open();
            Add(string.Format("return ExecuteQuery(\"SELECT {0} FROM {1} t WHERE \" + where).ToList();", GetSQLColumns(table, "t"), table.Name));
            Close();
            Add(string.Format("public virtual IEnumerable<{0}> GetAll()", table.ModelName));
            Open();
            Add(string.Format("return ExecuteQuery(\"SELECT {0} FROM {1} p\");", GetSQLColumns(table, "p"), table.Name));
            Close();
        }

        private void AddDeleteMethods(TableModel table, ColumnModel primaryKeyColumn)
        {
            Add(string.Format("public virtual void Delete({0} id)", primaryKeyColumn.Type));
            Open();
            Add(string.Format("ExecuteParamNonQuery(\"DELETE FROM {0} WHERE {1} = @{1}\", new SqlParameter(\"@{1}\", id));", table.Name, primaryKeyColumn.Name));
            Close();
            Add(string.Format("public virtual void Delete({0} model)", table.ModelName));
            Open();
            Add(string.Format("Delete(model.{0});", primaryKeyColumn.Name));
            Close();
        }

        #endregion

        private void AddMappers()
        {
            foreach (var table in dbModel.Tables.Where(t => !t.JoinOnly))
            {
                Add("public class {0}Mapper:Mapper<{0}>", table.ModelName);
                Open();
                Add("public override {0} Map(SqlDataReader reader)", table.ModelName);
                Open();
                Add("var model = new {0}();", table.ModelName);
                foreach (var column in table.Columns)
                {
                    Add("model.{0} = To{1}(reader[\"{0}\"]);", column.Name, GetStringType(column));
                }
                Add("return model;");
                Close();
                Close();
            }
        }

        private string GetStringType(ColumnModel column)
        {
            var sb = new StringBuilder();
            if (column.Nullable && !column.Type.Equals("string"))
            {
                sb.Append("Nullable");
            }
            sb.Append(column.Type.First().ToString().ToUpper());
            sb.Append(column.Type.Substring(1));
            return sb.ToString();
        }

        private void AddModels()
        {
            var singularizer = PluralizationService.CreateService(CultureInfo.CurrentCulture);
            foreach (var table in dbModel.Tables.Where(t=>!t.JoinOnly))
            {
                string proposedName = singularizer.Singularize(table.Name);
                string response = Program.CallResponse(string.Format("What is the model name for {0}? (Default: {1})", table.Name, proposedName));
                table.ModelName = string.IsNullOrWhiteSpace(response) ? proposedName : response;
                Add(string.Format("public partial class {0}", table.ModelName));
                Open();
                foreach (var column in table.Columns)
                {
                    Add("public " + column.Type + (column.Nullable && !column.Type.Equals("string") ? "? " : " ") + column.Name + " { get; set; }");
                }
                Close();
            }
        }

        private void AddConstants()
        {
            foreach (var line in ConstantText.Split('\n'))
            {
                Add(line.Replace("\r", string.Empty));
            }
        }

        private void AddNamespace(string ns)
        {
            Add("namespace {0}", ns);
            Open();
        }

        private void AddImports()
        {
            Add("using System;");
            Add("using System.Collections.Generic;");
            Add("using System.Linq;");
            Add("using System.Text;");
            Add("using System.Data.SqlClient;");
            Add("using System.Configuration;");
            Add("using System.Text.RegularExpressions;");
        }

        private void AddUOW()
        {
            Add("public class UnitOfWork : IDisposable");
            Open();
            Add("protected SqlConnection connection;");
            Add("public UnitOfWork()");
            Open();
            Add("connection = new SqlConnection(@\"{0}\");", dbModel.ConnectionString);
            Add("connection.Open();");
            Close();
            Add("public void Dispose()");
            Open();
            Add("connection.Dispose();");
            Close();
            foreach (var table in dbModel.Tables.Where(t => !t.JoinOnly))
            {
                Add("public virtual {0}Repository {0}Repository", table.ModelName);
                Open();
                Add("get");
                Open();
                Add("return new {0}Repository(connection);", table.ModelName);
                Close();
                Close();
            }
            Close();
        }

        private void Add(string s, params object[] parameters)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tabs; i++)
            {
                sb.Append('\t');
            }
            sb.Append(parameters.Any() ? string.Format(s, parameters) : s);
            lines.Add(sb.ToString());
        }

        private const string ConstantText = 
@"
    public abstract partial class Repository<T>
    {
        private SqlConnection db;

        private IMapper<T> _mapper;

        protected Repository(SqlConnection connection, IMapper<T> mapper)
        {
            db = connection;
            _mapper = mapper; // some kind of factory based on typeof(T) to not force the subclass to pass this in?
        }

        partial void LogCmd(SqlCommand command);

        protected void ExecuteParamNonQuery(string command, params SqlParameter[] parameters)
        {
            using (var cmd = BuildCommand(command, false))
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                }
                LogCmd(cmd);
                cmd.ExecuteNonQuery();
            }
        }

        protected IEnumerable<T> ExecuteParamQuery(string command, params SqlParameter[] parameters)
        {
            using (var cmd = BuildCommand(command, false))
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                }
                return ExecuteReader(cmd);
            }
        }

        protected IEnumerable<T> ExecuteSproc(string procName, params SqlParameter[] sqlParameters)
        {
            using (var cmd = BuildCommand(procName, true))
            {
                if (sqlParameters != null)
                {
                    foreach (var parameter in sqlParameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                }
                return ExecuteReader(cmd);
            }
        }

        /// <summary>
        /// Ensure that values are in order of appearance in the query. THIS IS NOT FOR STORED PROCS. USE EXECUTESPROC.
        /// </summary>
        /// <param name=""cmd"">The text of the query</param>
        /// <param name=""parameterValues"">The parameter values in order of appearance</param>
        /// <returns>Query results (T)</returns>
        protected IEnumerable<T> ExecuteParamQuery(string cmd, params object[] parameterValues)
        {
            var parameterNames = GetParameterNames(cmd);
            if (parameterNames.Count == parameterValues.Length)
            {
                using (var command = BuildCommand(cmd, false))
                {
                    for (int i = 0; i < parameterNames.Count; i++)
                    {
                        command.Parameters.Add(new SqlParameter(parameterNames[i], parameterValues[i]));
                    }
                    return ExecuteReader(command);
                }
            }
            else
            {
                throw new Exception(""Ensure that there is a parameter value provided for each parameter in the query"");
            }
        }

        private List<string> GetParameterNames(string cmd)
        {
            var matches = Regex.Matches(cmd, @""@[\w$#]*"");
            List<string> parameters = new List<string>();
            foreach (var match in matches)
            {
                if (!parameters.Contains(match.ToString()))
                {
                    parameters.Add(match.ToString());
                }
            }
            return parameters;
        }

        protected IEnumerable<T> ExecuteQuery(string cmd)
        {
            return ExecuteQuery(cmd, _mapper.Map);
        }

        protected IEnumerable<TD> ExecuteQuery<TD>(string cmd, Func<SqlDataReader, TD> map)
        {
            using (SqlCommand command = BuildCommand(cmd, false))
            {
                return ExecuteReader(command, map);
            }
        }

        protected IEnumerable<T> ExecuteReader(SqlCommand cmd)
        {
            return ExecuteReader(cmd, _mapper.Map);
        }

        protected IEnumerable<TD> ExecuteReader<TD>(SqlCommand cmd, Func<SqlDataReader, TD> map)
        {
            LogCmd(cmd);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return map(reader);
                }
            }
        }

        protected void ExecuteNonQuery(string cmd)
        {
            using (SqlCommand command = BuildCommand(cmd, false))
            {
                LogCmd(command);
                command.ExecuteNonQuery();
            }
        }

        private SqlCommand BuildCommand(string cmd, bool sproc)
        {
            var command = new SqlCommand(cmd, db);
            if (sproc)
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
            }
            return command;
        }
    }



public interface IMapper<T>
{
	T Map(SqlDataReader reader);
}

public abstract class Mapper<T> : IMapper<T>
{
    public abstract T Map(SqlDataReader reader);

    private TD ToType<TD>(object value, Func<string, TD> parse)
    {
        return parse(value.ToString());
    }

    private Nullable<TD> ToNullableType<TD>(object value, Func<string, TD> parse) where TD : struct
    {
        Nullable<TD> result = null;
        if (value != DBNull.Value)
        {
            result = ToType(value, parse);
        }
        return result;
    }

    protected DateTime? ToNullableDateTime(object value)
    {
        return ToNullableType(value, DateTime.Parse);
    }

    protected decimal? ToNullableDecimal(object value)
    {
        return ToNullableType(value, Decimal.Parse);
    }

    protected TimeSpan ToTimeSpan(object value)
    {
        return ToType(value, TimeSpan.Parse);
    }

    protected TimeSpan? ToNullableTimeSpan(object value)
    {
        return ToNullableType(value, TimeSpan.Parse);
    }

    protected DateTime ToDateTime(object value)
    {
        return ToType(value, DateTime.Parse);
    }

    protected int? ToNullableInt(object value)
    {
        return ToNullableType(value, Int32.Parse);
    }

    protected int ToInt(object value)
    {
        return ToType(value, Int32.Parse);
    }

    protected string ToString(object value)
    {
        string result = null;
        if (value != DBNull.Value)
        {
            result = value.ToString();
        }
        return result;
    }

    protected bool ToBool(object value)
    {
        return ToType(value, bool.Parse);
    }

    protected bool? ToNullableBool(object value)
    {
        return ToNullableType(value, bool.Parse);
    }

    protected double ToDouble(object value)
    {
        return ToType(value, double.Parse);
    }

    protected decimal ToDecimal(object value)
    {
        return ToType(value, decimal.Parse);
    }
}";
    }
}
