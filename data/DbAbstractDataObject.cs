using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace data
{
    public interface IDatabaseObject
    {

    }

    //public interface ICountedData
    //{
    //    int RowNumber { get; set; }
    //}

    //public interface IDirectory
    //{
    //    string Name { get; set; }
    //    string RefName { get; set; }
    //}

    [NonPersistent]
    public abstract class DbAbstractDataObject : XPBaseObject, IDatabaseObject
    {
        protected Guid hOID;
        protected string hDescription;

        [Key(false)]
        public Guid OID
        {
            get { return hOID; }
            set { SetPropertyValue(ref hOID, value); }
        }

        [Size(SizeAttribute.Unlimited)]
        [AllowNull]
        public string Description
        {
            get { return hDescription; }
            set { SetPropertyValue(ref hDescription, value); }
        }

        [NonPersistent]
        public string SystemTypeName => GetType().Name;

        protected override XPCollection<T> CreateCollection<T>(XPMemberInfo property)
        {
            XPCollection<T> col = base.CreateCollection<T>(property);
            col.DeleteObjectOnRemove = true;
            return col;
        }

        protected bool SetPropertyValue<T>(ref T propertyValueHolder, T newValue, [CallerMemberName] string propertyName = null)
        {
            return SetPropertyValue<T>(propertyName, ref propertyValueHolder, newValue);
        }

        protected bool SetPropertyValue(ref DateTime propertyValueHolder, DateTime newValue, [CallerMemberName] string propertyName = null)
        {
            return SetPropertyValue(propertyName, ref propertyValueHolder, newValue);
        }

        protected bool SetPropertyValue(ref decimal propertyValueHolder, decimal newValue, [CallerMemberName] string propertyName = null)
        {
            return SetPropertyValue(propertyName, ref propertyValueHolder, newValue);
        }

        protected bool SetPropertyValue(ref int propertyValueHolder, int newValue, [CallerMemberName] string propertyName = null)
        {
            return SetPropertyValue(propertyName, ref propertyValueHolder, newValue);
        }

        protected bool SetPropertyValue(object newValue, [CallerMemberName] string propertyName = null)
        {
            return SetPropertyValue(propertyName, newValue);
        }

        protected bool SetPropertyValue<T>(T newValue, [CallerMemberName] string propertyName = null)
        {
            return SetPropertyValue<T>(propertyName, newValue);
        }

        protected bool SetDelayedPropertyValue(object newValue, [CallerMemberName] string propertyName = null)
        {
            return SetDelayedPropertyValue(propertyName, newValue);
        }

        protected bool SetDelayedPropertyValue<T>(T newValue, [CallerMemberName] string propertyName = null)
        {
            return SetDelayedPropertyValue<T>(propertyName, newValue);
        }

        protected new T GetDelayedPropertyValue<T>([CallerMemberName] string propertyName = null)
        {
            return base.GetDelayedPropertyValue<T>(propertyName);
        }

        protected new XPCollection<T> GetCollection<T>([CallerMemberName] string propertyName = null) where T : class
        {
            return base.GetCollection<T>(propertyName);
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();

            OID = Guid.NewGuid();
        }

        public DbAbstractDataObject(Session session)
            : base(session)
        {
        }
    }

    [NonPersistent]
    public class DbBaseDataObject : DbAbstractDataObject
    {
        protected override void OnSaving()
        {
            base.OnSaving();
        }

        protected override void OnDeleting()
        {
            base.OnDeleting();
        }

        public virtual void WriteInRegisters()
        {
        }

        public DbBaseDataObject(Session session)
            : base(session)
        {
        }

    }
}
