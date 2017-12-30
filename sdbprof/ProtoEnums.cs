using System;

namespace sdbprof
{
    public enum CommandSet : byte {
        VM = 1,
        OBJECT_REF = 9,
        STRING_REF = 10,
        THREAD = 11,
        ARRAY_REF = 13,
        EVENT_REQUEST = 15,
        STACK_FRAME = 16,
        APPDOMAIN = 20,
        ASSEMBLY = 21,
        METHOD = 22,
        TYPE = 23,
        MODULE = 24,
        FIELD = 25,
        EVENT = 64
    }

    public enum EventKind : byte {
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

    public enum SuspendPolicy : byte {
        NONE = 0,
        EVENT_THREAD = 1,
        ALL = 2
    }

    public enum ErrorCode : UInt16 {
        NONE = 0,
        INVALID_OBJECT = 20,
        INVALID_FIELDID = 25,
        INVALID_FRAMEID = 30,
        NOT_IMPLEMENTED = 100,
        NOT_SUSPENDED = 101,
        INVALID_ARGUMENT = 102,
        UNLOADED = 103,
        NO_INVOCATION = 104,
        ABSENT_INFORMATION = 105,
        NO_SEQ_POINT_AT_IL_OFFSET = 106,
        INVOKE_ABORTED = 107,
        LOADER_ERROR = 200, /*XXX extend the protocol to pass this information down the pipe */
    }

    public enum ModifierKind : byte {
        COUNT = 1,
        THREAD_ONLY = 3,
        LOCATION_ONLY = 7,
        EXCEPTION_ONLY = 8,
        STEP = 10,
        ASSEMBLY_ONLY = 11,
        SOURCE_FILE_ONLY = 12,
        TYPE_NAME_ONLY = 13,
        NONE = 14
    }

    public enum StepDepth : byte {
        INTO = 0,
        OVER = 1,
        OUT = 2
    }

    public enum StepSize : byte {
        MIN = 0,
        LINE = 1
    }

    [Flags] public enum StepFilter : byte {
        NONE = 0,
        STATIC_CTOR = 1,
        DEBUGGER_HIDDEN = 2,
        DEBUGGER_STEP_THROUGH = 4,
        DEBUGGER_NON_USER_CODE = 8
    }

    public enum DebuggerTokenType : byte {
        STRING = 0,
        TYPE = 1,
        FIELD = 2,
        METHOD = 3,
        UNKNOWN = 4
    }

    public enum ValueTypeId : byte {
        NULL = 0xf0,
        TYPE = 0xf1,
        PARENT_VTYPE = 0xf2
    }

    [Flags] public enum StackFrameFlags : byte {
        DEBUGGER_INVOKE = 1,
        NATIVE_TRANSITION = 2
    }

    [Flags] public enum InvokeFlags : byte {
        DISABLE_BREAKPOINTS = 1,
        SINGLE_THREADED = 2,
        RETURN_OUT_THIS = 4,
        RETURN_OUT_ARGS = 8,
        VIRTUAL = 16
    }

    public enum BindingFlagsExtensions : UInt32 {
        IGNORE_CASE = 0x70000000,
    }

    /* Command types */

    public enum CmdVM : byte {
        VERSION = 1,
        ALL_THREADS = 2,
        SUSPEND = 3,
        RESUME = 4,
        EXIT = 5,
        DISPOSE = 6,
        INVOKE_METHOD = 7,
        SET_PROTOCOL_VERSION = 8,
        ABORT_INVOKE = 9,
        SET_KEEPALIVE = 10,
        GET_TYPES_FOR_SOURCE_FILE = 11,
        GET_TYPES = 12,
        INVOKE_METHODS = 13,
        START_BUFFERING = 14,
        STOP_BUFFERING = 15
    }

    public enum CmdThread : byte {
        GET_FRAME_INFO = 1,
        GET_NAME = 2,
        GET_STATE = 3,
        GET_INFO = 4,
        GET_ID = 5,
        GET_TID = 6,
        SET_IP = 7
    }
    
    public enum CmdEvent : byte {
        SET = 1,
        CLEAR = 2,
        CLEAR_ALL_BREAKPOINTS = 3
    }

    public enum CmdComposite : byte {
        COMPOSITE = 100
    }

    public enum CmdAppDomain : byte {
        GET_ROOT_DOMAIN = 1,
        GET_FRIENDLY_NAME = 2,
        GET_ASSEMBLIES = 3,
        GET_ENTRY_ASSEMBLY = 4,
        CREATE_STRING = 5,
        GET_CORLIB = 6,
        CREATE_BOXED_VALUE = 7
    }

    public enum CmdAssembly : byte {
        GET_LOCATION = 1,
        GET_ENTRY_POINT = 2,
        GET_MANIFEST_MODULE = 3,
        GET_OBJECT = 4,
        GET_TYPE = 5,
        GET_NAME = 6,
        GET_DOMAIN = 7
    }

    public enum CmdModule : byte {
        GET_INFO = 1,
    }

    public enum CmdField : byte {
        GET_INFO = 1,
    }
    
    public enum CmdMethod : byte {
        GET_NAME = 1,
        GET_DECLARING_TYPE = 2,
        GET_DEBUG_INFO = 3,
        GET_PARAM_INFO = 4,
        GET_LOCALS_INFO = 5,
        GET_INFO = 6,
        GET_BODY = 7,
        RESOLVE_TOKEN = 8,
        GET_CATTRS = 9,
        MAKE_GENERIC_METHOD = 10
    }

    public enum CmdType : byte {
        GET_INFO = 1,
        GET_METHODS = 2,
        GET_FIELDS = 3,
        GET_VALUES = 4,
        GET_OBJECT = 5,
        GET_SOURCE_FILES = 6,
        SET_VALUES = 7,
        IS_ASSIGNABLE_FROM = 8,
        GET_PROPERTIES = 9,
        GET_CATTRS = 10,
        GET_FIELD_CATTRS = 11,
        GET_PROPERTY_CATTRS = 12,
        GET_SOURCE_FILES_2 = 13,
        GET_VALUES_2 = 14,
        GET_METHODS_BY_NAME_FLAGS = 15,
        GET_INTERFACES = 16,
        GET_INTERFACE_MAP = 17,
        IS_INITIALIZED = 18,
        CREATE_INSTANCE = 19
    }

    public enum CmdStackFrame : byte {
        GET_VALUES = 1,
        GET_THIS = 2,
        SET_VALUES = 3,
        GET_DOMAIN = 4,
        SET_THIS = 5,
    }
    
    public enum CmdArray : byte {
        GET_LENGTH = 1,
        GET_VALUES = 2,
        SET_VALUES = 3,
    }

    public enum CmdString : byte {
        GET_VALUE = 1,
        GET_LENGTH = 2,
        GET_CHARS = 3
    }

    public enum CmdObject : byte {
        GET_TYPE = 1,
        GET_VALUES = 2,
        IS_COLLECTED = 3,
        GET_ADDRESS = 4,
        GET_DOMAIN = 5,
        SET_VALUES = 6,
        GET_INFO = 7,
    }
}
