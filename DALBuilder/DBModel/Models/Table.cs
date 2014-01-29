using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.DBModel.Models
{
    class Table : DbObject
    {
        public HashSet<Column> Columns { get; set; }
        public HashSet<Relationship> Relationships { get; set; }
    }
}
