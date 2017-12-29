using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sdbprof
{
    class SDB
    {
        private Socket sdb;
        public NetworkStream stream;
        private UInt32 nextUnusedPacketId = 0;

        public SDB(string server, int port)
        {
            sdb = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sdb.Connect(server, port);
            stream = new NetworkStream(sdb);

            Handshake();
        }

        private static byte[] handshakeStr = Encoding.ASCII.GetBytes("DWP-Handshake");

        private void Handshake()
        {
            stream.Write(handshakeStr, 0, handshakeStr.Length);

            byte[] recvbuf = new byte[1024];
            int recvlen = stream.Read(recvbuf, 0, handshakeStr.Length);

            /* TODO: Check successful */

            Console.WriteLine("Received " + recvlen + " bytes: <" + Encoding.ASCII.GetString(recvbuf, 0, recvlen) + ">");
        }

        private void PutUInt32(UInt32 val, byte[] buf, int offset)
        {
            buf[offset + 0] = (byte)((val >> 24) & 0xff);
            buf[offset + 1] = (byte)((val >> 16) & 0xff);
            buf[offset + 2] = (byte)((val >> 8) & 0xff);
            buf[offset + 3] = (byte)((val >> 0) & 0xff);
        }

        private void PutUInt32(Int32 val, byte[] buf, int offset)
        {
            /* TODO: May truncate stuff */
            PutUInt32((UInt32)val, buf, offset);
        }

        private UInt32 ReadUInt32()
        {
            byte[] buf = new byte[4];
            stream.Read(buf, 0, 4);
            return Unpack.UInt32(buf);
        }

        private UInt16 ReadUInt16()
        {
            byte[] buf = new byte[2];
            stream.Read(buf, 0, 2);
            return Unpack.UInt16(buf);
        }

        public static string DumpByteArray(byte[] ba)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ba.Length; i++)
            {
                if (i != 0) sb.Append(' ');
                sb.AppendFormat("{0:X2}", ba[i]);
            }
            return sb.ToString();
        }

        public Packet ReadPacketFromStream()
        {
            UInt32 len = ReadUInt32();
            UInt32 id = ReadUInt32();
            byte flags = (byte) stream.ReadByte();
            byte nextHi = (byte)stream.ReadByte();
            byte nextLo = (byte)stream.ReadByte();
            byte[] extraData = new byte[len];
            stream.Read(extraData, 0, (int)len - 11);

            /*
            Console.WriteLine(String.Format("Packet data read: {0:X8} {1:X8} {2:X2} {3:X2} {4:X2}  {5}",
                len, id, flags, nextHi, nextLo, DumpByteArray(extraData)));
            */

            if ((flags & 0x80) != 0)
            {
                return DecodeReplyPacket(id, flags, (UInt16)((nextHi << 8) | (nextLo << 0)), extraData);
            } else
            {
                return DecodeRequestPacket(id, flags, nextHi, nextLo, extraData);
            }
        }

        private RequestPacket DecodeRequestPacket(UInt32 id, byte flags, byte set, byte cmd, byte[] extraData)
        {
            if (set == 64 /* Events */)
            {
                /* Events */
                if (cmd == 100 /* Composite */)
                {
                    return new EventCompositePacket(id, flags, extraData);
                }
            }
            return new UnknownRequestPacket(id, flags, set, cmd, extraData);
        }

        private ReplyPacket DecodeReplyPacket(UInt32 id, byte flags, UInt16 errorCode, byte[] extraData)
        {
            /* TODO: We need to store at least the type of every request sent, otherwise we will never know how to decode */
            return new UnknownReplyPacket(id, flags, errorCode, extraData);
        }

        public void SendPacketToStream(Packet packet)
        {
            if (packet is RequestPacket)
            {
                /* Give it an unique id */
                packet.id = nextUnusedPacketId;
                nextUnusedPacketId++;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                packet.ToStream(ms);
                // Console.WriteLine("Sending packet <" + DumpByteArray(ms.ToArray()) + ">");
                stream.Write(ms.ToArray(), 0, (int) ms.Length);
            }

            /* TODO: We might have to process a reply here ... or do we */
        }

        public void SendPacket(byte set, byte cmd, byte[] data)
        {
            byte[] sendbuf = new byte[1024];
            PutUInt32(11 + data.Length, sendbuf, 0);
            PutUInt32(nextUnusedPacketId, sendbuf, 4);
            nextUnusedPacketId++;
            sendbuf[8] = 0x00;
            sendbuf[9] = set;
            sendbuf[10] = cmd;

            Array.Copy(data, 0, sendbuf, 11, data.Length);

            sdb.Send(sendbuf, 11 + data.Length, SocketFlags.None);

            byte[] recvbuf = new byte[1024];
            int recvlen = sdb.Receive(recvbuf);

            Console.Write("Received " + recvlen + " bytes: <");
            for (int i = 0; i < recvlen; i++)
            {
                Console.Write(recvbuf[i].ToString("X2") + " ");
            }
            Console.WriteLine(">");
        }
    }
}
