﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace HydraX.Library
{
    public partial class BlackOps3
    {
        /// <summary>
        /// Black Ops 3 TTF Logic
        /// </summary>
        private class TTF : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// TTF Asset Structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct TTFAsset
            {
                #region ScriptParseTreeAssetProperties
                public long NamePointer { get; set; }
                public long Size { get; set; }
                public long DataPointer { get; set; }
                #endregion
            }
            #endregion

            /// <summary>
            /// Size of each asset
            /// </summary>
            public int AssetSize { get; set; }

            /// <summary>
            /// Gets or Sets the number of Assets 
            /// </summary>
            public int AssetCount { get; set; }

            /// <summary>
            /// Gets or Sets the Start Address
            /// </summary>
            public long StartAddress { get; set; }

            /// <summary>
            /// Gets or Sets the End Address
            /// </summary>
            public long EndAddress { get { return StartAddress + (AssetCount * AssetSize); } set => throw new NotImplementedException(); }

            /// <summary>
            /// Gets the Name of this Pool
            /// </summary>
            public string Name => "ttf";

            /// <summary>
            /// Gets the Setting Group for this Pool
            /// </summary>
            public string SettingGroup => "RawFile";

            /// <summary>
            /// Gets the Index of this Pool
            /// </summary>
            public int Index => (int)AssetPool.ttf;

            /// <summary>
            /// Loads Assets from this Asset Pool
            /// </summary>
            public List<GameAsset> Load(HydraInstance instance)
            {
                var results = new List<GameAsset>();

                var poolInfo = instance.Reader.ReadStruct<AssetPoolInfo>(instance.Game.BaseAddress + instance.Game.AssetPoolsAddresses[instance.Game.ProcessIndex] + (Index * 0x20));

                StartAddress = poolInfo.PoolPointer;
                AssetSize = poolInfo.AssetSize;
                AssetCount = poolInfo.PoolSize;

                for(int i = 0; i < AssetCount; i++)
                {
                    var header = instance.Reader.ReadStruct<TTFAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;

                    results.Add(new GameAsset()
                    {
                        Name = instance.Reader.ReadNullTerminatedString(header.NamePointer),
                        HeaderAddress = StartAddress + (i * AssetSize),
                        AssetPool = this,
                        Type = Name,
                        Information = string.Format("Size: 0x{0:X}", header.Size)
                    });
                }

                return results;
            }

            /// <summary>
            /// Exports the given asset from this pool
            /// </summary>
            public HydraStatus Export(GameAsset asset, HydraInstance instance)
            {
                var header = instance.Reader.ReadStruct<TTFAsset>(asset.HeaderAddress);

                if (asset.Name != instance.Reader.ReadNullTerminatedString(header.NamePointer))
                    return HydraStatus.MemoryChanged;

                string path = Path.Combine("exported_files", instance.Game.Name, asset.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                File.WriteAllBytes(path, instance.Reader.ReadBytes(header.DataPointer, (int)header.Size));

                return HydraStatus.Success;
            }

            /// <summary>
            /// Checks if the given asset is a null slot
            /// </summary>
            public bool IsNullAsset(GameAsset asset)
            {
                return IsNullAsset(asset.NameLocation);
            }

            /// <summary>
            /// Checks if the given asset is a null slot
            /// </summary>
            public bool IsNullAsset(long nameAddress)
            {
                return nameAddress >= StartAddress && nameAddress <= EndAddress || nameAddress == 0;
            }
        }
    }
}
