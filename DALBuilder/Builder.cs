using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Extensions;

namespace DALBuilder
{
    class Builder
    {
        string connectionString;

        DataBaseModel db;

        public Builder(string cstring)
        {
            connectionString = cstring;
            db = new DataBaseModel();
            SetDbName();
        }

        public DataBaseModel Build()
        {
            SetTables();
            SetColumns();
            SetPrimaryKeys();
            SetAssociations();
            return db;
        }

        private void SetPrimaryKeys()
        {
            using (var dal = new DAL(connectionString))
            {
                foreach (var table in db.Tables)
                {
                    string pColumnName = dal.GetPrimaryColumnName(table);
                    table.PrimaryKey = table.Columns.Single(c => c.Name.EqualsIgnoreCase(pColumnName));
                }
            }
        }

        private void SetAssociations()
        {
            List<AssociationModel> associations;
            using (var dal = new DAL(connectionString))
            {
                associations = BuildAssociations(dal);
            }
            SetOneToMany(associations);
            SetManyToMany(associations);
            db.Associations = associations;
        }

        private void SetOneToMany(List<AssociationModel> associations)
        {
            foreach (var association in associations)
            {
                if (association.PrimaryTable.ForeignKeys.Contains(association.ForeignColumn))
                {
                    if (Program.CallResponse(string.Format("Is {0} to {1} one to one (or zero)?", association.PrimaryTable, association.ForeignTable))
                        .StartsWith("y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        association.Type = AssociationType.OneToOne;
                    }
                }
            }
        }

        private void SetManyToMany(List<AssociationModel> associations)
        {
            var possibleJoinTables = db.Tables.Where(t => t.Columns.Count < 3 ||
                (t.Columns.Count < 4 && t.Columns.Any(c => c.Name.EqualsIgnoreCase("id"))));
            foreach (var table in possibleJoinTables)
            {
                var associationsInQuestion = associations.Where(a => a.ForeignTable.Equals(table)).ToList();
                if (associationsInQuestion.Count() > 1)
                {
                    if (Program.CallResponse(string.Format("Is {0} a join table that should not appear in the model? (y/n)", table.Name)).EqualsIgnoreCase("y"))
                    {
                        var newAssociation = new AssociationModel() { Type = AssociationType.ManyToMany };
                        newAssociation.PrimaryTable = associationsInQuestion[0].PrimaryTable.Equals(table) ? associationsInQuestion[0].ForeignTable : associationsInQuestion[0].PrimaryTable;
                        newAssociation.ForeignTable = associationsInQuestion[1].PrimaryTable.Equals(table) ? associationsInQuestion[1].ForeignTable : associationsInQuestion[1].PrimaryTable;
                        table.JoinOnly = true;
                        using (var dal = new DAL(connectionString))
                        {
                            newAssociation.ForeignColumn = table.Columns.Single(c => c.Name.EqualsIgnoreCase(dal.GetForeignColumnName(newAssociation.ForeignTable, table)));
                            newAssociation.PrimaryColumn = table.Columns.Single(c => c.Name.EqualsIgnoreCase(dal.GetForeignColumnName(newAssociation.PrimaryTable, table)));
                        }
                        newAssociation.JoinTable = table;
                        associationsInQuestion.ForEach(aiq => associations.Remove(aiq));
                        associations.Add(newAssociation);
                    }
                }
            }
        }

        private List<AssociationModel> BuildAssociations(DAL dal)
        {
            var associations = new List<AssociationModel>();
            foreach (var rawAssociation in dal.GetRawAssociations())
            {
                var association = new AssociationModel();
                association.PrimaryTable = db.Tables.Single(t => t.Name.Equals(rawAssociation.Item2));
                association.ForeignTable = db.Tables.Single(t => t.Name.Equals(rawAssociation.Item1));
                association.PrimaryColumn = association.PrimaryTable.Columns.Single(c => c.Name.EqualsIgnoreCase(dal.GetPrimaryColumnName(association)));
                association.ForeignColumn = association.ForeignTable.Columns.Single(c => c.Name.EqualsIgnoreCase(dal.GetForeignColumnName(association)));
                association.ForeignTable.ForeignKeys.Add(association.ForeignColumn);
                association.Type = AssociationType.OneToMany;
                associations.Add(association);
            }
            return associations;
        }

        private void SetColumns()
        {
            using (var dal = new DAL(connectionString))
            {
                foreach (var table in db.Tables)
                {
                    table.Columns = dal.GetColumns(table);
                }
            }
        }

        private void SetTables()
        {
            using (var dal = new DAL(connectionString))
            {
                db.Tables = dal.GetTables();
            }
        }

        private void SetDbName()
        {
            var match = Regex.Match(connectionString, "Initial Catalog( *)?=( *)?.*;") ?? Regex.Match(connectionString, "Database( *)?=( *)?.*;");
            db.ConnectionString = connectionString;
            if (match != null)
            {
                var sb = new StringBuilder();
                bool record = false;
                foreach (char c in match.ToString())
                {
                    if (record && c != ';')
                    {
                        sb.Append(c);
                    }
                    if (c.Equals('='))
                    {
                        record = true;
                    }
                }
                db.Name = sb.ToString().Trim();
            }
            else
            {
                throw new Exception("FAILED TO OBTAIN DB NAME FROM CONNECTION STRING");
            }
        }
    }
}
