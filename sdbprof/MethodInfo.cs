
using System;
using System.Collections.Generic;
using System.Linq;

namespace sdbprof
{
    class MethodInfo
    {
        public String name;
        public UInt32 declaringTypeId; /* XXX resolve */
        public TypeInfo declaringType;
        public UInt32 codeSize;
        public String sourceFilename;
        public Dictionary<UInt32, UInt32> ilOffsetsToLineNumbers;
        /* TODO: params, etc. */

        /* TODO: Equals for checking the methodId behind it, so that method infos compare properly
         * (methodIds are supposed to be unique across a debugging session)
         */
        
        /* Queries the debugger for information about this method ID */
        public static MethodInfo Query(UInt32 methodId)
        {
            MethodInfo mi = new MethodInfo();

            MethodGetNameReply reName = Program.sdb.SendPacketToStreamSync(new MethodGetNameRequest(methodId)) as MethodGetNameReply;
            mi.name = reName.methodName;

            MethodGetDeclaringTypeReply reType = Program.sdb.SendPacketToStreamSync(new MethodGetDeclaringTypeRequest(methodId)) as MethodGetDeclaringTypeReply;
            mi.declaringTypeId = reType.typeId;
            mi.declaringType = TypeInfo.GetOrQueryTypeInfo(reType.typeId);

            MethodGetDebugInfoReply reDebug = Program.sdb.SendPacketToStreamSync(new MethodGetDebugInfoRequest(methodId)) as MethodGetDebugInfoReply;
            mi.codeSize = reDebug.codeSize;
            mi.sourceFilename = reDebug.sourceFilename;
            mi.ilOffsetsToLineNumbers = reDebug.ilOffsetsToLineNumbers;

            return mi;
        }

        /* Cached method info */
        private static Dictionary<UInt32, MethodInfo> methodInfos = new Dictionary<uint, MethodInfo>();

        public static MethodInfo GetOrQueryMethodInfo(UInt32 methodId)
        {
            if (!methodInfos.ContainsKey(methodId))
            {
                methodInfos[methodId] = Query(methodId);
            }

            return methodInfos[methodId];
        }

        public String GoodName { get { return declaringType.GoodName + "." + name; } }
        public String GoodFilename {  get
            {
                if (String.IsNullOrWhiteSpace(sourceFilename))
                {
                    return declaringType.Classname + ".dll"; // XXX
                }
                else
                {
                    return sourceFilename;
                }
            } }
        public UInt32 LineNumber { get { if (ilOffsetsToLineNumbers.Count > 0) { return ilOffsetsToLineNumbers.Min((kv) => kv.Key); } else { return 0; } } }
    }
}
