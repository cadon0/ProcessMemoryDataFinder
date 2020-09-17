using System;

namespace GifToTheBeatDataProvider
{
    [Flags]
    public enum Mods
    {
        DT = 1 << 6,
        HT = 1 << 8,
        NC = 1 << 9,
    }
}