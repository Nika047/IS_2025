using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Text;

namespace data
{
    public class DbSymptom : DbBaseDataObject
    {
        protected string hName;
        protected DbParentSymptom hParentSymptom;
        protected float hSymptomGivenNotDiagnoseP;
        protected string hQuestion;

        public string Name
        {
            get { return hName; }
            set { SetPropertyValue(ref hName, value); }
        }
        
        public DbParentSymptom ParentSymptom
        {
            get { return hParentSymptom; }
            set { SetPropertyValue(ref hParentSymptom, value); }
        }

        public float SymptomGivenNotDiagnoseP
        {
            get { return hSymptomGivenNotDiagnoseP; }
            set { SetPropertyValue(ref hSymptomGivenNotDiagnoseP, value); }
        }
        
        public string Question
        {
            get { return hQuestion; }
            set { SetPropertyValue(ref hQuestion, value); }
        }


        public DbSymptom(Session session) : base(session) { }
    }
}
