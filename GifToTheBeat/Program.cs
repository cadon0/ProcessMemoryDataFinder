using System;
using System.Linq;

namespace GifToTheBeat
{
    class Program
    {
        static void Main(string[] args)
        {
            var socketPort = 7270;
            if (args.Length > 0)
                socketPort = int.Parse(args[0]);
            var stateManager = new OsuStateWriter(socketPort);
            stateManager.GetState();
            System.Threading.Thread.Sleep(-1);
        }
    }
}
