using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DALBuilder
{
    class TableModel
    {
        public string Name { get; set; }
        public string ModelName { get; set; }
        public ColumnModel PrimaryKey { get; set; }
        public List<ColumnModel> ForeignKeys { get; set; }
        public List<ColumnModel> Columns { get; set; }
        public int object_id { get; set; }
        public bool JoinOnly { get; set; }

        public TableModel()
        {
            ForeignKeys = new List<ColumnModel>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
