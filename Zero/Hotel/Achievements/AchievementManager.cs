using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Hotel.Users.Badges;
using Zero.Messages;
using Zero.Storage;
using System.Collections.Concurrent;

namespace Zero.Hotel.Achievements;

internal class AchievementManager
{
    public ConcurrentDictionary<uint, Achievement> Achievements;

    public AchievementManager()
    {
        Achievements = new ConcurrentDictionary<uint, Achievement>();
    }

    public void LoadAchievements()
    {
        Achievements.Clear();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT * FROM achievements");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            Achievements.TryAdd((uint)Row["id"], new Achievement((uint)Row["id"], (int)Row["levels"], (string)Row["badge"], (int)Row["pixels_base"], (double)Row["pixels_multiplier"], HolographEnvironment.EnumToBool(Row["dynamic_badgelevel"].ToString())));
        }
    }

    public bool UserHasAchievement(GameClient Session, uint Id, int MinLevel)
    {
        if (!Session.GetHabbo().Achievements.ContainsKey(Id))
        {
            return false;
        }
        if (Session.GetHabbo().Achievements[Id] >= MinLevel)
        {
            return true;
        }
        return false;
    }
    public ServerMessage SerializeAchievementList(GameClient Session)
    {
        List<Achievement> AchievementsToList = new List<Achievement>();
        ConcurrentDictionary<uint, int> NextAchievementLevels = new ConcurrentDictionary<uint, int>();

        foreach (Achievement Achievement in Achievements.Values)
        {
            if (!Session.GetHabbo().Achievements.ContainsKey(Achievement.Id))
            {
                AchievementsToList.Add(Achievement);
                NextAchievementLevels.TryAdd(Achievement.Id, 1);
            }
            else
            {
                if (Session.GetHabbo().Achievements[Achievement.Id] >= Achievement.Levels)
                {
                    continue;
                }

                AchievementsToList.Add(Achievement);
                NextAchievementLevels.TryAdd(Achievement.Id, Session.GetHabbo().Achievements[Achievement.Id] + 1);
            }
        }
        ServerMessage Message = new ServerMessage(436u);
        Message.AppendInt32(AchievementsToList.Count);
        foreach (Achievement Achievement in AchievementsToList)
        {
            int Level = NextAchievementLevels[Achievement.Id];
            Message.AppendUInt(Achievement.Id);
            Message.AppendInt32(Level);
            Message.AppendStringWithBreak(FormatBadgeCode(Achievement.BadgeCode, Level, Achievement.DynamicBadgeLevel));
        }
        return Message;
    }

    public void UnlockAchievement(GameClient Session, uint AchievementId, int Level)
    {
        // Get the achievement
        Achievement Achievement = Achievements[AchievementId];

        // Make sure the achievement is valid and has not already been unlocked
        if (Achievement == null || UserHasAchievement(Session, Achievement.Id, Level) || Level < 1 || Level > Achievement.Levels)
        {
            return;
        }

        // Calculate the pixel value for this achievement
        int Value = CalculateAchievementValue(Achievement.PixelBase, Achievement.PixelMultiplier, Level);

        // Remove any previous badges for this achievement (old levels)
        List<string> BadgesToRemove = new List<string>();

        foreach (Badge Badge in Session.GetHabbo().GetBadgeComponent().BadgeList)
        {
            if (Badge.Code.StartsWith(Achievement.BadgeCode))
            {
                BadgesToRemove.Add(Badge.Code);
            }
        }

        foreach (string Badge in BadgesToRemove)
        {
            Session.GetHabbo().GetBadgeComponent().RemoveBadge(Badge);
        }

        // Give the user the new badge
        Session.GetHabbo().GetBadgeComponent().GiveBadge(FormatBadgeCode(Achievement.BadgeCode, Level, Achievement.DynamicBadgeLevel), true);

        // Update or set the achievement level for the user
        if (Session.GetHabbo().Achievements.ContainsKey(Achievement.Id))
        {
            Session.GetHabbo().Achievements[Achievement.Id] = Level;

            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("UPDATE user_achievements SET achievement_level = '" + Level + "' WHERE user_id = '" + Session.GetHabbo().Id + "' AND achievement_id = '" + Achievement.Id + "' LIMIT 1");
            }
        }
        else
        {
            Session.GetHabbo().Achievements.TryAdd(Achievement.Id, Level);

            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("INSERT INTO user_achievements (user_id,achievement_id,achievement_level) VALUES ('" + Session.GetHabbo().Id + "','" + Achievement.Id + "','" + Level + "')");
            }
        }
        Session.GetMessageHandler().GetResponse().Init(437u);
        Session.GetMessageHandler().GetResponse().AppendUInt(Achievement.Id);
        Session.GetMessageHandler().GetResponse().AppendInt32(Level);
        Session.GetMessageHandler().GetResponse().AppendStringWithBreak(FormatBadgeCode(Achievement.BadgeCode, Level, Achievement.DynamicBadgeLevel));
        if (Level > 1)
        {
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak(FormatBadgeCode(Achievement.BadgeCode, Level - 1, Achievement.DynamicBadgeLevel));
        }
        else
        {
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak("");
        }
        Session.GetMessageHandler().SendResponse();
        Session.GetHabbo().ActivityPoints += Value;
        Session.GetHabbo().UpdateActivityPointsBalance(InDatabase: true, Value);
    }

    public int CalculateAchievementValue(int BaseValue, double Multiplier, int Level)
    {
        return BaseValue + 50 * Level;
    }

    public string FormatBadgeCode(string BadgeTemplate, int Level, bool Dyn)
    {
        if (!Dyn)
        {
            return BadgeTemplate;
        }
        return BadgeTemplate + Level;
    }
}
