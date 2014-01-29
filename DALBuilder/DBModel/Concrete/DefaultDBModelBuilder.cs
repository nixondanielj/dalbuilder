using DALBuilder.DBModel.Models;
using DALBuilder.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALBuilder.DBModel.Concrete
{
    class DefaultDBModelBuilder:IDBModelBuilder
    {
        private IInputService InputService { get; set; }

        public DefaultDBModelBuilder(IInputService iservice)
        {
            this.InputService = iservice;
        }
        public DatabaseModel Build()
        {
            throw new NotImplementedException();
        }
    }
}
