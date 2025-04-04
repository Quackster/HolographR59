using System.Collections.Generic;
using Zero.Hotel.Items;
using Zero.Messages;

namespace Zero.Hotel.Rooms;

internal class Trade
{
    private List<TradeUser> Users;

    private int TradeStage;

    private uint RoomId;

    private uint oneId;

    private uint twoId;

    public bool AllUsersAccepted
    {
        get
        {
            lock (Users)
            {
                foreach (TradeUser User in Users)
                {
                    if (!User.HasAccepted)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public Trade(uint UserOneId, uint UserTwoId, uint RoomId)
    {
        oneId = UserOneId;
        twoId = UserTwoId;
        Users = new List<TradeUser>(2);
        Users.Add(new TradeUser(UserOneId, RoomId));
        Users.Add(new TradeUser(UserTwoId, RoomId));
        TradeStage = 1;
        this.RoomId = RoomId;
        foreach (TradeUser User in Users)
        {
            if (!User.GetRoomUser().Statusses.ContainsKey("trd"))
            {
                User.GetRoomUser().AddStatus("trd", "");
                User.GetRoomUser().UpdateNeeded = true;
            }
        }
        ServerMessage Message = new ServerMessage(104u);
        Message.AppendUInt(UserOneId);
        Message.AppendBoolean(Bool: true);
        Message.AppendUInt(UserTwoId);
        Message.AppendBoolean(Bool: true);
        SendMessageToUsers(Message);
    }

    public bool ContainsUser(uint Id)
    {
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                if (User.UserId == Id)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public TradeUser GetTradeUser(uint Id)
    {
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                if (User.UserId == Id)
                {
                    return User;
                }
            }
        }
        return null;
    }

    public void OfferItem(uint UserId, UserItem Item)
    {
        TradeUser User = GetTradeUser(UserId);
        if (User != null && Item != null && Item.GetBaseItem().AllowTrade && !User.HasAccepted && TradeStage == 1)
        {
            ClearAccepted();
            User.OfferedItems.Add(Item);
            UpdateTradeWindow();
        }
    }

    public void TakeBackItem(uint UserId, UserItem Item)
    {
        TradeUser User = GetTradeUser(UserId);
        if (User != null && Item != null && !User.HasAccepted && TradeStage == 1)
        {
            ClearAccepted();
            User.OfferedItems.Remove(Item);
            UpdateTradeWindow();
        }
    }

    public void Accept(uint UserId)
    {
        TradeUser User = GetTradeUser(UserId);
        if (User != null && TradeStage == 1)
        {
            User.HasAccepted = true;
            ServerMessage Message = new ServerMessage(109u);
            Message.AppendUInt(UserId);
            Message.AppendBoolean(Bool: true);
            SendMessageToUsers(Message);
            if (AllUsersAccepted)
            {
                SendMessageToUsers(new ServerMessage(111u));
                TradeStage++;
                ClearAccepted();
            }
        }
    }

    public void Unaccept(uint UserId)
    {
        TradeUser User = GetTradeUser(UserId);
        if (User != null && TradeStage == 1 && !AllUsersAccepted)
        {
            User.HasAccepted = false;
            ServerMessage Message = new ServerMessage(109u);
            Message.AppendUInt(UserId);
            Message.AppendBoolean(Bool: false);
            SendMessageToUsers(Message);
        }
    }

    public void CompleteTrade(uint UserId)
    {
        TradeUser User = GetTradeUser(UserId);
        if (User != null && TradeStage == 2)
        {
            User.HasAccepted = true;
            ServerMessage Message = new ServerMessage(109u);
            Message.AppendUInt(UserId);
            Message.AppendBoolean(Bool: true);
            SendMessageToUsers(Message);
            if (AllUsersAccepted)
            {
                TradeStage = 999;
                DeliverItems();
                CloseTradeClean();
            }
        }
    }

    public void ClearAccepted()
    {
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                User.HasAccepted = false;
            }
        }
    }

    public void UpdateTradeWindow()
    {
        ServerMessage Message = new ServerMessage(108u);
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                Message.AppendUInt(User.UserId);
                Message.AppendInt32(User.OfferedItems.Count);
                lock (User.OfferedItems)
                {
                    foreach (UserItem Item in User.OfferedItems)
                    {
                        Message.AppendUInt(Item.Id);
                        Message.AppendStringWithBreak(Item.GetBaseItem().Type.ToLower());
                        Message.AppendUInt(Item.Id);
                        Message.AppendInt32(Item.GetBaseItem().SpriteId);
                        Message.AppendBoolean(Bool: true);
                        Message.AppendBoolean(Bool: true);
                        Message.AppendStringWithBreak("");
                        Message.AppendBoolean(Bool: false);
                        Message.AppendBoolean(Bool: false);
                        Message.AppendBoolean(Bool: false);
                        if (Item.GetBaseItem().Type.ToLower() == "s")
                        {
                            Message.AppendInt32(-1);
                        }
                    }
                }
            }
        }
        SendMessageToUsers(Message);
    }

    public void DeliverItems()
    {
        List<UserItem> ItemsOne = GetTradeUser(oneId).OfferedItems;
        List<UserItem> ItemsTwo = GetTradeUser(twoId).OfferedItems;
        foreach (UserItem I in ItemsOne)
        {
            if (GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent()
                .GetItem(I.Id) == null)
            {
                GetTradeUser(oneId).GetClient().SendNotif("Trade failed.");
                GetTradeUser(twoId).GetClient().SendNotif("Trade failed.");
                return;
            }
        }
        foreach (UserItem I in ItemsTwo)
        {
            if (GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent()
                .GetItem(I.Id) == null)
            {
                GetTradeUser(oneId).GetClient().SendNotif("Trade failed.");
                GetTradeUser(twoId).GetClient().SendNotif("Trade failed.");
                return;
            }
        }
        foreach (UserItem I in ItemsOne)
        {
            GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent()
                .RemoveItem(I.Id);
            GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent()
                .AddItem(I.Id, I.BaseItem, I.ExtraData);
        }
        foreach (UserItem I in ItemsTwo)
        {
            GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent()
                .RemoveItem(I.Id);
            GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent()
                .AddItem(I.Id, I.BaseItem, I.ExtraData);
        }
        GetTradeUser(oneId).GetClient().GetHabbo().GetInventoryComponent()
            .UpdateItems(FromDatabase: false);
        GetTradeUser(twoId).GetClient().GetHabbo().GetInventoryComponent()
            .UpdateItems(FromDatabase: false);
    }

    public void CloseTradeClean()
    {
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                User.GetRoomUser().RemoveStatus("trd");
                User.GetRoomUser().UpdateNeeded = true;
            }
        }
        SendMessageToUsers(new ServerMessage(112u));
        GetRoom().ActiveTrades.Remove(this);
    }

    public void CloseTrade(uint UserId)
    {
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                if (User.GetRoomUser() != null)
                {
                    User.GetRoomUser().RemoveStatus("trd");
                    User.GetRoomUser().UpdateNeeded = true;
                }
            }
        }
        ServerMessage Message = new ServerMessage(110u);
        Message.AppendUInt(UserId);
        SendMessageToUsers(Message);
    }

    public void SendMessageToUsers(ServerMessage Message)
    {
        lock (Users)
        {
            foreach (TradeUser User in Users)
            {
                User.GetClient().SendMessage(Message);
            }
        }
    }

    private Room GetRoom()
    {
        return HolographEnvironment.GetGame().GetRoomManager().GetRoom(RoomId);
    }
}
