using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.IO
{
	public class StreamBinaryReader : IBinaryReader
	{
		byte[] _buffer = new byte[16];
		Stream _stream;
		bool _ownStream;

		public StreamBinaryReader(Stream stream, bool ownStream = false)
		{
			_stream = stream;
			_ownStream = ownStream;
		}

		public long Position
		{
			get { return _stream.Position; }
			set { _stream.Position = value; }
		}

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

		public long Seek(int offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}

		public void Close()
		{
			_stream.Close();
		}

		public void Dispose()
		{
			if (_ownStream)
				_stream.Dispose();
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
    }
}
