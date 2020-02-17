﻿using System;
using System.Collections.Generic;
using System.Text;
using fNbt;
using McFileIo.Blocks;
using McFileIo.Interfaces;
using McFileIo.Utility;

namespace McFileIo.World
{
    /// <summary>
    /// Stores BlockId-based Anvil chunk (pre 1.13)
    /// </summary>
    public sealed class ClassicChunk : Chunk
    {
        private const string FieldY = "Y";
        private const string FieldBlocks = "Blocks";
        private const string FieldAdd = "Add";
        private const string FieldData = "Data";

        private readonly byte[][] _blocks = new byte[16][];
        private readonly byte[][] _data = new byte[16][];
        private readonly byte[][] _add = new byte[16][];

        internal ClassicChunk()
        {
        }

        protected override bool GetBlockData(NbtCompound section)
        {
            if (!section.TryGet(FieldY, out NbtByte y)) return false;
            if (y.Value == 255) return true;

            if (y.Value >= 16) return false;

            // Old format with numeric block ID
            if (!section.TryGet(FieldBlocks, out NbtByteArray blocks)) return false;
            if (blocks.Value.Length != 4096) return false;
            _blocks[y.Value] = blocks.Value;

            var addSuccess = section.TryGet(FieldAdd, out NbtByteArray add);
            if (addSuccess && add.Value.Length != 2048) return false;
            if (addSuccess) _add[y.Value] = add.Value;

            var dataSuccess = section.TryGet(FieldData, out NbtByteArray data);
            if (dataSuccess && data.Value.Length != 2048) return false;
            if (dataSuccess) _data[y.Value] = data.Value;

            return true;
        }

        /// <summary>
        /// Get all blocks' world coordinates, block Id and block data.
        /// The blocks may not be ordered to maximize performance.
        /// </summary>
        /// <returns>Blocks</returns>
        public IEnumerable<(int X, int Y, int Z, ClassicBlock Block)> AllBlocks()
        {
            for (var sy = 0; sy < 16; sy++)
            {
                var data = _data[sy];
                var add = _add[sy];
                var blocks = _blocks[sy];

                if (blocks == null)
                    continue;

                var dataAvailable = data != null;
                var addAvailable = add != null;
                var baseY = sy << 4;
                var index = 0;
                int blockId, blockData;

                for (var y = 0; y < 16; y++)
                    for (var z = 0; z < 16; z++)
                    {
                        for (var x = 0; x < 16; x += 2)
                        {
                            blockId = addAvailable ? ((EndianHelper.GetHalfIntEvenIndex(add, index) << 8) | blocks[index]) : blocks[index];
                            blockData = dataAvailable ? EndianHelper.GetHalfIntEvenIndex(data, index) : 0;
                            yield return (x, y + baseY, z, new ClassicBlock { Data = blockData, Id = blockId });

                            index += 2;
                        }

                        index -= 15;

                        for (var x = 1; x < 16; x += 2)
                        {
                            blockId = addAvailable ? ((EndianHelper.GetHalfIntOddIndex(add, index) << 8) | blocks[index]) : blocks[index];
                            blockData = dataAvailable ? EndianHelper.GetHalfIntOddIndex(data, index) : 0;
                            yield return (x, y + baseY, z, new ClassicBlock { Data = blockData, Id = blockId });

                            index += 2;
                        }

                        index -= 1;
                    }
            }
        }

        /// <summary>
        /// Get block's Id and data at a given coordinate
        /// </summary>
        /// <param name="x">World X</param>
        /// <param name="y">World Y</param>
        /// <param name="z">World Z</param>
        /// <returns>Block</returns>
        public ClassicBlock GetBlock(int x, int y, int z)
        {
            var sec = y >> 4;
            var data = 0;
            var blocks = _blocks[sec];
            if (blocks == null) return ClassicBlock.AirBlock;

            var index = GetBlockIndexByCoord(x, y, z);
            var blockId = (int)blocks[index];
            if (_add[sec] != null)
                blockId += (EndianHelper.GetHalfInt(_add[sec], index) << 8);
            if (_data[sec] != null)
                data = EndianHelper.GetHalfInt(_data[sec], index);

            return new ClassicBlock
            {
                Id = blockId,
                Data = data
            };
        }

        public override IEnumerable<int> GetExistingYs()
        {
            for (var sy = 0; sy < 16; sy++)
            {
                if (_blocks[sy] != null)
                    yield return sy;
            }
        }
    }
}