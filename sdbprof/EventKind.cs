using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public enum EventKind : byte
    {
        VM_START = 0,
        VM_DEATH = 1,
        THREAD_START = 2,
        THREAD_DEATH = 3,
        APPDOMAIN_CREATE = 4,
        APPDOMAIN_UNLOAD = 5,
        METHOD_ENTRY = 6,
        METHOD_EXIT = 7,
        ASSEMBLY_LOAD = 8,
        ASSEMBLY_UNLOAD = 9,
        BREAKPOINT = 10,
        STEP = 11,
        TYPE_LOAD = 12,
        EXCEPTION = 13,
        KEEPALIVE = 14,
        USER_BREAK = 15,
        USER_LOG = 16
    }
}
