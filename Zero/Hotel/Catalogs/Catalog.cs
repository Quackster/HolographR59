using System;
using System.Collections.Generic;
using System.Data;
using Zero.Core;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Navigators;
using Zero.Hotel.Pets;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Catalogs;

internal class Catalog
{
	public Dictionary<int, CatalogPage> Pages;

	public List<EcotronReward> EcotronRewards;

	private VoucherHandler VoucherHandler;

	private Marketplace Marketplace;

	private readonly object ItemGeneratorLock = new object();

	public Catalog()
	{
		VoucherHandler = new VoucherHandler();
		Marketplace = new Marketplace();
	}

	public void Initialize()
	{
		Pages = new Dictionary<int, CatalogPage>();
		EcotronRewards = new List<EcotronReward>();
		DataTable Data = null;
		DataTable EcoData = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT * FROM catalog_pages ORDER BY order_num ASC");
			EcoData = dbClient.ReadDataTable("SELECT * FROM ecotron_rewards ORDER BY item_id");
		}
		if (Data != null)
		{
			foreach (DataRow Row in Data.Rows)
			{
				bool Visible = false;
				bool Enabled = false;
				bool ComingSoon = false;
				if (Row["visible"].ToString() == "1")
				{
					Visible = true;
				}
				if (Row["enabled"].ToString() == "1")
				{
					Enabled = true;
				}
				if (Row["coming_soon"].ToString() == "1")
				{
					ComingSoon = true;
				}
				Pages.Add((int)Row["id"], new CatalogPage((int)Row["id"], (int)Row["parent_id"], (string)Row["caption"], Visible, Enabled, ComingSoon, (uint)Row["min_rank"], HolographEnvironment.EnumToBool(Row["club_only"].ToString()), (int)Row["icon_color"], (int)Row["icon_image"], (string)Row["page_layout"], (string)Row["page_headline"], (string)Row["page_teaser"], (string)Row["page_special"], (string)Row["page_text1"], (string)Row["page_text2"], (string)Row["page_text_details"], (string)Row["page_text_teaser"]));
			}
		}
		if (EcoData == null)
		{
			return;
		}
		foreach (DataRow Row in EcoData.Rows)
		{
			EcotronRewards.Add(new EcotronReward((uint)Row["id"], (uint)Row["display_id"], (uint)Row["item_id"], (uint)Row["reward_level"]));
		}
	}

	public CatalogItem FindItem(uint ItemId)
	{
		lock (Pages.Values)
		{
			foreach (CatalogPage Page in Pages.Values)
			{
				lock (Page.Items)
				{
					foreach (CatalogItem Item in Page.Items)
					{
						if (Item.Id == ItemId)
						{
							return Item;
						}
					}
				}
			}
		}
		return null;
	}

	public bool IsItemInCatalog(uint BaseId)
	{
		DataRow Row = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Row = dbClient.ReadDataRow("SELECT id FROM catalog_items WHERE item_ids = '" + BaseId + "' LIMIT 1");
		}
		if (Row != null)
		{
			return true;
		}
		return false;
	}

	public int GetTreeSize(GameClient Session, int TreeId)
	{
		int i = 0;
		lock (Pages)
		{
			foreach (CatalogPage Page in Pages.Values)
			{
				if (Page.MinRank <= Session.GetHabbo().Rank && Page.ParentId == TreeId)
				{
					i++;
				}
			}
		}
		return i;
	}

	public CatalogPage GetPage(int Page)
	{
		if (!Pages.ContainsKey(Page))
		{
			return null;
		}
		return Pages[Page];
	}

	public void HandlePurchase(GameClient Session, int PageId, uint ItemId, string ExtraData, bool IsGift, string GiftUser, string GiftMessage)
	{
		CatalogPage Page = GetPage(PageId);
		if (Page == null || Page.ComingSoon || !Page.Enabled || !Page.Visible)
		{
			return;
		}
		if (Page.MinRank > Session.GetHabbo().Rank || !Page.Visible)
		{
			Session.SendNotif("Tentativa de Hacking Detectada");
			return;
		}
		if (Page.ClubOnly && !Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip"))
		{
			Session.SendNotif("¡Esta página es sólo para miembros del club!");
			return;
		}
		if (Page.ClubOnly && !Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_club"))
		{
			Session.SendNotif("¡Esta página es sólo para miembros del club!");
			return;
		}
		CatalogItem Item = Page.GetItem(ItemId);
		if (Item == null)
		{
			return;
		}
		uint GiftUserId = 0u;
		if (IsGift)
		{
			if (!Item.GetBaseItem().AllowGift)
			{
				return;
			}
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("gift_user", GiftUser);
				try
				{
					GiftUserId = (uint)dbClient.ReadDataRow("SELECT id FROM users WHERE username = @gift_user LIMIT 1")[0];
				}
				catch (Exception)
				{
				}
			}
			if (GiftUserId == 0)
			{
				Session.GetMessageHandler().GetResponse().Init(76u);
				Session.GetMessageHandler().GetResponse().AppendBoolean(Bool: true);
				Session.GetMessageHandler().GetResponse().AppendStringWithBreak(GiftUser);
				Session.GetMessageHandler().SendResponse();
				return;
			}
		}
		bool CreditsError = false;
		bool PixelError = false;
		if (Session.GetHabbo().Credits < Item.CreditsCost)
		{
			CreditsError = true;
		}
		if (Session.GetHabbo().ActivityPoints < Item.PixelsCost)
		{
			PixelError = true;
		}
		if (CreditsError || PixelError)
		{
			Session.GetMessageHandler().GetResponse().Init(68u);
			Session.GetMessageHandler().GetResponse().AppendBoolean(CreditsError);
			Session.GetMessageHandler().GetResponse().AppendBoolean(PixelError);
			Session.GetMessageHandler().SendResponse();
			return;
		}
		if (IsGift && Item.GetBaseItem().Type.ToLower() == "e")
		{
			Session.SendNotif("You can not send this item as a gift.");
			return;
		}
		switch (Item.GetBaseItem().InteractionType.ToLower())
		{
		case "pet":
			try
			{
				string[] Bits = ExtraData.Split('\n');
				string PetName = Bits[0];
				string Race = Bits[1];
				string Color = Bits[2];
				int.Parse(Race);
				if (!CheckPetName(PetName) || Race.Length != 3 || Color.Length != 6)
				{
					return;
				}
				ExtraData = ExtraData + "\n" + ItemId;
			}
			catch (Exception)
			{
				return;
			}
			break;
		case "roomeffect":
		{
			double Number = 0.0;
			try
			{
				Number = double.Parse(ExtraData);
			}
			catch (Exception)
			{
			}
			ExtraData = Number.ToString().Replace(',', '.');
			break;
		}
		case "postit":
			ExtraData = "FFFF33";
			break;
		case "dimmer":
			ExtraData = "1,1,1,#000000,255";
			break;
		case "trophy":
			ExtraData = Session.GetHabbo().Username + Convert.ToChar(9) + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + Convert.ToChar(9) + HolographEnvironment.FilterInjectionChars(ExtraData, AllowLinebreaks: true);
			break;
		default:
			ExtraData = "";
			break;
		}
		if (Item.CreditsCost > 0)
		{
			Session.GetHabbo().Credits -= Item.CreditsCost;
			Session.GetHabbo().UpdateCreditsBalance(InDatabase: true);
		}
		if (Item.PixelsCost > 0)
		{
			Session.GetHabbo().ActivityPoints -= Item.PixelsCost;
			Session.GetHabbo().UpdateActivityPointsBalance(InDatabase: true);
		}
		Session.GetMessageHandler().GetResponse().Init(67u);
		Session.GetMessageHandler().GetResponse().AppendUInt(Item.GetBaseItem().ItemId);
		Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Item.GetBaseItem().Name);
		Session.GetMessageHandler().GetResponse().AppendInt32(Item.CreditsCost);
		Session.GetMessageHandler().GetResponse().AppendInt32(Item.PixelsCost);
		Session.GetMessageHandler().GetResponse().AppendInt32(1);
		Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Item.GetBaseItem().Type.ToLower());
		Session.GetMessageHandler().GetResponse().AppendInt32(Item.GetBaseItem().SpriteId);
		Session.GetMessageHandler().GetResponse().AppendStringWithBreak("");
		Session.GetMessageHandler().GetResponse().AppendInt32(1);
		Session.GetMessageHandler().GetResponse().AppendInt32(-1);
		Session.GetMessageHandler().GetResponse().AppendStringWithBreak("");
		Session.GetMessageHandler().SendResponse();
		if (IsGift)
		{
			uint GenId = GenerateItemId();
			Item Present = GeneratePresent();
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("gift_message", "!" + GiftMessage);
				dbClient.AddParamWithValue("extra_data", ExtraData);
				dbClient.ExecuteQuery("INSERT INTO user_items (id,user_id,base_item,extra_data) VALUES ('" + GenId + "','" + GiftUserId + "','" + Present.ItemId + "',@gift_message)");
				dbClient.ExecuteQuery("INSERT INTO user_presents (item_id,base_id,amount,extra_data) VALUES ('" + GenId + "','" + Item.GetBaseItem().ItemId + "','" + Item.Amount + "',@extra_data)");
			}
			GameClient Receiver = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(GiftUserId);
			if (Receiver != null)
			{
				Receiver.SendNotif("You have received a gift! Check your inventory.");
				Receiver.GetHabbo().GetInventoryComponent().UpdateItems(FromDatabase: true);
			}
			Session.SendNotif("Gift sent successfully!");
		}
		else
		{
			DeliverItems(Session, Item.GetBaseItem(), Item.Amount, ExtraData);
		}
	}

	public bool CheckPetName(string PetName)
	{
		if (PetName.Length < 1 || PetName.Length > 16)
		{
			return false;
		}
		if (!HolographEnvironment.IsValidAlphaNumeric(PetName))
		{
			return false;
		}
		return true;
	}

	public void DeliverItems(GameClient Session, Item Item, int Amount, string ExtraData)
	{
		switch (Item.Type.ToLower())
		{
		case "i":
		case "s":
		{
			for (int i = 0; i < Amount; i++)
			{
				uint GeneratedId = GenerateItemId();
				switch (Item.InteractionType.ToLower())
				{
				case "pet":
				{
					string[] PetData = ExtraData.Split('\n');
					int PetType = 0;
					switch (PetData[3])
					{
					case "2349":
						PetType = 5;
						break;
					case "2430":
						PetType = 3;
						break;
					case "2431":
						PetType = 4;
						break;
					case "2432":
						PetType = 1;
						break;
					case "2433":
						PetType = 0;
						break;
					case "2434":
						PetType = 2;
						break;
					case "7457":
						PetType = 6;
						break;
					case "5354":
						PetType = 7;
						break;
					default:
						PetType = 8;
						Session.SendNotif("Something went wrong! The item type could not be processed. Please do not try to buy this item anymore, instead inform support as soon as possible.");
						break;
					}
					if (PetType != 8)
					{
						Pet GeneratedPet = CreatePet(Session.GetHabbo().Id, PetData[0], PetType, PetData[1], PetData[2]);
						Session.GetHabbo().GetInventoryComponent().AddPet(GeneratedPet);
						Session.GetHabbo().GetInventoryComponent().AddItem(GeneratedId, 320u, "0");
					}
					else
					{
						HolographEnvironment.GetLogging().WriteLine("Pet Error: Someone just tried to buy ItemID: " + PetData[3] + " which is not a valid pet. (Catalog.cs)", LogLevel.Error);
					}
					break;
				}
				case "teleport":
				{
					uint TeleTwo = GenerateItemId();
					using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
					{
						dbClient.ExecuteQuery("INSERT INTO tele_links (tele_one_id,tele_two_id) VALUES ('" + GeneratedId + "','" + TeleTwo + "')");
						dbClient.ExecuteQuery("INSERT INTO tele_links (tele_one_id,tele_two_id) VALUES ('" + TeleTwo + "','" + GeneratedId + "')");
					}
					Session.GetHabbo().GetInventoryComponent().AddItem(TeleTwo, Item.ItemId, "0");
					Session.GetHabbo().GetInventoryComponent().AddItem(GeneratedId, Item.ItemId, "0");
					break;
				}
				case "darbadge_rare_trex":
				{
					using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
					{
						int TieneLaPlaca = dbClient.ReadInt32("SELECT COUNT( id ) FROM `user_items` WHERE base_item = '20208' AND user_id = '" + Session.GetHabbo().Id + "'");
						if (TieneLaPlaca < 1 && TieneLaPlaca == 0)
						{
							int Primerito_a = dbClient.ReadInt32("SELECT COUNT( id ) FROM `user_items` WHERE base_item = '20208'");
							int Primerito_b = dbClient.ReadInt32("SELECT COUNT( id ) FROM `room_items` WHERE base_item = '20208'");
							int Primerito = Primerito_a + Primerito_b;
							if (Primerito == 0)
							{
								Session.GetHabbo().GetBadgeComponent().GiveBadge("RA2", InDatabase: true);
							}
							else if (Primerito >= 1)
							{
								Session.GetHabbo().GetBadgeComponent().GiveBadge("RA1", InDatabase: true);
							}
						}
					}
					Session.GetHabbo().GetBadgeComponent().GiveBadge("RA2", InDatabase: true);
					break;
				}
				case "dimmer":
				{
					using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
					{
						dbClient.ExecuteQuery("INSERT INTO room_items_moodlight (item_id,enabled,current_preset,preset_one,preset_two,preset_three) VALUES ('" + GeneratedId + "','0','1','#000000,255,0','#000000,255,0','#000000,255,0')");
					}
					Session.GetHabbo().GetInventoryComponent().AddItem(GeneratedId, Item.ItemId, ExtraData);
					break;
				}
				default:
					Session.GetHabbo().GetInventoryComponent().AddItem(GeneratedId, Item.ItemId, ExtraData);
					break;
				}
			}
			Session.GetHabbo().GetInventoryComponent().UpdateItems(FromDatabase: true);
			break;
		}
		case "e":
		{
			for (int i = 0; i < Amount; i++)
			{
				Session.GetHabbo().GetAvatarEffectsInventoryComponent().AddEffect(Item.SpriteId, 3600);
			}
			break;
		}
		case "h":
		{
			for (int i = 0; i < Amount; i++)
			{
				Session.GetHabbo().GetSubscriptionManager().AddOrExtendSubscription("habbo_club", 2678400);
			}
			if (!Session.GetHabbo().GetBadgeComponent().HasBadge("HC1"))
			{
				Session.GetHabbo().GetBadgeComponent().GiveBadge("HC1", InDatabase: true);
			}
			Session.GetMessageHandler().GetResponse().Init(7u);
			Session.GetMessageHandler().GetResponse().AppendStringWithBreak("habbo_club");
			if (Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_club"))
			{
				double Expire = Session.GetHabbo().GetSubscriptionManager().GetSubscription("habbo_club")
					.ExpireTime;
				double TimeLeft = Expire - HolographEnvironment.GetUnixTimestamp();
				int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400.0);
				int MonthsLeft = TotalDaysLeft / 31;
				if (MonthsLeft >= 1)
				{
					MonthsLeft--;
				}
				Session.GetMessageHandler().GetResponse().AppendInt32(TotalDaysLeft - MonthsLeft * 31);
				Session.GetMessageHandler().GetResponse().AppendBoolean(Bool: true);
				Session.GetMessageHandler().GetResponse().AppendInt32(MonthsLeft);
			}
			else
			{
				for (int i = 0; i < 3; i++)
				{
					Session.GetMessageHandler().GetResponse().AppendInt32(0);
				}
			}
			Session.GetMessageHandler().SendResponse();
			List<string> Rights3 = HolographEnvironment.GetGame().GetRoleManager().GetRightsForHabbo(Session.GetHabbo());
			Session.GetMessageHandler().GetResponse().Init(2u);
			Session.GetMessageHandler().GetResponse().AppendInt32(Rights3.Count);
			foreach (string Right in Rights3)
			{
				Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Right);
			}
			Session.GetMessageHandler().SendResponse();
			break;
		}
		case "v":
		{
			for (int i = 0; i < Amount; i++)
			{
				Session.GetHabbo().GetSubscriptionManager().AddOrExtendSubscription("habbo_vip", 2678400);
			}
			if (!Session.GetHabbo().GetBadgeComponent().HasBadge("ACH_VipClub1"))
			{
				Session.GetHabbo().GetBadgeComponent().GiveBadge("ACH_VipClub1", InDatabase: true);
			}
			Session.GetMessageHandler().GetResponse().Init(7u);
			Session.GetMessageHandler().GetResponse().AppendStringWithBreak("habbo_vip");
			if (Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip"))
			{
				double Expire = Session.GetHabbo().GetSubscriptionManager().GetSubscription("habbo_vip")
					.ExpireTime;
				double TimeLeft = Expire - HolographEnvironment.GetUnixTimestamp();
				int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400.0);
				int MonthsLeft = TotalDaysLeft / 31;
				if (MonthsLeft >= 1)
				{
					MonthsLeft--;
				}
				Session.GetMessageHandler().GetResponse().AppendInt32(TotalDaysLeft - MonthsLeft * 31);
				Session.GetMessageHandler().GetResponse().AppendBoolean(Bool: true);
				Session.GetMessageHandler().GetResponse().AppendInt32(MonthsLeft);
			}
			else
			{
				for (int i = 0; i < 3; i++)
				{
					Session.GetMessageHandler().GetResponse().AppendInt32(0);
				}
			}
			Session.GetMessageHandler().SendResponse();
			List<string> Rights2 = HolographEnvironment.GetGame().GetRoleManager().GetRightsForHabbo(Session.GetHabbo());
			Session.GetMessageHandler().GetResponse().Init(2u);
			Session.GetMessageHandler().GetResponse().AppendInt32(Rights2.Count);
			foreach (string Right in Rights2)
			{
				Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Right);
			}
			Session.GetMessageHandler().SendResponse();
			break;
		}
		default:
			Session.SendNotif("Something went wrong! The item type could not be processed. Please do not try to buy this item anymore, instead inform support as soon as possible.");
			break;
		}
	}

	public Item GeneratePresent()
	{
		return HolographEnvironment.GetRandomNumber(0, 6) switch
		{
			1 => HolographEnvironment.GetGame().GetItemManager().GetItem(165u), 
			2 => HolographEnvironment.GetGame().GetItemManager().GetItem(166u), 
			3 => HolographEnvironment.GetGame().GetItemManager().GetItem(167u), 
			4 => HolographEnvironment.GetGame().GetItemManager().GetItem(168u), 
			5 => HolographEnvironment.GetGame().GetItemManager().GetItem(169u), 
			6 => HolographEnvironment.GetGame().GetItemManager().GetItem(170u), 
			_ => HolographEnvironment.GetGame().GetItemManager().GetItem(164u), 
		};
	}

	public Pet CreatePet(uint UserId, string Name, int Type, string Race, string Color)
	{
		DataRow Row = null;
		lock (HolographEnvironment.GetGame().GetCatalog().ItemGeneratorLock)
		{
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("userid", UserId);
				dbClient.AddParamWithValue("name", Name);
				dbClient.AddParamWithValue("type", Type);
				dbClient.AddParamWithValue("race", Race);
				dbClient.AddParamWithValue("color", Color);
				dbClient.AddParamWithValue("createstamp", HolographEnvironment.GetUnixTimestamp());
				dbClient.ReadDataRow("INSERT INTO user_pets (user_id,name,type,race,color,expirience,energy,createstamp) VALUES (@userid,@name,@type,@race,@color,0,100,@createstamp)");
			}

			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("userid", UserId);
				dbClient.AddParamWithValue("name", Name);
				Row = dbClient.ReadDataRow("SELECT * FROM user_pets WHERE user_id = @userid AND name = @name LIMIT 1");
			}
		}
		return GeneratePetFromRow(Row);
	}

	public Pet GeneratePetFromRow(DataRow Row)
	{
		if (Row == null)
		{
			return null;
		}
		return new Pet((uint)Row["id"], (uint)Row["user_id"], (uint)Row["room_id"], (string)Row["name"], (uint)Row["type"], (string)Row["race"], (string)Row["color"], (int)Row["expirience"], (int)Row["energy"], (int)Row["nutrition"], (int)Row["respect"], (double)Row["createstamp"], (int)Row["x"], (int)Row["y"], (double)Row["z"]);
	}

	public uint GenerateItemId()
	{
		lock (ItemGeneratorLock)
		{
			uint i = 0u;
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				i = (uint)dbClient.ReadDataRow("SELECT id_generator FROM item_id_generator LIMIT 1")[0];
				dbClient.ExecuteQuery("Update item_id_generator SET id_generator = id_generator + 1 LIMIT 1");
			}
			return i;
		}
	}

	public EcotronReward GetRandomEcotronReward()
	{
		uint Level = 1u;
		if (HolographEnvironment.GetRandomNumber(1, 2000) == 2000)
		{
			Level = 5u;
		}
		else if (HolographEnvironment.GetRandomNumber(1, 200) == 200)
		{
			Level = 4u;
		}
		else if (HolographEnvironment.GetRandomNumber(1, 40) == 40)
		{
			Level = 3u;
		}
		else if (HolographEnvironment.GetRandomNumber(1, 4) == 4)
		{
			Level = 2u;
		}
		List<EcotronReward> PossibleRewards = GetEcotronRewardsForLevel(Level);
		if (PossibleRewards != null && PossibleRewards.Count >= 1)
		{
			return PossibleRewards[HolographEnvironment.GetRandomNumber(0, PossibleRewards.Count - 1)];
		}
		return new EcotronReward(0u, 0u, 1479u, 0u);
	}

	public List<EcotronReward> GetEcotronRewardsForLevel(uint Level)
	{
		List<EcotronReward> Rewards = new List<EcotronReward>();
		lock (EcotronRewards)
		{
			foreach (EcotronReward R in EcotronRewards)
			{
				if (R.RewardLevel == Level)
				{
					Rewards.Add(R);
				}
			}
		}
		return Rewards;
	}

	public ServerMessage SerializeIndex(GameClient Client)
	{
		ServerMessage Index = new ServerMessage(126u);
		Index.AppendBoolean(Bool: false);
		Index.AppendInt32(0);
		Index.AppendInt32(0);
		Index.AppendInt32(-1);
		Index.AppendStringWithBreak("");
		Index.AppendBoolean(Bool: false);
		Index.AppendInt32(GetTreeSize(Client, -1));
		lock (Pages)
		{
			foreach (CatalogPage Page in Pages.Values)
			{
				if (Page.ParentId != -1)
				{
					continue;
				}
				Page.Serialize(Client, Index);
				foreach (CatalogPage _Page in Pages.Values)
				{
					if (_Page.ParentId == Page.PageId)
					{
						_Page.Serialize(Client, Index);
					}
				}
			}
		}
		return Index;
	}

	public ServerMessage SerializePage(CatalogPage Page)
	{
		ServerMessage PageData = new ServerMessage(127u);
		PageData.AppendInt32(Page.PageId);
		ServerMessage PageDataVip = new ServerMessage(270527613u);
		switch (Page.Layout)
		{
		case "frontpage":
			PageData.AppendStringWithBreak("frontpage3");
			PageData.AppendInt32(3);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendStringWithBreak(Page.LayoutTeaser);
			PageData.AppendStringWithBreak("");
			PageData.AppendInt32(11);
			PageData.AppendStringWithBreak(Page.Text1);
			PageData.AppendStringWithBreak("");
			PageData.AppendStringWithBreak(Page.Text2);
			PageData.AppendStringWithBreak(Page.TextDetails);
			PageData.AppendStringWithBreak("");
			PageData.AppendStringWithBreak("#FAF8CC");
			PageData.AppendStringWithBreak("#FAF8CC");
			PageData.AppendStringWithBreak("Otras maneras de conseguir créditos >>");
			PageData.AppendStringWithBreak("magic.credits");
			break;
		case "recycler_info":
			PageData.AppendStringWithBreak(Page.Layout);
			PageData.AppendInt32(2);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendStringWithBreak(Page.LayoutTeaser);
			PageData.AppendInt32(3);
			PageData.AppendStringWithBreak(Page.Text1);
			PageData.AppendStringWithBreak(Page.Text2);
			PageData.AppendStringWithBreak(Page.TextDetails);
			break;
		case "recycler_prizes":
			PageData.AppendStringWithBreak("recycler_prizes");
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak("catalog_recycler_headline3");
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak(Page.Text1);
			break;
		case "club_buy":
			PageData.AppendStringWithBreak("club_buy");
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak("hc2_clubtitle");
			PageData.AppendInt32(1);
			break;
		case "club_gifts":
			PageData.AppendStringWithBreak("club_gifts");
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak("hc2_clubtitle");
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak("Echa un vistazo a los regalos HC y VIP, si eres HC, podrías elegir entre una selección de  reglaos HC. Si eres VIP, entre una selección de regalso VIP.");
			PageData.AppendInt32(1);
			break;
		case "spaces":
			PageData.AppendStringWithBreak(Page.Layout);
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak(Page.Text1);
			break;
		case "recycler":
			PageData.AppendStringWithBreak(Page.Layout);
			PageData.AppendInt32(2);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendStringWithBreak(Page.LayoutTeaser);
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak(Page.Text1, 10);
			PageData.AppendStringWithBreak(Page.Text2);
			PageData.AppendStringWithBreak(Page.TextDetails);
			break;
		case "trophies":
			PageData.AppendStringWithBreak("trophies");
			PageData.AppendInt32(1);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendInt32(2);
			PageData.AppendStringWithBreak(Page.Text1);
			PageData.AppendStringWithBreak(Page.TextDetails);
			break;
		case "pets":
			PageData.AppendStringWithBreak("pets");
			PageData.AppendInt32(2);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendStringWithBreak(Page.LayoutTeaser);
			PageData.AppendInt32(4);
			PageData.AppendStringWithBreak(Page.Text1);
			PageData.AppendStringWithBreak("Dê um Nome:");
			PageData.AppendStringWithBreak("Escolha uma cor:");
			PageData.AppendStringWithBreak("Escolha uma raça:");
			break;
		default:
			PageData.AppendStringWithBreak(Page.Layout);
			PageData.AppendInt32(3);
			PageData.AppendStringWithBreak(Page.LayoutHeadline);
			PageData.AppendStringWithBreak(Page.LayoutTeaser);
			PageData.AppendStringWithBreak(Page.LayoutSpecial);
			PageData.AppendInt32(3);
			PageData.AppendStringWithBreak(Page.Text1);
			PageData.AppendStringWithBreak(Page.TextDetails);
			PageData.AppendStringWithBreak(Page.TextTeaser);
			break;
		}
		PageData.AppendInt32(Page.Items.Count);
		lock (Page.Items)
		{
			foreach (CatalogItem Item in Page.Items)
			{
				Item.Serialize(PageData);
			}
		}
		return PageData;
	}

	public ServerMessage ClubPage()
	{
		return new ServerMessage(625u);
	}

	public ServerMessage SerializeTestIndex()
	{
		ServerMessage Message = new ServerMessage(126u);
		Message.AppendInt32(0);
		Message.AppendInt32(0);
		Message.AppendInt32(0);
		Message.AppendInt32(-1);
		Message.AppendStringWithBreak("");
		Message.AppendInt32(0);
		Message.AppendInt32(100);
		for (int i = 1; i <= 150; i++)
		{
			Message.AppendInt32(1);
			Message.AppendInt32(i);
			Message.AppendInt32(i);
			Message.AppendInt32(i);
			Message.AppendStringWithBreak("#" + i);
			Message.AppendInt32(0);
			Message.AppendInt32(0);
		}
		return Message;
	}

	public VoucherHandler GetVoucherHandler()
	{
		return VoucherHandler;
	}

	public Marketplace GetMarketplace()
	{
		return Marketplace;
	}
}
