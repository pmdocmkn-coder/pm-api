namespace Pm.Enums
{
    public enum InternalLinkServiceType
    {
        Internet = 0,
        AudioCodesVoip = 1,
        LocalLoop = 2,
        CCTV = 3,
        LinkInternal = 4
    }

    public enum InternalLinkStatus
    {
        Active = 0,
        Dismantled = 1,
        Removed = 2,
        Obstacle = 3
    }

    public enum InternalLinkDirection
    {
        None = 0,
        TX = 1,
        RX = 2
    }
}
