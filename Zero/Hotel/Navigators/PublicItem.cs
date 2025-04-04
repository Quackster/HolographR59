using Zero.Hotel.Rooms;
using Zero.Messages;

namespace Zero.Hotel.Navigators;

internal class PublicItem
{
    private int BannerId;

    public int Type;

    public string Caption;

    public string Image;

    public PublicImageType ImageType;

    public uint RoomId;

    public int CategoryId;

    public int ParentId;

    public int Id => BannerId;

    public bool IsCategory
    {
        get
        {
            if (CategoryId > 0)
            {
                return true;
            }
            return false;
        }
    }

    public RoomData RoomData
    {
        get
        {
            if (IsCategory)
            {
                return new RoomData();
            }
            return HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(RoomId);
        }
    }

    public PublicItem(int Id, int Type, string Caption, string Image, PublicImageType ImageType, uint RoomId, int CategoryId, int ParentId)
    {
        BannerId = Id;
        this.Type = Type;
        this.Caption = Caption;
        this.Image = Image;
        this.ImageType = ImageType;
        this.RoomId = RoomId;
        this.CategoryId = CategoryId;
        this.ParentId = ParentId;
    }

    public void Serialize(ServerMessage Message)
    {
        Message.AppendInt32(Id);
        if (IsCategory)
        {
            Message.AppendStringWithBreak(Caption);
        }
        else
        {
            Message.AppendStringWithBreak(RoomData.Name);
        }
        Message.AppendStringWithBreak(RoomData.Description);
        Message.AppendInt32(Type);
        Message.AppendStringWithBreak(Caption);
        Message.AppendStringWithBreak((ImageType == PublicImageType.EXTERNAL) ? Image : "");
        if (!IsCategory)
        {
            Message.AppendUInt(0u);
            Message.AppendInt32(RoomData.UsersNow);
            Message.AppendInt32(3);
            Message.AppendStringWithBreak((ImageType == PublicImageType.INTERNAL) ? Image : "");
            Message.AppendUInt(1337u);
            Message.AppendInt32(0);
            Message.AppendStringWithBreak(RoomData.CCTs);
            Message.AppendInt32(RoomData.UsersMax);
            Message.AppendUInt(RoomId);
        }
        else
        {
            Message.AppendInt32(0);
            Message.AppendInt32(4);
            Message.AppendInt32(CategoryId);
        }
    }
}
