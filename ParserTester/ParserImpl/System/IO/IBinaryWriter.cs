using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.IO
{
	public interface IBinaryWriter : IDisposable
	{
		long Position { get; set; }

		void WriteBytes(byte[] buffer, int index, int count);
		void WriteChars(char[] chars, int index, int count);
		void WriteBoolean(bool value);
		void WriteSByte(sbyte value);
		void WriteByteArray(byte[] buffer);
		void WriteCharArray(char[] chars);
		void WriteByte(byte value);
		void WriteChar(char ch);
		void WriteDecimal(decimal value);
		void WriteDouble(double value);
		void WriteInt16(short value);
		void WriteInt32(int value);
		void WriteInt64(long value);
		void WriteSingle(float value);
		void WriteString(string value);
		void WriteUInt16(ushort value);
		void WriteUInt32(uint value);
		void WriteUInt64(ulong value);

		long Seek(int offset, SeekOrigin origin);
		void Flush();
		void Close();
	}
}
