namespace Zero.Hotel.Roles;

internal class Role
{
    private uint Id;

    public string Caption;

    public uint RoleId => Id;

    public Role(uint Id, string Caption)
    {
        this.Id = Id;
        this.Caption = Caption;
    }
}
