using System.Linq;

public static class SeedCreator
{
    public static int CreateFromString(string seed)
    {
        return seed.GetHashCode();        
    }
}