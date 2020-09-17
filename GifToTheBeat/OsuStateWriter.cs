using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OsuMemoryDataProvider;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace GifToTheBeat
{
    /// <summary>
    /// Writes osu! state to a Web Socket
    /// </summary>
    public partial class OsuStateWriter
    {
        private readonly string _webSocketAddress = "127.0.0.1";
        private WebSocketServer _webSocketServer;
        private DataContainer _dataContainer = new DataContainer();

        private readonly int _readDelay = 100;
        private readonly IOsuMemoryReader _reader;

        public OsuStateWriter(int socketPort)
        {
            _reader = OsuMemoryReader.Instance.GetInstanceForWindowTitleHint("osu!");
            _webSocketServer = new WebSocketServer(IPAddress.Parse(_webSocketAddress), socketPort);
            _webSocketServer.ReuseAddress = true;
            _webSocketServer.WaitTime = TimeSpan.FromSeconds(30);
            _webSocketServer.AddWebSocketService("/GifToTheBeatOsuDataFeed", () => new DataSender(_dataContainer));
            _webSocketServer.Start();
        }

        public void GetState()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var mapFolderName = string.Empty;
                        var osuFileName = string.Empty;
                        var status = OsuMemoryStatus.Unknown;
                        var playTime = -1;

                        mapFolderName = _reader.GetMapFolderName();
                        osuFileName = _reader.GetOsuFileName();
                        status = _reader.GetCurrentStatus();
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
                        _dataContainer.Data = JsonConvert.SerializeObject(new
                        {
                            status = $"{status}",
                            mapTime = playTime,
                            isoTime,
                            bpmMultiplier,
                            // Relative to song directory
                            relativeOsuFilePath = !string.IsNullOrEmpty(osuFileName) ? $"{mapFolderName}{sep}{osuFileName}" : null
                        });

                        await Task.Delay(_readDelay);
                    }
                }
                catch (ThreadAbortException)
                {
                }
            });
        }

        internal class DataContainer
        {
            public volatile string Data;
        }

        internal class DataSender : WebSocketBehavior
        {
            protected readonly DataContainer DataContainer;
            public DataSender(DataContainer dataContainer)
            {
                DataContainer = dataContainer;
                Task.Run(SendLoop);
            }

            public async Task SendLoop()
            {
                string lastSentData = string.Empty;
                while (true)
                {
                    if (lastSentData != DataContainer.Data)
                    {
                        lastSentData = DataContainer.Data;
                        await Send(lastSentData);
                    }
                    if (State == WebSocketState.Closed)
                    {
                        return;
                    }
                    await Task.Delay(250);
                }
            }
        }
    }
}