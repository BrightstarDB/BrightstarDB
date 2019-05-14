using System;
using System.IO;
using System.Security.Cryptography;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage
{
    internal class CommitPoint
    {
        public static int RecordSize = 256;

        public int VersionNumber { get; set; }
        public ulong LocationOffset { get; set; }
        public DateTime CommitTime { get; set; }
        public Guid JobId { get; set; }
        public ulong CommitNumber { get; set; }

        /// <summary>
        /// Get or set the next commit number to record
        /// </summary>
        /// <remarks>This is not persisted in the master file, but is instead caculated. Usually it is this.CommitNumber+1, but if the master file has corrupted commit points in it then this number can be higher to ensure a skip over old (corrupted) commit numbers</remarks>
        public ulong NextCommitNumber { get; set; }

        public CommitPoint(ulong offset, ulong commitNumber, DateTime commitTime, Guid jobId)
        {
            VersionNumber = 2;
            CommitNumber = commitNumber;
            LocationOffset = offset;
            CommitTime = commitTime;
            JobId = jobId;
        }

        private CommitPoint()
        {
        }

        /// <summary>
        /// Loads a CommitPoint structure from a stream
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <returns>A new CommitPoint structure constructed from the data in the stream</returns>
        /// <exception cref="BrightstarInternalException">Raised if the data could not be validated as a CommitPoint structure</exception>
        public static CommitPoint Load(Stream stream)
        {
            var ret = new CommitPoint();
            ret.Read(stream);
            ret.NextCommitNumber = ret.CommitNumber + 1;
            return ret;
        }

        public void Read(Stream stream)
        {
            try
            {
                var record = new byte[RecordSize];
                var commitPointRecord = new byte[RecordSize/2];
                stream.Read(record, 0, RecordSize);
                if (!ValidateCommitPointRecord(record, 0, commitPointRecord))
                {
                    if (!ValidateCommitPointRecord(record, RecordSize/2, commitPointRecord))
                    {
                        Logging.LogError(BrightstarEventId.CommitPointReadError,
                                         "Invalid commit point. Hashcode validation failed");
                        throw new InvalidCommitPointException(
                            "Invalid commit point - failed hash validation on both halves of commit point record.");
                    }
                }
                ReadCommitPointData(commitPointRecord);
            }
            catch (InvalidCommitPointException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.CommitPointReadError,
                                 "Error reading commit point from stream - " + ex);
                throw new InvalidCommitPointException(
                    "Invalid CommitPoint - error reading commit point from stream: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Provides the generic save method for commit point data
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            // WARNING: Changing the write buffer size will break compatibility

            // The commit point data is written twice, in each case followed by the MD5 hash
            // of the data as an error check code.
            // Each commit point is saved as 256 bytes, leaving 128 bytes for
            // commit data and checksum
            // MD5 hash is 16 bytes long, so commit point data cannot be > 112 bytes
            // For maintaining forwards compatibility, we should always write the
            // format version number for the commit point data as a 32-bit int 
            // in the first four bytes of the commit point data.
            var writeBuffer = new byte[256];
            var commitPointData = new byte[112];
            WriteCommitPointData(commitPointData);
            commitPointData.CopyTo(writeBuffer, 0);
            commitPointData.CopyTo(writeBuffer, 128);

            var hashAlgorithm = MD5.Create();
            var hash = hashAlgorithm.ComputeHash(commitPointData);
            hash.CopyTo(writeBuffer, 112);
            hash.CopyTo(writeBuffer, 240);

            stream.Write(writeBuffer, 0, 256);
        }

        /// <summary>
        /// This method writes the commit point data in the correct format
        /// for the current version to the specified byte array.
        /// </summary>
        /// <param name="commitPointData"></param>
        private void WriteCommitPointData(byte[] commitPointData)
        {
            BitConverter.GetBytes(VersionNumber).CopyTo(commitPointData, 0);
            BitConverter.GetBytes(CommitNumber).CopyTo(commitPointData, 4);
            BitConverter.GetBytes(LocationOffset).CopyTo(commitPointData, 12);
            BitConverter.GetBytes(CommitTime.Ticks).CopyTo(commitPointData, 20);
            JobId.ToByteArray().CopyTo(commitPointData, 28);
        }

        private bool ValidateCommitPointRecord(byte[] rawData, int offset, byte[] validatedRecord)
        {
            var dataLength = (RecordSize/2) - 16;
            byte[] dataToValidate = new byte[dataLength];
            Array.Copy(rawData, offset, dataToValidate, 0, dataLength);
            var hashAlgorithm = MD5.Create();
            var hash = hashAlgorithm.ComputeHash(dataToValidate);
            var recordedHash = new byte[16];
            Array.Copy(rawData, offset+dataLength, recordedHash, 0, 16);
            if (hash.Compare(recordedHash) == 0)
            {
                dataToValidate.CopyTo(validatedRecord, 0);
                return true;
            }
            return false;
        }

        private void ReadCommitPointData(byte[] commitPointData)
        {
            VersionNumber = BitConverter.ToInt32(commitPointData, 0);
            if (VersionNumber == 2)
            {
                CommitNumber = BitConverter.ToUInt64(commitPointData, 4);
                LocationOffset = BitConverter.ToUInt64(commitPointData, 12);
                var ticks = BitConverter.ToInt64(commitPointData, 20);
                CommitTime = new DateTime(ticks);
                var guidBytes = new byte[16];
                Array.Copy(commitPointData, 28, guidBytes, 0, 16);
                JobId = new Guid(guidBytes);
            }
            else
            {
                throw new InvalidCommitPointException(String.Format("Unrecognized commit point data version '{0}'",
                                                                    VersionNumber));

            }
        }
    }
}
