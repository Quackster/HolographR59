namespace Zero.Hotel.Achievements;

internal class Achievement
{
    public uint Id;

    public int Levels;

    public string BadgeCode;

    public int PixelBase;

    public double PixelMultiplier;

    public bool DynamicBadgeLevel;

    public Achievement(uint Id, int Levels, string BadgeCode, int PixelBase, double PixelMultiplier, bool DynamicBadgeLevel)
    {
        this.Id = Id;
        this.Levels = Levels;
        this.BadgeCode = BadgeCode;
        this.PixelBase = PixelBase;
        this.PixelMultiplier = PixelMultiplier;
        this.DynamicBadgeLevel = DynamicBadgeLevel;
    }
}
