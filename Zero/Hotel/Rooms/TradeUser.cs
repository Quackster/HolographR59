using System.Collections.Generic;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;

namespace Zero.Hotel.Rooms;

internal class TradeUser
{
    public uint UserId;

    private uint RoomId;

    private bool Accepted;

    public List<UserItem> OfferedItems;

    public bool HasAccepted
    {
        get
        {
            return Accepted;
        }
        set
        {
            Accepted = value;
        }
    }

    public TradeUser(uint UserId, uint RoomId)
    {
        this.UserId = UserId;
        this.RoomId = RoomId;
        Accepted = false;
        OfferedItems = new List<UserItem>();
    }

    public RoomUser GetRoomUser()
    {
        return HolographEnvironment.GetGame().GetRoomManager().GetRoom(RoomId)?.GetRoomUserByHabbo(UserId);
    }

    public GameClient GetClient()
    {
        return HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
    }
}
