using System;
using System.Collections.Concurrent;
using ProcessMemoryDataFinder.API;

namespace OsuMemoryDataProvider
{
    public class OsuMemoryReader : MemoryReaderEx, IOsuMemoryReader
    {
        protected readonly object _lockingObject = new object();

        /// <summary>
        ///     It is strongly encouraged to use single <see cref="OsuMemoryReader" /> instance in order to not have to duplicate
        ///     find-signature-location work
        /// </summary>
        public static IOsuMemoryReader Instance { get; } = new OsuMemoryReader();

        private static readonly ConcurrentDictionary<string, IOsuMemoryReader> Instances =
            new ConcurrentDictionary<string, IOsuMemoryReader>();

        public IOsuMemoryReader GetInstanceForWindowTitleHint(string windowTitleHint)
        {
            if (string.IsNullOrEmpty(windowTitleHint)) return Instance;
            return Instances.GetOrAdd(windowTitleHint, s => new OsuMemoryReader(s));
        }

        public OsuMemoryReader(string mainWindowTitleHint = null) : base("osu!", mainWindowTitleHint)
        {
            CreateSignatures();
        }

        internal void CreateSignatures()
        {
            Signatures.Add((int)SignatureNames.OsuBase, new SigEx
            {
                Name = "OsuBase",
                Pattern = UnpackStr("F80174048365"),
                UseMask = false
            });

            CreateBeatmapDataSignatures();

            Signatures.Add((int)SignatureNames.OsuStatus, new SigEx
            {
                Name = "OsuStatus",
                ParentSig = Signatures[(int)SignatureNames.OsuBase],
                Offset = -60,
                PointerOffsets = { 0 }
            });
            Signatures.Add((int)SignatureNames.PlayTime, new SigEx
            {
                Name = "PlayTime",
                ParentSig = Signatures[(int)SignatureNames.OsuBase],
                Offset = 100,
                PointerOffsets = { -16 }
            });
            Signatures[(int)SignatureNames.Mods] = new SigEx
            {
                Name = "mods",
                Pattern = UnpackStr("810D0000000000080000"),
                Mask = "xx????xxxx",
                Offset = 2,
                PointerOffsets = { 0 },
                UseMask = true,
            };

            CreatePlaySignatures();
        }

        private void CreateBeatmapDataSignatures()
        {
            Signatures.Add((int)SignatureNames.CurrentBeatmapData, new SigEx
            {
                Name = "CurrentBeatmapData",
                ParentSig = Signatures[(int)SignatureNames.OsuBase],
                Offset = -12,
                PointerOffsets = { 0 },
                UseMask = false
            });

            Signatures.Add((int)SignatureNames.MapFolderName, new SigEx
            {
                // string
                ParentSig = Signatures[(int)SignatureNames.CurrentBeatmapData],
                PointerOffsets = { 116 }
            });

            Signatures.Add((int)SignatureNames.MapOsuFileName, new SigEx
            {
                // string
                ParentSig = Signatures[(int)SignatureNames.CurrentBeatmapData],
                PointerOffsets = { 140 }
            });
        }

        private void CreatePlaySignatures()
        {
            Signatures.Add((int)SignatureNames.PlayContainer, new SigEx
            {
                // Available only when playing;
                // need to reset on each play
                Name = "PlayContainer",
                Pattern = UnpackStr("85C9741F8D55F08B01"),
                Offset = -4,
                PointerOffsets = { 0 },
                UseMask = false
            });

            Signatures.Add((int)SignatureNames.PlayingMods, new SigEx
            {
                // Complex - 2 xored ints
                ParentSig = Signatures[(int)SignatureNames.PlayContainer],
                PointerOffsets = { 56, 28 }
            });
        }

        public int GetPlayingMods()
        {
            lock (_lockingObject)
            {
                ResetPointer((int)SignatureNames.PlayingMods);
                var pointer = GetPointer((int)SignatureNames.PlayingMods);
                var data1 = ReadData(pointer + 8, 4);
                var data2 = ReadData(pointer + 12, 4);

                if (data1 != null && data2 != null)
                {
                    var num1 = BitConverter.ToInt32(data1, 0);
                    var num2 = BitConverter.ToInt32(data2, 0);
                    return num1 ^ num2;
                }

                return -1;
            }
        }

        public string GetOsuFileName()
        {
            return GetString((int)SignatureNames.MapOsuFileName);
        }

        public string GetMapFolderName()
        {
            return GetString((int)SignatureNames.MapFolderName);
        }

        public int ReadPlayTime()
        {
            return GetInt((int)SignatureNames.PlayTime);
        }

        /// <summary>
        /// Gets the current osu! status.
        /// </summary>
        /// <param name="statusNumber">Use this number whenever <see cref="OsuMemoryStatus.Unknown"/> is returned</param>
        /// <returns></returns>
        public OsuMemoryStatus GetCurrentStatus(out int statusNumber)
        {
            int num;
            lock (_lockingObject)
            {
                num = GetInt((int)SignatureNames.OsuStatus);
            }

            statusNumber = num;
            if (Enum.IsDefined(typeof(OsuMemoryStatus), num))
            {
                return (OsuMemoryStatus)num;
            }

            return OsuMemoryStatus.Unknown;
        }


        protected override int GetInt(int signatureId)
        {
            lock (_lockingObject)
            {
                ResetPointer(signatureId);
                return base.GetInt(signatureId);
            }
        }

        protected override string GetString(int signatureId)
        {
            lock (_lockingObject)
            {
                ResetPointer(signatureId);
                return base.GetString(signatureId);
            }
        }
    }
}
