﻿using IO.MzML;
using MassSpectrometry;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using ThermoRawFileReader;

namespace Test
{
    [TestFixture]
    public sealed class TestRawFileReader
    {
        [Test]
        [TestCase("testFileWMS2.raw", "a.mzML", "aa.mzML")]
        [TestCase("small.raw", "a.mzML", "aa.mzML")]
        [TestCase("05-13-16_cali_MS_60K-res_MS.raw", "a.mzML", "aa.mzML")]
        public static void TestRawFileReader1(string infile, string outfile1, string outfile2)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", infile);
            outfile1 = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", outfile1);
            outfile2 = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", outfile2);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var a = ThermoRawFileReaderData.LoadAllStaticData(path, maxThreads: 1);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(a, outfile1, false);
            var aa = Mzml.LoadAllStaticData(outfile1);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(aa, outfile2, true);
            Mzml.LoadAllStaticData(outfile2);
            Console.WriteLine($"Analysis time for TestRawFileReader1({infile}): {stopwatch.Elapsed.Hours}h {stopwatch.Elapsed.Minutes}m {stopwatch.Elapsed.Seconds}s");
        }

        [Test]
        public static void TestSingleScanRawReader()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "small.raw");

            ThermoRawFileReaderData.InitiateDynamicConnection(path);

            var a = ThermoRawFileReaderData.GetOneBasedScanFromDynamicConnection(1);
            Assert.That(a != null);

            a = ThermoRawFileReaderData.GetOneBasedScanFromDynamicConnection(10000);
            Assert.That(a == null);

            ThermoRawFileReaderData.CloseDynamicConnection();
            
            Console.WriteLine($"Analysis time for TestSingleScanRawReader: {stopwatch.Elapsed.Hours}h {stopwatch.Elapsed.Minutes}m {stopwatch.Elapsed.Seconds}s");
        }

        [Test]
        public static void TestPeakFilteringRawFileReader()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "testFileWMS2.raw");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var a = ThermoRawFileReaderData.LoadAllStaticData(path, new FilteringParams(200, null, 1, true, true), maxThreads: 1);
            foreach(var scan in a.GetAllScansList())
            {
                Assert.That(scan.MassSpectrum.XArray.Length <= 200);
            }

            Console.WriteLine($"Analysis time for TestPeakFilteringRawFileReader: {stopwatch.Elapsed.Hours}h {stopwatch.Elapsed.Minutes}m {stopwatch.Elapsed.Seconds}s");
        }
    }
}
