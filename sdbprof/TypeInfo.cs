
using System;
using System.Collections.Generic;

namespace sdbprof
{
    class TypeInfo
    {
        public String Namespace;
        public String Classname;
        public String FullName;
        public UInt32 AssemblyId;
        public UInt32 ModuleId;
        /* TODO: More fields */

        /* TODO: Equals for checking the typeId behind it, so that type infos compare properly
         * (they are supposed to be unique across a debugging session)
         */

        /* Queries the debugger for information about this type ID */
        public static TypeInfo Query(UInt32 typeId)
        {
            TypeInfo ti = new TypeInfo();
            TypeGetInfoReply re = Program.sdb.SendPacketToStreamSync(new TypeGetInfoRequest(typeId)) as TypeGetInfoReply;
            ti.Namespace = re.Namespace;
            ti.Classname = re.Classname;
            ti.FullName = re.FullName;
            ti.AssemblyId = re.AssemblyId;
            ti.ModuleId = re.ModuleId;

            return ti;
        }

        /* Cached type info */
        private static Dictionary<UInt32, TypeInfo> typeInfos = new Dictionary<uint, TypeInfo>();

        public static TypeInfo GetOrQueryTypeInfo(UInt32 typeId)
        {
            if (!typeInfos.ContainsKey(typeId))
            {
                typeInfos[typeId] = Query(typeId);
            }

            return typeInfos[typeId];
        }

        public String GoodName { get { return FullName; } }
    }
}
