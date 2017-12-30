namespace sdbprof
{
    public interface IRequestPacket
    {
        RequestFrame MakeRequestFrame();
        IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame);
    }
}