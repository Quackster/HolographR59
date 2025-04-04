using System;
using System.Collections.Generic;
using System.Data;
using Zero.Core;
using Zero.Storage;

namespace Zero.Hotel.Items;

internal class ItemManager
{
	private Dictionary<uint, Item> Items;

	public void LoadItems()
	{
		Items = new Dictionary<uint, Item>();
		DataTable ItemData = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			ItemData = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM furniture");
		}
		int i = 0;
		int j = 0;
		if (ItemData != null)
		{
			foreach (DataRow Row in ItemData.Rows)
			{
				try
				{
					Items.Add((uint)Row["id"], new Item((uint)Row["id"], (int)Row["sprite_id"], (string)Row["public_name"], (string)Row["item_name"], (string)Row["type"], (int)Row["width"], (int)Row["length"], (double)Row["stack_height"], HolographEnvironment.EnumToBool(Row["can_stack"].ToString()), HolographEnvironment.EnumToBool(Row["is_walkable"].ToString()), HolographEnvironment.EnumToBool(Row["can_sit"].ToString()), HolographEnvironment.EnumToBool(Row["allow_recycle"].ToString()), HolographEnvironment.EnumToBool(Row["allow_trade"].ToString()), HolographEnvironment.EnumToBool(Row["allow_marketplace_sell"].ToString()), HolographEnvironment.EnumToBool(Row["allow_gift"].ToString()), HolographEnvironment.EnumToBool(Row["allow_inventory_stack"].ToString()), (string)Row["interaction_type"], (int)Row["interaction_modes_count"], (string)Row["vending_ids"]));
					i++;
				}
				catch (Exception)
				{
					HolographEnvironment.GetLogging().WriteLine("Could not load item #" + (uint)Row["id"] + ", please verify the data is okay.", LogLevel.Error);
					j++;
				}
			}
		}
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[stringManager.Init] » Checking users table..");
        Console.WriteLine("[stringManager.Init] » Hotel starting in PT-BR language");
        Console.WriteLine("[stringManager.Init] » Welcome message disabled.");
        Console.WriteLine("");

        if (j > 0)
		{
			HolographEnvironment.GetLogging().WriteLine(j + " item defenition(s) could not be loaded.");
		}
	}

	public bool ContainsItem(uint Id)
	{
		return Items.ContainsKey(Id);
	}

	public Item GetItem(uint Id)
	{
		if (ContainsItem(Id))
		{
			return Items[Id];
		}
		return null;
	}
}
