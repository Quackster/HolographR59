using System.Collections.Generic;
using System.Data;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Users.Badges;

internal class BadgeComponent
{
	private List<Badge> Badges;

	private uint UserId;

	public int Count => Badges.Count;

	public int EquippedCount
	{
		get
		{
			int i = 0;
			lock (Badges)
			{
				foreach (Badge Badge in Badges)
				{
					if (Badge.Slot > 0)
					{
						i++;
					}
				}
			}
			return i;
		}
	}

	public List<Badge> BadgeList => Badges;

	public BadgeComponent(uint UserId)
	{
		Badges = new List<Badge>();
		this.UserId = UserId;
	}

	public Badge GetBadge(string Badge)
	{
		lock (Badges)
		{
			foreach (Badge B in Badges)
			{
				if (Badge.ToLower() == B.Code.ToLower())
				{
					return B;
				}
			}
		}
		return null;
	}

	public bool HasBadge(string Badge)
	{
		if (GetBadge(Badge) != null)
		{
			return true;
		}
		return false;
	}

	public void GiveBadge(string Badge, bool InDatabase)
	{
		GiveBadge(Badge, 0, InDatabase);
	}

	public void GiveBadge(string Badge, int Slot, bool InDatabase)
	{
		if (HasBadge(Badge))
		{
			return;
		}
		if (InDatabase)
		{
			using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
			dbClient.ExecuteQuery("INSERT INTO user_badges (user_id,badge_id,badge_slot) VALUES ('" + UserId + "','" + Badge + "','" + Slot + "')");
		}
		Badges.Add(new Badge(Badge, Slot));
	}

	public void SetBadgeSlot(string Badge, int Slot)
	{
		Badge B = GetBadge(Badge);
		if (B != null)
		{
			B.Slot = Slot;
		}
	}

	public void ResetSlots()
	{
		lock (Badges)
		{
			foreach (Badge Badge in Badges)
			{
				Badge.Slot = 0;
			}
		}
	}

	public void RemoveBadge(string Badge)
	{
		if (HasBadge(Badge))
		{
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.ExecuteQuery("DELETE FROM user_badges WHERE badge_id = '" + Badge + "' AND user_id = '" + UserId + "' LIMIT 1");
			}
			Badges.Remove(GetBadge(Badge));
		}
	}

	public void LoadBadges()
	{
		Badges.Clear();
		DataTable Data = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Data = dbClient.ReadDataTable("SELECT badge_id,badge_slot FROM user_badges WHERE user_id = '" + UserId + "'");
		}
		if (Data == null)
		{
			return;
		}
		foreach (DataRow Row in Data.Rows)
		{
			GiveBadge((string)Row["badge_id"], (int)Row["badge_slot"], InDatabase: false);
		}
	}

	public ServerMessage Serialize()
	{
		List<Badge> EquippedBadges = new List<Badge>();
		ServerMessage Message = new ServerMessage(229u);
		Message.AppendInt32(Count);
		lock (Badges)
		{
			foreach (Badge Badge in Badges)
			{
				Message.AppendStringWithBreak(Badge.Code);
				if (Badge.Slot > 0)
				{
					EquippedBadges.Add(Badge);
				}
			}
		}
		Message.AppendInt32(EquippedBadges.Count);
		foreach (Badge Badge in EquippedBadges)
		{
			Message.AppendInt32(Badge.Slot);
			Message.AppendStringWithBreak(Badge.Code);
		}
		return Message;
	}
}
