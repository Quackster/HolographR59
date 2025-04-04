namespace Zero.Hotel.Support;

internal class HelpCategory
{
    private uint Id;

    public string Caption;

    public uint CategoryId => Id;

    public HelpCategory(uint Id, string Caption)
    {
        this.Id = Id;
        this.Caption = Caption;
    }
}
