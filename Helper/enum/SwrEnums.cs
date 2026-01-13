namespace Pm.Enums
{
    public enum SwrSiteType
    {
        Trunking,      // Has FPWR and VSWR
        Conventional   // Only has VSWR
    }

    public enum SwrOperationalStatus
    {
        Active ,
        Dismantled,
        Removed,
        Obstacle
    }
}