namespace Zero.Hotel.Rooms;

internal class TeleUserData
{
    private RoomUser User;

    private uint RoomId;

    private uint TeleId;

    public TeleUserData(RoomUser User, uint RoomId, uint TeleId)
    {
        this.User = User;
        this.RoomId = RoomId;
        this.TeleId = TeleId;
    }

    public void Execute()
    {
        if (User != null && !User.IsBot)
        {
            User.GetClient().GetHabbo().IsTeleporting = true;
            User.GetClient().GetHabbo().TeleporterId = TeleId;
            User.GetClient().GetMessageHandler().PrepareRoomForUser(RoomId, "");
        }
    }
}
