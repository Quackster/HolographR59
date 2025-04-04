using System.Data;
using Zero.Storage;

namespace Zero.Hotel.Items;

internal class TeleHandler
{
	public static uint GetLinkedTele(uint TeleId)
	{
		DataRow Row = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Row = dbClient.ReadDataRow("SELECT tele_two_id FROM tele_links WHERE tele_one_id = '" + TeleId + "' LIMIT 1");
		}
		if (Row == null)
		{
			return 0u;
		}
		return (uint)Row[0];
	}

	public static uint GetTeleRoomId(uint TeleId)
	{
		DataRow Row = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Row = dbClient.ReadDataRow("SELECT room_id FROM room_items WHERE id = '" + TeleId + "' LIMIT 1");
		}
		if (Row == null)
		{
			return 0u;
		}
		return (uint)Row[0];
	}

	public static bool IsTeleLinked(uint TeleId)
	{
		uint LinkId = GetLinkedTele(TeleId);
		if (LinkId == 0)
		{
			return false;
		}
		if (GetTeleRoomId(LinkId) == 0)
		{
			return false;
		}
		return true;
	}
}
