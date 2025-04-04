using Zero.Hotel.GameClients;

namespace Zero.Hotel.Items.Interactors;

internal abstract class FurniInteractor
{
    public abstract void OnPlace(GameClient Session, RoomItem Item);

    public abstract void OnRemove(GameClient Session, RoomItem Item);

    public abstract void OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights);
}
