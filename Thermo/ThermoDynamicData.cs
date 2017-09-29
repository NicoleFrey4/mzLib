﻿using MassSpectrometry;
using MSFileReaderLib;
using System;
using System.IO;
using System.Security.Cryptography;

namespace IO.Thermo
{
    public class ThermoDynamicData : ThermoFile, IMsDynamicDataFile<IThermoScan>
    {
        #region Private Fields

        private IXRawfile5 _rawConnection;

        #endregion Private Fields

        #region Private Constructors

        private ThermoDynamicData(IXRawfile5 _rawConnection, int numSpectra, ManagedThermoHelperLayer.PrecursorInfo[] couldBePrecursor, SourceFile sourceFile, ThermoGlobalParams thermoGlobalParams) : base(_rawConnection, numSpectra, couldBePrecursor, sourceFile, thermoGlobalParams)
        {
            this._rawConnection = _rawConnection;
        }

        #endregion Private Constructors

        #region Public Methods

        public static ThermoDynamicData InitiateDynamicConnection(string filePath, int? topNpeaks = null, double? minRatio = null, bool trimMs1Peaks = true, bool trimMsMsPeaks = true)
        {
            IXRawfile5 _rawConnection = (IXRawfile5)new MSFileReader_XRawfile();
            _rawConnection.Open(filePath);
            _rawConnection.SetCurrentController(0, 1);

            int lastspectrumNumber = -1;
            _rawConnection.GetLastSpectrumNumber(ref lastspectrumNumber);
            int firstspectrumNumber = -1;
            _rawConnection.GetFirstSpectrumNumber(ref firstspectrumNumber);

            var precursorInfoArray = new ManagedThermoHelperLayer.PrecursorInfo[lastspectrumNumber - firstspectrumNumber + 1];

            string sendCheckSum;
            using (FileStream stream = File.OpenRead(filePath))
            {
                using (SHA1Managed sha = new SHA1Managed())
                {
                    byte[] checksum = sha.ComputeHash(stream);
                    sendCheckSum = BitConverter.ToString(checksum)
                        .Replace("-", string.Empty);
                }
            }
            SourceFile sourceFile = new SourceFile(
                @"Thermo nativeID format",
                @"Thermo RAW format",
                sendCheckSum,
                @"SHA-1",
                filePath,
                Path.GetFileNameWithoutExtension(filePath));

            var thermoGlobalParams = GetAllGlobalStuff(_rawConnection, precursorInfoArray, filePath);

            return new ThermoDynamicData(_rawConnection, lastspectrumNumber - firstspectrumNumber + 1, precursorInfoArray, sourceFile, thermoGlobalParams);
        }

        public override IThermoScan GetOneBasedScan(int oneBasedScanNumber)
        {
            if (Scans[oneBasedScanNumber - 1] == null)
                Scans[oneBasedScanNumber - 1] = GetMsDataOneBasedScanFromThermoFile(oneBasedScanNumber, _rawConnection, ThermoGlobalParams);
            return Scans[oneBasedScanNumber - 1];
        }

        public virtual void ClearCachedScans()
        {
            Array.Clear(Scans, 0, Scans.Length);
        }

        public override int GetClosestOneBasedSpectrumNumber(double retentionTime)
        {
            int spectrumNumber = 0;
            _rawConnection.ScanNumFromRT(retentionTime, ref spectrumNumber);
            return spectrumNumber;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (_rawConnection != null)
                    _rawConnection.Close();
        }

        #endregion Protected Methods
    }
}