
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sdbprof
{
    class AssemblyInfo
    {
        public String name;
        public String location;
        /* TODO more fields? */

        public String GoodFilename
        {
            get
            {
                if (location.StartsWith("data-"))
                {
                    return String.Format("{0} ({1})", location, name);
                }
                else
                {
                    return String.Format("{0} ({1})", Path.GetFileName(location), location);
                }
            }
        }

        /* TODO: Equals for checking the methodId behind it, so that method infos compare properly
         * (methodIds are supposed to be unique across a debugging session)
         */

        /* Queries the debugger for information about this method ID */
        public static AssemblyInfo Query(UInt32 assemblyId)
        {
            AssemblyInfo ai = new AssemblyInfo();

            AssemblyGetNameReply reName = Program.sdb.SendPacketToStreamSync(new AssemblyGetNameRequest(assemblyId)) as AssemblyGetNameReply;
            ai.name = reName.assemblyName;

            AssemblyGetLocationReply reLocation = Program.sdb.SendPacketToStreamSync(new AssemblyGetLocationRequest(assemblyId)) as AssemblyGetLocationReply;
            ai.location = reLocation.location;
            
            return ai;
        }

        /* Cached method info */
        private static Dictionary<UInt32, AssemblyInfo> assemblyInfos = new Dictionary<uint, AssemblyInfo>();

        public static AssemblyInfo GetOrQueryAssemblyInfo(UInt32 assemblyId)
        {
            if (!assemblyInfos.ContainsKey(assemblyId))
            {
                assemblyInfos[assemblyId] = Query(assemblyId);
            }

            return assemblyInfos[assemblyId];
        }
    }
}
