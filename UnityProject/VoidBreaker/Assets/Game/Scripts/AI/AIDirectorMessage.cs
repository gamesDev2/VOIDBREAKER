using System.Collections.Generic;
/// <summary>
/// A simple message class for the AI Director to pass between agents.
/// </summary>
public class AIDirectorMessage
{
    public MessageType msgType;    // Renamed to avoid "type" ambiguity
    public GOAPAgent msgSender;    // Renamed to avoid "sender" ambiguity
    public GOAPAgent msgReceiver;  // Renamed to avoid "receiver" ambiguity
    public string msgContent;      // Renamed to avoid "content" ambiguity

    public AIDirectorMessage(MessageType t, GOAPAgent s, GOAPAgent r, string c)
    {
        this.msgType = t;
        this.msgSender = s;
        this.msgReceiver = r;
        this.msgContent = c;
    }
}