using System.Collections.Generic;

namespace Zero.Hotel.Items;

internal class Item
{
    private uint Id;

    public int SpriteId;

    public string PublicName;

    public string Name;

    public string Type;

    public int Width;

    public int Length;

    public double Height;

    public bool Stackable;

    public bool Walkable;

    public bool IsSeat;

    public bool AllowRecycle;

    public bool AllowTrade;

    public bool AllowMarketplaceSell;

    public bool AllowGift;

    public bool AllowInventoryStack;

    public string InteractionType;

    public List<int> VendingIds;

    public int Modes;

    public uint ItemId => Id;

    public Item(uint Id, int Sprite, string PublicName, string Name, string Type, int Width, int Length, double Height, bool Stackable, bool Walkable, bool IsSeat, bool AllowRecycle, bool AllowTrade, bool AllowMarketplaceSell, bool AllowGift, bool AllowInventoryStack, string InteractionType, int Modes, string VendingIds)
    {
        this.Id = Id;
        SpriteId = Sprite;
        this.PublicName = PublicName;
        this.Name = Name;
        this.Type = Type;
        this.Width = Width;
        this.Length = Length;
        this.Height = Height;
        this.Stackable = Stackable;
        this.Walkable = Walkable;
        this.IsSeat = IsSeat;
        this.AllowRecycle = AllowRecycle;
        this.AllowTrade = AllowTrade;
        this.AllowMarketplaceSell = AllowMarketplaceSell;
        this.AllowGift = AllowGift;
        this.AllowInventoryStack = AllowInventoryStack;
        this.InteractionType = InteractionType;
        this.Modes = Modes;
        this.VendingIds = new List<int>();
        string[] array = VendingIds.Split(',');
        foreach (string VendingId in array)
        {
            this.VendingIds.Add(int.Parse(VendingId));
        }
    }
}
