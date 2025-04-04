using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Zero.Storage;

namespace Zero.Hotel.Items;

internal class MoodlightData
{
	public bool Enabled;

	public int CurrentPreset;

	public List<MoodlightPreset> Presets;

	public uint ItemId;

	public MoodlightData(uint ItemId)
	{
		this.ItemId = ItemId;
		DataRow Row = null;
		using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
		{
			Row = dbClient.ReadDataRow("SELECT SQL_NO_CACHE enabled,current_preset,preset_one,preset_two,preset_three FROM room_items_moodlight WHERE item_id = '" + ItemId + "' LIMIT 1");
		}
		if (Row == null)
		{
			throw new ArgumentException();
		}
		Enabled = HolographEnvironment.EnumToBool(Row["enabled"].ToString());
		CurrentPreset = (int)Row["current_preset"];
		Presets = new List<MoodlightPreset>();
		Presets.Add(GeneratePreset((string)Row["preset_one"]));
		Presets.Add(GeneratePreset((string)Row["preset_two"]));
		Presets.Add(GeneratePreset((string)Row["preset_three"]));
	}

	public void Enable()
	{
		Enabled = true;
		using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
		dbClient.ExecuteQuery("Update room_items_moodlight SET enabled = '1' WHERE item_id = '" + ItemId + "' LIMIT 1");
	}

	public void Disable()
	{
		Enabled = false;
		using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
		dbClient.ExecuteQuery("Update room_items_moodlight SET enabled = '0' WHERE item_id = '" + ItemId + "' LIMIT 1");
	}

	public void UpdatePreset(int Preset, string Color, int Intensity, bool BgOnly)
	{
		if (IsValidColor(Color) && IsValidIntensity(Intensity))
		{
			string Pr = Preset switch
			{
				3 => "three", 
				2 => "two", 
				_ => "one", 
			};
			using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
			{
				dbClient.ExecuteQuery("Update room_items_moodlight SET preset_" + Pr + " = '" + Color + "," + Intensity + "," + HolographEnvironment.BoolToEnum(BgOnly) + "' WHERE item_id = '" + ItemId + "' LIMIT 1");
			}
			GetPreset(Preset).ColorCode = Color;
			GetPreset(Preset).ColorIntensity = Intensity;
			GetPreset(Preset).BackgroundOnly = BgOnly;
		}
	}

	public MoodlightPreset GeneratePreset(string Data)
	{
		string[] Bits = Data.Split(',');
		if (!IsValidColor(Bits[0]))
		{
			Bits[0] = "#000000";
		}
		return new MoodlightPreset(Bits[0], int.Parse(Bits[1]), HolographEnvironment.EnumToBool(Bits[2]));
	}

	public MoodlightPreset GetPreset(int i)
	{
		i--;
		if (Presets[i] != null)
		{
			return Presets[i];
		}
		return new MoodlightPreset("#000000", 255, BackgroundOnly: false);
	}

	public bool IsValidColor(string ColorCode)
	{
		switch (ColorCode)
		{
		case "#000000":
		case "#0053F7":
		case "#EA4532":
		case "#82F349":
		case "#74F5F5":
		case "#E759DE":
		case "#F2F851":
			return true;
		default:
			return false;
		}
	}

	public bool IsValidIntensity(int Intensity)
	{
		if (Intensity < 0 || Intensity > 255)
		{
			return false;
		}
		return true;
	}

	public string GenerateExtraData()
	{
		MoodlightPreset Preset = GetPreset(CurrentPreset);
		StringBuilder SB = new StringBuilder();
		if (Enabled)
		{
			SB.Append(2);
		}
		else
		{
			SB.Append(1);
		}
		SB.Append(",");
		SB.Append(CurrentPreset);
		SB.Append(",");
		if (Preset.BackgroundOnly)
		{
			SB.Append(2);
		}
		else
		{
			SB.Append(1);
		}
		SB.Append(",");
		SB.Append(Preset.ColorCode);
		SB.Append(",");
		SB.Append(Preset.ColorIntensity);
		return SB.ToString();
	}
}
