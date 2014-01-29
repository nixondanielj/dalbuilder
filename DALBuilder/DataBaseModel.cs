using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DALBuilder
{
    class DataBaseModel
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public List<TableModel> Tables { get; set; }
        public List<AssociationModel> Associations { get; set; }
    }
}