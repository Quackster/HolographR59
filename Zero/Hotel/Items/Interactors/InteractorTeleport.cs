using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.Items.Interactors;

internal class InteractorTeleport : FurniInteractor
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
				User.AllowOverride = false;
				User.CanWalk = true;
			}
			Item.InteractingUser = 0u;
		}
		if (Item.InteractingUser2 != 0)
		{
			RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser2);
			if (User != null)
			{
				User.ClearMovement(Update: true);
				User.AllowOverride = false;
				User.CanWalk = true;
			}
			Item.InteractingUser2 = 0u;
		}
		Item.GetRoom().RegenerateUserMatrix();
	}

	public override void OnRemove(GameClient Session, RoomItem Item)
	{
		Item.ExtraData = "0";
		if (Item.InteractingUser != 0)
		{
			Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser)?.UnlockWalking();
			Item.InteractingUser = 0u;
		}
		if (Item.InteractingUser2 != 0)
		{
			Item.GetRoom().GetRoomUserByHabbo(Item.InteractingUser2)?.UnlockWalking();
			Item.InteractingUser2 = 0u;
		}
		Item.GetRoom().RegenerateUserMatrix();
	}

	public override void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
	{
		RoomUser User = Item.GetRoom().GetRoomUserByHabbo(Session.GetHabbo().Id);
		if (User == null)
		{
			return;
		}
		if (User.Coordinate == Item.Coordinate || User.Coordinate == Item.SquareInFront)
		{
			if (Item.InteractingUser == 0)
			{
				User.TeleDelay = -1;
				Item.InteractingUser = User.GetClient().GetHabbo().Id;
			}
		}
		else if (User.CanWalk)
		{
			User.MoveTo(Item.SquareInFront);
		}
	}
}
