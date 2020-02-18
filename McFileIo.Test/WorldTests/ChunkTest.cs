﻿using McFileIo.Blocks;
using McFileIo.World;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace McFileIo.Test.WorldTests
{
    public class ChunkTest
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void ClassicChunkGetSetTest()
        {
            var chunk = new ClassicChunk();
            var blocks = new (int X, int Y, int Z, ClassicBlock Block)[] {
                (15, 255, -37, new ClassicBlock(4095, 0)),
                (13, 60, 0, new ClassicBlock(32, 14))
            };

            foreach(var it in blocks)
            {
                chunk.SetBlock(it.X, it.Y, it.Z, it.Block);
            }

            foreach (var it in blocks)
            {
                var block = chunk.GetBlock(it.X, it.Y, it.Z);
                Assert.AreEqual(block, it.Block);
            }

            Assert.AreEqual(chunk.GetExistingYs().OrderBy(t => t).ToArray(),
                new int[] { 60 >> 4, 255 >> 4 });

            var isAirBlock = typeof(ClassicChunk).GetMethod("IsAirBlock", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsTrue((bool)isAirBlock.Invoke(chunk, new object[] { 8, 8, 8 }));
            Assert.IsFalse((bool)isAirBlock.Invoke(chunk, new object[] { 13, 60, 0 }));
        }

        [Test]
        public void ClassicChunkHeightMapTest()
        {
            var chunk = new ClassicChunk();
            var blocks = new (int X, int Y, int Z, ClassicBlock Block)[] {
                (15, 255, 13, new ClassicBlock(4095, 0)),
                (7, 60, 0, new ClassicBlock(32, 14))
            };

            foreach (var it in blocks)
            {
                chunk.SetBlock(it.X, it.Y, it.Z, it.Block);
            }

            var map = chunk.HeightMap;
            map.Calculate(chunk);

            Assert.AreEqual(map.State, HeightMap.StorageType.Pre113);
            Assert.Throws<NotSupportedException>(() => map.GetAt(0, 0, HeightMap.Type.MotionBlocking));
            Assert.AreEqual(map.GetAt(15, 13), 255);
            Assert.AreEqual(map.GetAt(7, 0), 60);
            Assert.AreEqual(map.GetAt(8, 9), 0);
        }

        [Test]
        public void AllBlocksTest()
        {
            var chunk = new ClassicChunk();
            var blocks = new (int X, int Y, int Z, ClassicBlock Block)[] {
                (15, 255, 13, new ClassicBlock(4095, 0)),
                (7, 60, 0, new ClassicBlock(32, 14))
            };

            foreach (var it in blocks)
            {
                chunk.SetBlock(it.X, it.Y, it.Z, it.Block);
            }

            foreach(var (X, Y, Z, block) in chunk.AllBlocks())
            {
                Assert.AreEqual(chunk.GetBlock(X, Y, Z), block);
            }
        }
    }
}