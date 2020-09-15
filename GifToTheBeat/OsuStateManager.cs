using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OsuMemoryDataProvider;

namespace GifToTheBeat
{
    public partial class OsuStateManager
    {
        private int _readDelay = 500;
        private readonly IOsuMemoryReader _reader;
        private CancellationTokenSource cts = new CancellationTokenSource();
        public OsuStateManager()
        {
            _reader = OsuMemoryReader.Instance.GetInstanceForWindowTitleHint("osu!");
        }

        public void GetState()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var dir = "";
                    while (true)
                    {
                        if (dir == "")
                        {
                            var _processes = Process.GetProcessesByName("osu!");
                            if (_processes.Length > 0)
                            {
                                var osuExePath = _processes[0].Modules[0].FileName;
                                dir = osuExePath.Remove(osuExePath.LastIndexOf('\\'));
                            }
                        }

                        if (cts.IsCancellationRequested)
                            return;

                        var mapFolderName = string.Empty;
                        var osuFileName = string.Empty;
                        var status = OsuMemoryStatus.Unknown;
                        var statusNum = -1;
                        var playTime = -1;

                        mapFolderName = _reader.GetMapFolderName();
                        osuFileName = _reader.GetOsuFileName();
                        status = _reader.GetCurrentStatus(out statusNum);
                        playTime = _reader.ReadPlayTime();
                        var isoTime = DateTimeOffset.Now.ToString("o");

                        var playingMods = -1;
                        playingMods = _reader.GetPlayingMods();

                        var bpmMultiplier = 1.0;
                        if (playingMods != -1)
                        {
                            // The DT bit is set for both DT and NC, and gif-to-the-beat only cares about multiplier
                            int dtBit = 6;
                            int htBit = 8;
                            bool dtBitIsSet = (playingMods & (1 << dtBit)) != 0;
                            bool htBitIsSet = (playingMods & (1 << htBit)) != 0;
                            if (dtBitIsSet) bpmMultiplier = 1.5;
                            else if (htBitIsSet) bpmMultiplier = 0.75;
                        }

                        var sep = Path.DirectorySeparatorChar;
                        var output = JsonConvert.SerializeObject(new
                        {
                            status = $"{status}",
                            mapTime = playTime,
                            isoTime,
                            bpmMultiplier,
                            osuFile =  $"{dir}{sep}Songs{sep}{mapFolderName}{sep}{osuFileName}"
                        });
                        Console.WriteLine(output);

                        await Task.Delay(_readDelay);
                    }
                }
                catch (ThreadAbortException)
                {
                }
            });
        }
    }
}