using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Text;

namespace data
{
    public class DbDiagnose : DbBaseDataObject
    {
        protected string hCountry;
        protected float hPriorP;
        protected float hTouristsCount;

        public string Country
        {
            get { return hCountry; }
            set { SetPropertyValue(ref hCountry, value); }
        }
        
        public float PriorP
        {
            get { return hPriorP; }
            set { SetPropertyValue(ref hPriorP, value); }
        }
        
        public float TouristsCount
        {
            get { return hTouristsCount; }
            set { SetPropertyValue(ref hTouristsCount, value); }
        }

        [Association(typeof(DbDiagnoseSymptoms)), Aggregated]
        public XPCollection<DbDiagnoseSymptoms> DiagnoseSymptoms
        {
            get { return GetCollection<DbDiagnoseSymptoms>(); }
        }


        public DbDiagnose(Session session) : base(session) { }
    }
}
