using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Pets;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Users.Inventory;

internal class InventoryComponent
{
	private List<UserItem> InventoryItems;

	private List<Pet> InventoryPets;

	public uint UserId;

	public int ItemCount => InventoryItems.Count;

	public int PetCount => InventoryPets.Count;

	public InventoryComponent(uint UserId)
	{
		this.UserId = UserId;
		InventoryItems = new List<UserItem>();
		InventoryPets = new List<Pet>();
	}

	public void ClearItems()
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("userid", UserId);
			dbClient.ExecuteQuery("DELETE FROM user_items WHERE user_id = @userid");
		}
		UpdateItems(FromDatabase: true);
	}

	public void ClearPets()
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("userid", UserId);
			dbClient.ExecuteQuery("DELETE FROM user_pets WHERE user_id = @userid AND room_id = 0");
		}
		UpdatePets(FromDatabase: true);
	}

	public void LoadInventory()
	{
		InventoryItems.Clear();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("userid", UserId);
			Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE id,base_item,extra_data FROM user_items WHERE user_id = @userid");
		}
		if (Data != null)
		{
			foreach (DataRow Row in Data.Rows)
			{
				InventoryItems.Add(new UserItem((uint)Row["id"], (uint)Row["base_item"], (string)Row["extra_data"]));
			}
		}
		InventoryPets.Clear();
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			dbClient.AddParamWithValue("userid", UserId);
			Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM user_pets WHERE user_id = @userid AND room_id <= 0");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			InventoryPets.Add(HolographEnvironment.GetGame().GetCatalog().GeneratePetFromRow(Row));
		}
	}

	public void UpdateItems(bool FromDatabase)
	{
		if (FromDatabase)
		{
			LoadInventory();
		}
		GetClient().GetMessageHandler().GetResponse().Init(101u);
		GetClient().GetMessageHandler().SendResponse();
	}

	public void UpdatePets(bool FromDatabase)
	{
		if (FromDatabase)
		{
			LoadInventory();
		}
		GetClient().SendMessage(SerializePetInventory());
	}

	public Pet GetPet(uint Id)
	{
		List<Pet>.Enumerator Pets = InventoryPets.GetEnumerator();
		while (Pets.MoveNext())
		{
			Pet Pet = Pets.Current;
			if (Pet.PetId == Id)
			{
				return Pet;
			}
		}
		return null;
	}

	public UserItem GetItem(uint Id)
	{
		List<UserItem>.Enumerator Items = InventoryItems.GetEnumerator();
		while (Items.MoveNext())
		{
			UserItem Item = Items.Current;
			if (Item.Id == Id)
			{
				return Item;
			}
		}
		return null;
	}

	public void AddItem(uint Id, uint BaseItem, string ExtraData)
	{
		InventoryItems.Add(new UserItem(Id, BaseItem, ExtraData));
		using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
		dbClient.AddParamWithValue("extra_data", ExtraData);
		dbClient.ExecuteQuery("INSERT INTO user_items (id,user_id,base_item,extra_data) VALUES ('" + Id + "','" + UserId + "','" + BaseItem + "',@extra_data)");
	}

	public void AddPet(Pet Pet)
	{
		if (Pet != null)
		{
			Pet.PlacedInRoom = false;
			InventoryPets.Add(Pet);
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("botid", Pet.PetId);
				dbClient.ExecuteQuery("Update user_pets SET room_id = 0, x = 0, y = 0, z = 0 WHERE id = @botid LIMIT 1");
			}
			ServerMessage AddMessage = new ServerMessage(603u);
			Pet.SerializeInventory(AddMessage);
			GetClient().SendMessage(AddMessage);
		}
	}

	public bool RemovePet(uint PetId)
	{
		foreach (Pet Pet in InventoryPets)
		{
			if (Pet.PetId != PetId)
			{
				continue;
			}
			InventoryPets.Remove(Pet);
			ServerMessage RemoveMessage = new ServerMessage(604u);
			RemoveMessage.AppendUInt(PetId);
			GetClient().SendMessage(RemoveMessage);
			return true;
		}
		return false;
	}

	public void MovePetToRoom(uint PetId, uint RoomId)
	{
		if (RemovePet(PetId))
		{
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.AddParamWithValue("roomid", RoomId);
				dbClient.AddParamWithValue("petid", PetId);
				dbClient.ExecuteQuery("Update user_pets SET room_id = @roomid, x = 0, y = 0, z = 0 WHERE id = @petid LIMIT 1");
			}
		}
	}

	public void RemoveItem(uint Id)
	{
		GetClient().GetMessageHandler().GetResponse().Init(99u);
		GetClient().GetMessageHandler().GetResponse().AppendUInt(Id);
		GetClient().GetMessageHandler().SendResponse();
		InventoryItems.Remove(GetItem(Id));
		using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
		dbClient.ExecuteQuery("DELETE FROM user_items WHERE id = '" + Id + "' LIMIT 1");
	}

	public ServerMessage SerializeItemInventory()
	{
		ServerMessage Message = new ServerMessage(140u);
		Message.AppendInt32(ItemCount);
		List<UserItem>.Enumerator eItems = InventoryItems.GetEnumerator();
		while (eItems.MoveNext())
		{
			eItems.Current.Serialize(Message, Inventory: true);
		}
		Message.AppendInt32(ItemCount);
		return Message;
	}

	public ServerMessage SerializePetInventory()
	{
		ServerMessage Message = new ServerMessage(600u);
		Message.AppendInt32(InventoryPets.Count);
		foreach (Pet Pet in InventoryPets)
		{
			Pet.SerializeInventory(Message);
		}
		return Message;
	}

	private GameClient GetClient()
	{
		return HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(UserId);
	}
}
