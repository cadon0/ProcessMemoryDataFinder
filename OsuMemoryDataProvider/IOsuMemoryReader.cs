namespace OsuMemoryDataProvider
{
    public interface IOsuMemoryReader
    {
        IOsuMemoryReader GetInstanceForWindowTitleHint(string windowTitleHint);

        int GetPlayingMods();
        string GetOsuFileName();
        string GetMapFolderName();
        int ReadPlayTime();

        OsuMemoryStatus GetCurrentStatus(out int statusNumber);
    }
}