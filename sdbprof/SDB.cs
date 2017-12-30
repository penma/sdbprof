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
            private Action<IReplyPacket, object> onReply;
            private object onReplyData;
            public OutstandingPacketData(RequestFrame requestFrame, IRequestPacket requestPacket, Action<IReplyPacket, object> onReply, object onReplyData)
            {
                this.requestFrame = requestFrame;
                this.requestPacket = requestPacket;
                this.onReply = onReply;
                this.onReplyData = onReplyData;
            }
            public void Execute(ReplyFrame replyFrame)
            {
                this.onReply(requestPacket.DecodeReplyFrame(replyFrame), onReplyData);
            }
        }

        private Dictionary<UInt32, OutstandingPacketData> outstanding = new Dictionary<UInt32, OutstandingPacketData>();

        /* Postponed replies in case someone wanted a synchronous answer */
        private Queue<Frame> postponedFrames = new Queue<Frame>();

        public void ProcessReplies()
        {
            while (postponedFrames.Count > 0)
            {
                // Console.WriteLine("(processing postponed frame)");
                ProcessFrame(postponedFrames.Dequeue());
            }

            while (stream.DataAvailable)
            {
                Frame frame = Frame.ReadFromStream(stream);
                ProcessFrame(frame);
            }
        }

        private void ProcessFrame(Frame frame)
        {
            //Console.WriteLine("(Received frame: " + frame.ToString() + ")");
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
                RequestFrame req = frame as RequestFrame;
                if (req.commandSet == CommandSet.EVENT && req.command == (byte)CmdComposite.COMPOSITE)
                {
                    EventCompositePacket ecp = EventCompositePacket.DecodeFrame(req);
                    Console.WriteLine("Event: " + ecp);
               }
                else
                {
                    Console.WriteLine("Unknown request frame received: " + frame.ToString());
                }
            }
        }

        /* Send a packet and do something on reply */

        public void SendPacketToStream(IRequestPacket request, Action<IReplyPacket, object> onReply, object onReplyData = null)
        {
            /* Turn into a frame */
            RequestFrame rf = request.MakeRequestFrame();

            /* Give it an unique id and store the action to be executed later */
            rf.id = nextUnusedPacketId;
            outstanding[rf.id] = new OutstandingPacketData(rf, request, onReply, onReplyData);
            nextUnusedPacketId++;

            /* Send and hope for the best */
            // Console.WriteLine("(Sending frame: " + rf.ToString() + ")");
            rf.ToStream(stream);
        }

        public IReplyPacket SendPacketToStreamSync(IRequestPacket request)
        {
            /* Turn into a frame */
            RequestFrame rf = request.MakeRequestFrame();

            /* Give it an unique id and store the action to be executed later */
            rf.id = nextUnusedPacketId;
            nextUnusedPacketId++;

            /* Send and hope for the best */
            // Console.WriteLine("(Sending frame: " + rf.ToString() + ")");
            rf.ToStream(stream);

            while (true)
            {
                Frame frame = Frame.ReadFromStream(stream);
                if (!(frame.id == rf.id && frame is ReplyFrame))
                {
                    // Console.WriteLine("Postponing a frame");
                    postponedFrames.Enqueue(frame);
                }
                else
                {
                    return request.DecodeReplyFrame(frame as ReplyFrame);
                }
            }
        }
    }
}
