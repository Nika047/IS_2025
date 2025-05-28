using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace data
{
    public class DbDiagnoseSymptoms : DbBaseDataObject
    {
        protected DbDiagnose hDiagnose;
        protected DbSymptom hSymptom;
        protected float hSymptomGivenDiagnoseP;
        //protected float hSymptomGivenNotDiagnoseP;

        [Association]
        public DbDiagnose Diagnose
        {
            get { return hDiagnose; }
            set { SetPropertyValue(ref hDiagnose, value); }
        }
        
        public DbSymptom Symptom
        {
            get { return hSymptom; }
            set { SetPropertyValue(ref hSymptom, value); }
        }
        
        public float SymptomGivenDiagnoseP
        {
            get { return hSymptomGivenDiagnoseP; }
            set { SetPropertyValue(ref hSymptomGivenDiagnoseP, value); }
        }
        
        //public float SymptomGivenNotDiagnoseP
        //{
        //    get { return hSymptomGivenNotDiagnoseP; }
        //    set { SetPropertyValue(ref hSymptomGivenNotDiagnoseP, value); }
        //}


        public DbDiagnoseSymptoms(Session session) : base(session) { }
    }
}
