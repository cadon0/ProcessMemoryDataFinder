namespace GifToTheBeat
{
    class Program
    {
        static void Main()
        {
            var stateManager = new OsuStateManager();
            stateManager.GetState();
            System.Threading.Thread.Sleep(-1);
        }
    }
}
