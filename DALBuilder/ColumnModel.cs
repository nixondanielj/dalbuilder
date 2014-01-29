using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DALBuilder
{
    class ColumnModel
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public TableModel Reference { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
