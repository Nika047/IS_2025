﻿using DevExpress.Xpo.Metadata;
using data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ui.Helper
{
    public class XpoJsonSerializationContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        public bool SerializeCollections { get; set; } = false;
        public bool SerializeReferences { get; set; } = true;
        public bool SerializeByteArrays { get; set; } = true;
        readonly XPDictionary dictionary;

        public XpoJsonSerializationContractResolver()
        {
            dictionary = new ReflectionDictionary();
            //dictionary.GetDataStoreSchema(EntityTypeHelper.PersistentTypes);
        }

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            XPClassInfo classInfo = dictionary.QueryClassInfo(objectType);
            if (classInfo != null && classInfo.IsPersistent)
            {
                var allSerializableMembers = base.GetSerializableMembers(objectType);
                var serializableMembers = new List<MemberInfo>(allSerializableMembers.Count);
                foreach (MemberInfo member in allSerializableMembers)
                {
                    XPMemberInfo mi = classInfo.FindMember(member.Name);

                    if (!(mi.IsPersistent || mi.IsAliased || mi.IsCollection || mi.IsManyToManyAlias)
                        || ((mi.IsCollection || mi.IsManyToManyAlias) && !SerializeCollections)
                        || (mi.ReferenceType != null && !SerializeReferences)
                        || (mi.MemberType == typeof(byte[]) && !SerializeByteArrays))
                    {
                        continue;
                    }
                    serializableMembers.Add(member);
                }

                return serializableMembers;
            }

            return base.GetSerializableMembers(objectType);
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SampleJsonMvcBuilderExtensions
    {
        public static IMvcBuilder AddXpoModelJsonOptions(this IMvcBuilder builder, Action<ui.Helper.XpoJsonSerializationContractResolver> setupAction = null)
        {
            return builder.AddNewtonsoftJson(opt =>
            {
                var resolver = new ui.Helper.XpoJsonSerializationContractResolver();
                opt.SerializerSettings.ContractResolver = resolver;
                opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                setupAction?.Invoke(resolver);
            });
        }
    }
}
