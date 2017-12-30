
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
                sdb.ProcessReplies();
                sdb.SendPacketToStream(
                    new VMVersionRequestPacket(),
                    (reply) => { Console.WriteLine(reply.ToString()); }
                    );
            }
        }
    }
}
