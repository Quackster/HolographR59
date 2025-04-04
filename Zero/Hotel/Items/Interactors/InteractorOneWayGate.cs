using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorOneWayGate : FurniInteractor
{
    public override void OnPlace(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "0";
        if (Item.InteractingUser != 0)
        {
            RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser);
            if (User != null)
            {
                User.ClearMovement(Update: true);
                User.UnlockWalking();
            }
            Item.InteractingUser = 0u;
        }
    }

    public override void OnRemove(GameClient Session, RoomItem Item)
    {
        Item.ExtraData = "0";
        if (Item.InteractingUser != 0)
        {
            RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser);
            if (User != null)
            {
                User.ClearMovement(Update: true);
                User.UnlockWalking();
            }
            Item.InteractingUser = 0u;
        }
    }

    public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
    {
        RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (User == null)
        {
            return;
        }
        if (User.Coordinate != Item.SquareInFront && User.CanWalk)
        {
            User.MoveTo(Item.SquareInFront);
        }
        else if (Item.GetRoom().CanWalk(Item.SquareBehind.x, Item.SquareBehind.y, Item.Z, LastStep: true) && Item.InteractingUser == 0)
        {
            Item.InteractingUser = User.HabboId;
            User.CanWalk = false;
            if (User.IsWalking && (User.GoalX != Item.SquareInFront.x || User.GoalY != Item.SquareInFront.y))
            {
                User.ClearMovement(Update: true);
            }
            User.AllowOverride = true;
            User.MoveTo(Item.Coordinate);
            Item.ReqUpdate(3);
        }
    }
}
