using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DALBuilder
{
    public enum AssociationType
    {
        ManyToMany,
        OneToMany,
        OneToOne
    }
    class AssociationModel
    {
        public TableModel PrimaryTable { get; set; }
        public ColumnModel PrimaryColumn { get; set; }
        public TableModel ForeignTable { get; set; }
        public ColumnModel ForeignColumn { get; set; }
        public AssociationType Type { get; set; }
        public TableModel JoinTable { get; set; }
    }
}
