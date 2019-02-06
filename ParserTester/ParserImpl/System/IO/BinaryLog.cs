using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.IO
{
    public interface IBinaryLog : IDisposable
    {
        BinaryLogRecord Add(byte[] record);
        BinaryLogRecord Get(long n);
        IEnumerable<BinaryLogRecord> Get(long from, long to);
        IEnumerable<BinaryLogRecord> Get(DateTime form, DateTime to);
        IEnumerable<BinaryLogRecord> GetAll();
    }

    internal class BinaryLogFile : IBinaryLog
    {
        FileStream _headerStream;
        StreamBinaryReader _headerReader;
        StreamBinaryWriter _headerWriter;

        FileStream _contentStream;
        StreamBinaryReader _contentReader;
        StreamBinaryWriter _contentWriter;

        long _recCount;
        long _contentPosition;
        DateTime _creationTime;
        string _filename;

        internal BinaryLogFile(string filename)
        {
            _filename = filename;

            _headerStream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            _headerReader = new StreamBinaryReader(_headerStream);
            _headerWriter = new StreamBinaryWriter(_headerStream);

            _contentStream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            _contentReader = new StreamBinaryReader(_contentStream);
            _contentWriter = new StreamBinaryWriter(_contentStream);

            _recCount = 1;
            _creationTime = new DateTime(_headerReader.ReadInt64());
            _contentPosition = _headerReader.ReadInt64();
        }

        public static IBinaryLog OpenFile(string filename)
        {
            return new BinaryLogFile(filename);
        }

        public static IBinaryLog CreateFile(string filename, long headerSize)
        {
            if (headerSize % 16 != 0)
                throw new ArgumentException();

            using (var stream = new FileStream(filename, FileMode.CreateNew))
            using (var writer = new StreamBinaryWriter(stream))
            {
                writer.WriteInt64(DateTime.UtcNow.Ticks);
                writer.WriteInt64(headerSize);
                stream.SetLength(headerSize);
            }

            return OpenFile(filename);
        }

        public BinaryLogRecord Add(byte[] record)
        {
            var hpos = _recCount * BinaryLogRecord.StampSize;
            if (hpos >= _contentPosition)
                throw new LogFileOverflowException(_filename);

            var rec = new BinaryLogRecord(DateTime.UtcNow, record);
            _headerWriter.Position = hpos;
            _headerWriter.WriteInt64(rec.timestamp.Ticks);
            _headerWriter.WriteInt64(_contentStream.Length);
            _contentWriter.Position = _contentStream.Length;
            _contentWriter.WriteByteArray(rec.content);
            _recCount++;
            return rec;
        }

        public BinaryLogRecord Get(long n)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BinaryLogRecord> Get(long from, long to)
        {
            throw new NotImplementedException("");
        }

        public IEnumerable<BinaryLogRecord> Get(DateTime form, DateTime to)
        {
            throw new NotImplementedException("");
        }

        public IEnumerable<BinaryLogRecord> GetAll()
        {
            _headerReader.Position = BinaryLogRecord.StampSize; ;
            while (_headerReader.Position < _contentPosition)
            {
                var ticks = _headerReader.ReadInt64();
                var pos = _headerReader.ReadInt64();
                if (pos == 0)
                    break;

                _contentReader.Position = pos;
                var data = _contentReader.ReadByteArray();
                yield return new BinaryLogRecord(new DateTime(ticks, DateTimeKind.Utc), data);
            }
        }

        public void Dispose()
        {
            _contentWriter.Flush();
            _headerWriter.Flush();
            _contentWriter.Dispose();
            _headerWriter.Dispose();
        }
    }

    public static class BinaryLog
    {
        public static IBinaryLog OpenFile(string filename)
        {
            return BinaryLogFile.OpenFile(filename);
        }

        public static IBinaryLog CreateFile(string filename, long headerSize = 0, long recordsCount = 0)
        {
            if (headerSize <= 0 && recordsCount <= 0)
                throw new ArgumentOutOfRangeException();

            if (headerSize <= 0)
                headerSize = ((recordsCount + 1) + (recordsCount + 1) % 2) * BinaryLogRecord.StampSize;

            return BinaryLogFile.CreateFile(filename, headerSize);
        }
    }

    public struct BinaryLogRecord
    {
        public readonly DateTime timestamp;
        public readonly byte[] content;

        public const int StampSize = sizeof(long) * 2;

        public BinaryLogRecord(DateTime timestamp, byte[] content)
        {
            this.timestamp = timestamp;
            this.content = content;
        }
    }

    [Serializable]
    public class LogFileOverflowException : Exception
    {
        public LogFileOverflowException(string fileName) : base(string.Format("Log file \"{0}\" overflow: there is no space for new records stamps in the header", fileName)) { }
        protected LogFileOverflowException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
