using System;
using System.Collections.Generic;
using Zero.Hotel.GameClients;
using Zero.Messages;

namespace Zero.Hotel.Rooms;

internal class RoomEvent
{
    public string Name;

    public string Description;

    public int Category;

    public List<string> Tags;

    public string StartTime;

    public uint RoomId;

    public RoomEvent(uint RoomId, string Name, string Description, int Category, List<string> Tags)
    {
        this.RoomId = RoomId;
        this.Name = Name;
        this.Description = Description;
        this.Category = Category;
        this.Tags = Tags;
        StartTime = DateTime.Now.ToShortTimeString();
    }

    public ServerMessage Serialize(GameClient Session)
    {
        ServerMessage Message = new ServerMessage(370u);
        Message.AppendStringWithBreak(string.Concat(Session.GetHabbo().Id));
        Message.AppendStringWithBreak(Session.GetHabbo().Username);
        Message.AppendStringWithBreak(string.Concat(RoomId));
        Message.AppendInt32(Category);
        Message.AppendStringWithBreak(Name);
        Message.AppendStringWithBreak(Description);
        Message.AppendStringWithBreak(StartTime);
        Message.AppendInt32(Tags.Count);
        lock (Tags)
        {
            foreach (string Tag in Tags)
            {
                Message.AppendStringWithBreak(Tag);
            }
        }
        return Message;
    }
}
