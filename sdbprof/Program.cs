
using System;

namespace sdbprof
{
    class Program
    {
        public static void Main(string[] args)
        {
            SDB sdb = new SDB("127.0.0.1", 55555);
            while (true)
            {
                while (sdb.stream.DataAvailable)
                {
                    Packet p = sdb.ReadPacketFromStream();
                    Console.WriteLine("Read packet: " + p.ToString());
                    // should we even do this?? sdb.SendPacketToStream(new UnknownReplyPacket(p.id, 0, (UInt16) ErrorCode.NONE, new byte[0]));
                }
                sdb.SendPacketToStream(new UnknownRequestPacket(0, 0, 1, 1, new byte[0]));
            }
        }
    }
}
