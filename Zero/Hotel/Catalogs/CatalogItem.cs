using System;
using System.Collections.Generic;
using Zero.Hotel.Items;
using Zero.Messages;

namespace Zero.Hotel.Catalogs;

internal class CatalogItem
{
    public uint Id;

    public List<uint> ItemIds;

    public string Name;

    public int CreditsCost;

    public int PixelsCost;

    public int Amount;

    public bool IsDeal
    {
        get
        {
            if (ItemIds.Count > 1)
            {
                return true;
            }
            return false;
        }
    }

    public CatalogItem(uint Id, string Name, string ItemIds, int CreditsCost, int PixelsCost, int Amount)
    {
        this.Id = Id;
        this.Name = Name;
        this.ItemIds = new List<uint>();
        string[] array = ItemIds.Split(',');
        foreach (string ItemId in array)
        {
            this.ItemIds.Add(uint.Parse(ItemId));
        }
        this.CreditsCost = CreditsCost;
        this.PixelsCost = PixelsCost;
        this.Amount = Amount;
    }

    public Item GetBaseItem()
    {
        if (IsDeal)
        {
            return null;
        }
        return HolographEnvironment.GetGame().GetItemManager().GetItem(ItemIds[0]);
    }

    public void Serialize(ServerMessage Message)
    {
        if (IsDeal)
        {
            throw new NotImplementedException("Multipile item ids set for catalog item #" + Id + ", but this is usupported at this point");
        }
        Message.AppendUInt(Id);
        Message.AppendStringWithBreak(Name);
        Message.AppendInt32(CreditsCost);
        Message.AppendInt32(PixelsCost);
        Message.AppendInt32(1);
        Message.AppendStringWithBreak(GetBaseItem().Type);
        Message.AppendInt32(GetBaseItem().SpriteId);
        Message.AppendStringWithBreak("");
        Message.AppendInt32(Amount);
        Message.AppendInt32(-1);
    }
}
