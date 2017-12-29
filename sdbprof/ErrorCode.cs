using System;

namespace sdbprof
{
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

    static class ErrorCodeExt
    {
        public static string ToString(this ErrorCode e)
        {
            if (Enum.IsDefined(typeof(ErrorCode), e))
            {
                return Enum.GetName(typeof(ErrorCode), e);
            }
            else
            {
                return String.Format("Unknown:0x{0:X4}", (int)e);
            }
        }
    }
}
