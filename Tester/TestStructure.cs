﻿using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using multiplexingThrottler;

namespace Tester
{
    [TestClass]
    public class TestStructure
    {
        [TestMethod]
        [TestCategory("structure")]
        /**
         * make sure client spec construction and indexes look right
         */
        public void TestConstructor_simple()
        {

            var ips = new List<String>(){"192.168.1.1:8000","192.168.1.1:8001","192.168.1.1:8002","192.168.1.1:8003"};
            var bps = new List<int>(){1,2,3,4};
            var b = new Byte[1024];
            var ds = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, b).DeviceManagers;

            for (int i =0;i<ds.Count;i++)
            {
                DeviceManager device = (DeviceManager)ds[i];
                Console.WriteLine("Start = " + device.StartIdx + " End = " + device.EndIdx + " Size= " + (device.EndIdx - device.StartIdx));
                Assert.AreEqual(256 * i, device.StartIdx);
                Assert.AreEqual(256 * (i + 1), device.EndIdx);
                Assert.AreEqual(256, device.EndIdx - device.StartIdx);
                Assert.AreEqual(i + 1, device.SpeedInBitPerSecond);
            }
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "Destinations and SpeedinBPS must have equal none 0 length.")]
        [TestCategory("structure")]
        [TestCategory("exception")]
        public void TestConstructor_excep1()
        {
            var ips = new List<String>() { "192.168.1.1:8000", "192.168.1.1:8001", "192.168.1.1:8002", "192.168.1.1:8003" };
            var bps = new List<int>() { 1, 2, 3, 4,111111 };
            var b = new Byte[1024];
            var ds = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, b).DeviceManagers;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "The number of bytes must be evenly divided by destination number")]
        [TestCategory("structure")]
        [TestCategory("exception")]
        public void TestConstructor_excep2()
        {
            var ips = new List<String>() { "192.168.1.1:8000", "192.168.1.1:8001", "192.168.1.1:8002", "192.168.1.1:8003" };
            var bps = new List<int>() { 1, 2, 3, 4 };
            var b = new Byte[1023]; // die ... 
            var ds = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, b).DeviceManagers;
        }
    }
}
