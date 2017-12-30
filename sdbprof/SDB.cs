using System;
using System.Collections.Generic;
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

        /* Handling replies */

        private class OutstandingPacketData
        {
            private RequestFrame requestFrame;
            private IRequestPacket requestPacket;
            private Action<IReplyPacket> onReply;
            public OutstandingPacketData(RequestFrame requestFrame, IRequestPacket requestPacket, Action<IReplyPacket> onReply)
            {
                this.requestFrame = requestFrame;
                this.requestPacket = requestPacket;
                this.onReply = onReply;
            }
            public void Execute(ReplyFrame replyFrame)
            {
                this.onReply(requestPacket.DecodeReplyFrame(replyFrame));
            }
        }

        private Dictionary<UInt32, OutstandingPacketData> outstanding = new Dictionary<UInt32, OutstandingPacketData>();

        public void ProcessReplies()
        {
            while (stream.DataAvailable)
            {
                Frame frame = Frame.ReadFromStream(stream);
                if (frame is ReplyFrame)
                {
                    if (outstanding.ContainsKey(frame.id))
                    {
                        outstanding[frame.id].Execute(frame as ReplyFrame);
                        outstanding.Remove(frame.id);
                    }
                    else
                    {
                        Console.WriteLine("Reply for unknown request received! " + frame.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Request frame received: " + frame.ToString());
                }
            }
        }

        /* Send a packet and do something on reply */

        public void SendPacketToStream(IRequestPacket request, Action<IReplyPacket> onReply)
        {
            /* Turn into a frame */
            RequestFrame rf = request.MakeRequestFrame();

            /* Give it an unique id and store the action to be executed later */
            rf.id = nextUnusedPacketId;
            outstanding[rf.id] = new OutstandingPacketData(rf, request, onReply);
            nextUnusedPacketId++;

            /* Send and hope for the best */
            rf.ToStream(stream);
        }
    }
}
