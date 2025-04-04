using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Catalogs;

internal class CatalogPage
{
	private int Id;

	public int ParentId;

	public string Caption;

	public bool Visible;

	public bool Enabled;

	public bool ComingSoon;

	public uint MinRank;

	public bool ClubOnly;

	public int IconColor;

	public int IconImage;

	public string Layout;

	public string LayoutHeadline;

	public string LayoutTeaser;

	public string LayoutSpecial;

	public string Text1;

	public string Text2;

	public string TextDetails;

	public string TextTeaser;

	public List<CatalogItem> Items;

	public int PageId => Id;

	public CatalogPage(int Id, int ParentId, string Caption, bool Visible, bool Enabled, bool ComingSoon, uint MinRank, bool ClubOnly, int IconColor, int IconImage, string Layout, string LayoutHeadline, string LayoutTeaser, string LayoutSpecial, string Text1, string Text2, string TextDetails, string TextTeaser)
	{
		Items = new List<CatalogItem>();
		this.Id = Id;
		this.ParentId = ParentId;
		this.Caption = Caption;
		this.Visible = Visible;
		this.Enabled = Enabled;
		this.ComingSoon = ComingSoon;
		this.MinRank = MinRank;
		this.ClubOnly = ClubOnly;
		this.IconColor = IconColor;
		this.IconImage = IconImage;
		this.Layout = Layout;
		this.LayoutHeadline = LayoutHeadline;
		this.LayoutTeaser = LayoutTeaser;
		this.LayoutSpecial = LayoutSpecial;
		this.Text1 = Text1;
		this.Text2 = Text2;
		this.TextDetails = TextDetails;
		this.TextTeaser = TextTeaser;
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT id,item_ids,catalog_name,cost_credits,cost_pixels,amount FROM catalog_items WHERE page_id = '" + Id + "' ORDER BY item_ids ASC");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			if (!(Row["item_ids"].ToString() == "") && (int)Row["amount"] > 0)
			{
				Items.Add(new CatalogItem((uint)Row["id"], (string)Row["catalog_name"], (string)Row["item_ids"], (int)Row["cost_credits"], (int)Row["cost_pixels"], (int)Row["amount"]));
			}
		}
	}

	public CatalogItem GetItem(uint Id)
	{
		lock (Items)
		{
			foreach (CatalogItem Item in Items)
			{
				if (Item.Id == Id)
				{
					return Item;
				}
			}
		}
		return null;
	}

	public void Serialize(GameClient Session, ServerMessage Message)
	{
		Message.AppendBoolean(Visible);
		Message.AppendInt32(IconColor);
		Message.AppendInt32(IconImage);
		Message.AppendInt32(Id);
		Message.AppendStringWithBreak(Caption);
		Message.AppendBoolean(ComingSoon);
		Message.AppendInt32(HolographEnvironment.GetGame().GetCatalog().GetTreeSize(Session, Id));
	}
}
