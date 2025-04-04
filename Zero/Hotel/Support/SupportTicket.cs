using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Support;

internal class SupportTicket
{
    private uint Id;

    public int Score;

    public int Type;

    public TicketStatus Status;

    public uint SenderId;

    public uint ReportedId;

    public uint ModeratorId;

    public string Message;

    public uint RoomId;

    public string RoomName;

    public double Timestamp;

    public int TabId
    {
        get
        {
            if (Status == TicketStatus.OPEN)
            {
                return 1;
            }
            if (Status == TicketStatus.PICKED)
            {
                return 2;
            }
            return 0;
        }
    }

    public uint TicketId => Id;

    public SupportTicket(uint Id, int Score, int Type, uint SenderId, uint ReportedId, string Message, uint RoomId, string RoomName, double Timestamp)
    {
        this.Id = Id;
        this.Score = Score;
        this.Type = Type;
        Status = TicketStatus.OPEN;
        this.SenderId = SenderId;
        this.ReportedId = ReportedId;
        ModeratorId = 0u;
        this.Message = Message;
        this.RoomId = RoomId;
        this.RoomName = RoomName;
        this.Timestamp = Timestamp;
    }

    public void Pick(uint ModeratorId, bool UpdateInDb)
    {
        Status = TicketStatus.PICKED;
        this.ModeratorId = ModeratorId;
        if (UpdateInDb)
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update moderation_tickets SET status = 'picked', moderator_id = '" + ModeratorId + "' WHERE id = '" + Id + "' LIMIT 1");
            }
        }
    }

    public void Close(TicketStatus NewStatus, bool UpdateInDb)
    {
        Status = NewStatus;
        if (UpdateInDb)
        {
            string dbType = "";
            dbType = NewStatus switch
            {
                TicketStatus.ABUSIVE => "abusive",
                TicketStatus.INVALID => "invalid",
                _ => "resolved",
            };
            using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
            dbClient.ExecuteQuery("Update moderation_tickets SET status = '" + dbType + "' WHERE id = '" + Id + "' LIMIT 1");
        }
    }

    public void Release(bool UpdateInDb)
    {
        Status = TicketStatus.OPEN;
        if (UpdateInDb)
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update moderation_tickets SET status = 'open' WHERE id = '" + Id + "' LIMIT 1");
            }
        }
    }

    public void Delete(bool UpdateInDb)
    {
        Status = TicketStatus.DELETED;
        if (UpdateInDb)
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update moderation_tickets SET status = 'deleted' WHERE id = '" + Id + "' LIMIT 1");
            }
        }
    }

    public ServerMessage Serialize()
    {
        ServerMessage Message = new ServerMessage(530u);
        Message.AppendUInt(Id);
        Message.AppendInt32(TabId);
        Message.AppendInt32(11);
        Message.AppendInt32(Type);
        Message.AppendInt32(11);
        Message.AppendInt32(Score);
        Message.AppendUInt(SenderId);
        Message.AppendStringWithBreak(HolographEnvironment.GetGame().GetClientManager().GetNameById(SenderId));
        Message.AppendUInt(ReportedId);
        Message.AppendStringWithBreak(HolographEnvironment.GetGame().GetClientManager().GetNameById(ReportedId));
        Message.AppendUInt(ModeratorId);
        Message.AppendStringWithBreak(HolographEnvironment.GetGame().GetClientManager().GetNameById(ModeratorId));
        Message.AppendStringWithBreak(this.Message);
        Message.AppendUInt(RoomId);
        Message.AppendStringWithBreak(RoomName);
        return Message;
    }
}
