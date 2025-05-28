using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Text;

namespace data
{
    public class DbParentSymptom : DbBaseDataObject
    {
        protected string hName;

        public string Name
        {
            get { return hName; }
            set { SetPropertyValue(ref hName, value); }
        }
        

        public DbParentSymptom(Session session) : base(session) { }
    }
}
