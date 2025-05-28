using DevExpress.Xpo;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace data
{
    public static class DatabaseDefault
    {
        public static string HidSeparator => "|";
    }

    public interface IHierarchicalData
    {
        string HID { get; set; }
        IHierarchicalData Parent { get; set; }
        Guid ParentOID { get; }
        int Level { get; set; }
        int ID { get; set; }
        IEnumerable<IHierarchicalData> GetPath();
        IEnumerable<IHierarchicalData> SubTree();
        bool IsChildOf(IHierarchicalData data);

        string GenerateHID();
    }

    public interface ISerialData
    {
        int RegNumber { get; set; }
        string RegCode { get; set; }

        string ObjectCode { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SequenceTemplateAttribute : Attribute
    {
        protected Guid hId;
        protected string hCodeTemplate;
        protected string hRequestTemplate;

        public string CodeTemplate
        {
            get { return hCodeTemplate; }
        }

        public string RequestTemplate
        {
            get { return hRequestTemplate; }
        }

        public SequenceTemplateAttribute(string codeTemplate, string requestTemplate)
        {
            this.hCodeTemplate = codeTemplate;
            this.hRequestTemplate = requestTemplate;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IconAttribute : Attribute
    {
        public string Icon { get; set; }

        public IconAttribute(string iconName)
        {
            Icon = iconName;
        }
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DataObjectBehaviorAttribute : Attribute
    {
        private Type hDataType;
        //private string hDisplayName;

        public Type DataType
        {
            get { return hDataType; }
        }
        
        //public string DisplayName
        //{
        //    get { return hDisplayName; }
        //}

        public DataObjectBehaviorAttribute(Type dataType)
        {
            this.hDataType = dataType;
        }

        protected static List<MetadataItem> cache = null;

        protected static void CreateCache()
        {
            cache = new List<MetadataItem>();
            cache.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.DefinedTypes)
                .SelectMany(r => r.GetCustomAttributes(typeof(DataObjectBehaviorAttribute), false).OfType<DataObjectBehaviorAttribute>(), (ci, att) =>
                    new MetadataItem { BehaviorType = ci.AsType(), DataType = att.DataType }));

            //foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            //{
            //    // Для атрибутов поведения
            //    var behaviorAttributes = type.GetCustomAttributes<DataObjectBehaviorAttribute>();
            //    foreach (var attr in behaviorAttributes)
            //    {
            //        cache.Add(new MetadataItem
            //        {
            //            DataType = attr.DataType,
            //            BehaviorType = type,
            //            DisplayName = attr.DisplayName // Сохраняем DisplayName
            //        });
            //    }

            //    // Для DisplayName атрибутов свойств
            //    if (typeof(IBehavior).IsAssignableFrom(type)) continue;

            //    foreach (var prop in type.GetProperties())
            //    {
            //        var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            //        if (displayNameAttr != null)
            //        {
            //            cache.Add(new MetadataItem
            //            {
            //                DataType = type,
            //                BehaviorType = null, // Для свойств
            //                DisplayName = displayNameAttr.DisplayName
            //            });
            //        }
            //    }
            //}
        }

        public static IEnumerable<Type> GetBehaviors(Type dataType)
        {
            if (cache == null)
                CreateCache();

            List<Type> result = new List<Type>();

            while (dataType != null ? dataType != typeof(XPBaseObject) : false)
            {
                foreach (var bhv in cache.Where(c => c.DataType == dataType))
                {
                    if (!result.Contains(bhv.BehaviorType))
                    {
                        result.Add(bhv.BehaviorType);
                    }
                }

                dataType = dataType.BaseType;
            }

            foreach (var t in result.ToArray())
            {
                if (result.Any(c => c.IsSubclassOf(t)))
                    result.Remove(t);
            }

            return result;
        }

        public static IEnumerable<IBehavior> GetBhvInstancies(Type dataType) => GetBehaviors(dataType).Select(c => Activator.CreateInstance(c) as IBehavior);

        //public static string GetDisplayName(Type dataType, string propertyName = null)
        //{
        //    if (cache == null)
        //        CreateCache();

        //    var query = cache.Where(c => c.DataType == dataType);

        //    if (!string.IsNullOrEmpty(propertyName))
        //    {
        //        // Для свойств
        //        return query.FirstOrDefault(c =>
        //            c.BehaviorType == null &&
        //            c.DisplayName == propertyName)?.DisplayName;
        //    }

        //    // Для типа
        //    return query.FirstOrDefault(c => c.BehaviorType == null)?.DisplayName;
        //}

        //public static IEnumerable<string> GetDisplayNames(Type dataType)
        //{
        //    return cache?
        //        .Where(c => c.DataType == dataType && c.BehaviorType == null)
        //        .Select(c => c.DisplayName)
        //        ?? Enumerable.Empty<string>();
        //}
    }

    public record MetadataItem
    {
        public Type DataType { get; set; }
        public Type BehaviorType { get; set; }
        public string DisplayName { get; set; }
    }

    public interface IBehavior
    {
        public void BeforeSaving(IDatabaseObject data);
        public void OnSaving(IDatabaseObject data);
        public void OnSaved(IDatabaseObject data);

        public void OnDeleting(IDatabaseObject data);
        public void OnDeleted(IDatabaseObject data);
        //public void InnerValidate(IDatabaseObject data, ValidationContextEnum context, ValidationResults results);
    }
}
