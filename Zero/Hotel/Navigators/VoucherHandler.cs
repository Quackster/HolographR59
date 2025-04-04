using System.Data;
using Zero.Hotel.GameClients;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Navigators;

internal class VoucherHandler
{
	public bool IsValidCode(string Code)
	{
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			if (dbClient.ReadDataRow("SELECT null FROM credit_vouchers WHERE code = '" + Code + "' LIMIT 1") != null)
			{
				return true;
			}
		}
		return false;
	}

	public int GetVoucherValue(string Code)
	{
		DataRow Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataRow("SELECT value FROM credit_vouchers WHERE code = '" + Code + "' LIMIT 1");
		}
		if (Data != null)
		{
			return (int)Data[0];
		}
		return 0;
	}

	public void TryRedeemVoucher(GameClient Session, string Code)
	{
		if (!IsValidCode(Code))
		{
			Session.SendMessage(new ServerMessage(213u));
		}
		int Value = GetVoucherValue(Code);
		if (Value >= 0)
		{
			Session.GetHabbo().Credits += Value;
			Session.GetHabbo().UpdateCreditsBalance(InDatabase: true);
		}
		Session.SendMessage(new ServerMessage(212u));
	}
}
