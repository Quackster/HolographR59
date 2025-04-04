using Zero.Hotel.Items;

namespace Zero.Hotel.Catalogs;

internal class EcotronReward
{
	public uint Id;

	public uint DisplayId;

	public uint BaseId;

	public uint RewardLevel;

	public EcotronReward(uint Id, uint DisplayId, uint BaseId, uint RewardLevel)
	{
		this.Id = Id;
		this.DisplayId = DisplayId;
		this.BaseId = BaseId;
		this.RewardLevel = RewardLevel;
	}

	public Item GetBaseItem()
	{
		return HolographEnvironment.GetGame().GetItemManager().GetItem(BaseId);
	}
}
