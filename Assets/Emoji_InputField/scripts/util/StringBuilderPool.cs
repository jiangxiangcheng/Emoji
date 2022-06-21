using System.Text;

public static class StringBuilderPool
{
    static readonly ObjectPool<StringBuilder> s_strBuilder = new ObjectPool<StringBuilder>(null, s => s.Length = 0);

    public static StringBuilder Get(int length = 0)
    {
        StringBuilder sb = s_strBuilder.Get();
        if (sb.Capacity < length)
            sb.Capacity = length;
        return sb;
    }

    public static void Release(StringBuilder toRelease)
    {
        s_strBuilder.Release(toRelease);
    }
}
