using System.Collections.Generic;
public class AIDirectorMessage
{
    public MessageType type;
    public GOAPAgent sender;
    public GOAPAgent receiver; // if null, message can be broadcast or directed via custom methods
    public string content;

    public AIDirectorMessage(MessageType type, GOAPAgent sender, GOAPAgent receiver, string content)
    {
        this.type = type;
        this.sender = sender;
        this.receiver = receiver;
        this.content = content;
    }
}
