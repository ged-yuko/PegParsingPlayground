using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.IO
{
    public interface IBinaryCursor : IBinaryReader, IBinaryWriter, IDisposable
    {
    }

    public class StreamBinaryCursor : IBinaryCursor
    {
        byte[] _buffer = new byte[16];
        Stream _stream;
        bool _ownStream;

        public StreamBinaryCursor(Stream stream, bool ownStream = false)
        {
            _stream = stream;
            _ownStream = ownStream;
        }

        public long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        #region writer

        public void WriteBytes(byte[] buffer, int index, int count)
        {
            _stream.Write(buffer, index, count);
        }

        public void WriteChars(char[] chars, int index, int count)
        {
            var data = Encoding.UTF8.GetBytes(chars, index, count);
            _stream.Write(data, 0, data.Length);
        }

        public void WriteBoolean(bool value)
        {
            _stream.Write(_buffer, 0, _buffer.PackUInt8(value ? ((byte)1) : ((byte)0), 0));
        }

        public void WriteSByte(sbyte value)
        {
            _stream.Write(_buffer, 0, _buffer.PackInt8(value, 0));
        }

        public void WriteByteArray(byte[] buffer)
        {
            this.WriteInt32(buffer.Length);
            this.WriteBytes(buffer, 0, buffer.Length);
        }

        public void WriteCharArray(char[] chars)
        {
            var data = Encoding.UTF8.GetBytes(chars);
            this.WriteInt32(data.Length);
            _stream.Write(data, 0, data.Length);
        }

        public void WriteByte(byte value)
        {
            _stream.Write(_buffer, 0, _buffer.PackUInt8(value, 0));
        }

        public void WriteChar(char ch)
        {
            _stream.Write(_buffer, 0, _buffer.PackUInt16((ushort)ch, 0));
        }

        public void WriteDecimal(decimal value)
        {
            _stream.Write(_buffer, 0, _buffer.PackDecimal(value, 0));
        }

        public void WriteDouble(double value)
        {
            _stream.Write(_buffer, 0, _buffer.PackDouble(value, 0));
        }

        public void WriteInt16(short value)
        {
            _stream.Write(_buffer, 0, _buffer.PackInt16(value, 0));
        }

        public void WriteInt32(int value)
        {
            _stream.Write(_buffer, 0, _buffer.PackInt32(value, 0));
        }

        public void WriteInt64(long value)
        {
            _stream.Write(_buffer, 0, _buffer.PackInt64(value, 0));
        }

        public void WriteSingle(float value)
        {
            _stream.Write(_buffer, 0, _buffer.PackSingle(value, 0));
        }

        public void WriteString(string value)
        {
            this.WriteByteArray(Encoding.UTF8.GetBytes(value));
        }

        public void WriteUInt16(ushort value)
        {
            _stream.Write(_buffer, 0, _buffer.PackUInt16(value, 0));
        }

        public void WriteUInt32(uint value)
        {
            _stream.Write(_buffer, 0, _buffer.PackUInt32(value, 0));
        }

        public void WriteUInt64(ulong value)
        {
            _stream.Write(_buffer, 0, _buffer.PackUInt64(value, 0));
        }

        public void Flush()
        {
            _stream.Flush();
        }

        #endregion

        #region reader

        public int ReadBytes(byte[] buffer, int index, int count)
        {
            return _stream.Read(buffer, index, count);
        }

        public int ReadChars(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public bool ReadBoolean()
        {
            return this.ReadByte() != 0;
        }

        public byte ReadByte()
        {
            _stream.Read(_buffer, 0, 1);
            return _buffer.UnpackUInt8(0);
        }

        public byte[] ReadBytes(int count)
        {
            var arr = new byte[count];
            _stream.Read(arr, 0, arr.Length);
            return arr;
        }

        public char ReadChar()
        {
            throw new NotImplementedException();
        }

        public char[] ReadChars(int count)
        {
            throw new NotImplementedException();
        }

        public decimal ReadDecimal()
        {
            throw new NotImplementedException();
        }

        public double ReadDouble()
        {
            throw new NotImplementedException();
        }

        public short ReadInt16()
        {
            _stream.Read(_buffer, 0, 2);
            return _buffer.UnpackInt16(0);
        }

        public int ReadInt32()
        {
            _stream.Read(_buffer, 0, 4);
            return _buffer.UnpackInt32(0);
        }

        public long ReadInt64()
        {
            _stream.Read(_buffer, 0, 8);
            return _buffer.UnpackInt64(0);
        }

        public sbyte ReadSByte()
        {
            throw new NotImplementedException();
        }

        public float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(this.ReadByteArray());
        }

        public ushort ReadUInt16()
        {
            _stream.Read(_buffer, 0, 2);
            return _buffer.UnpackUInt16(0);
        }

        public uint ReadUInt32()
        {
            _stream.Read(_buffer, 0, 4);
            return _buffer.UnpackUInt32(0);
        }

        public ulong ReadUInt64()
        {
            _stream.Read(_buffer, 0, 8);
            return _buffer.UnpackUInt64(0);
        }

        public byte[] ReadByteArray()
        {
            var count = this.ReadInt32();
            return this.ReadBytes(count);
        }

        public char[] ReadCharArray()
        {
            var count = this.ReadInt32();
            var bytes = this.ReadBytes(count);
            return Encoding.UTF8.GetChars(bytes);
        }

        #endregion

        public void Close()
        {
            if (_ownStream)
                _stream.Close();
        }

        public void Dispose()
        {
            if (_ownStream)
                _stream.Dispose();
        }
    }
}