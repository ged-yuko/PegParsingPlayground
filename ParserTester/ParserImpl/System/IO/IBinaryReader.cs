using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.IO
{
	public interface IBinaryReader : IDisposable
	{
		long Position { get; set; }

		int ReadBytes(byte[] buffer, int index, int count);
		int ReadChars(char[] buffer, int index, int count);
		bool ReadBoolean();
		byte ReadByte();
        byte[] ReadBytes(int count);
        byte[] ReadByteArray();
		char ReadChar();
        char[] ReadChars(int count);
        char[] ReadCharArray();
		decimal ReadDecimal();
		double ReadDouble();
		short ReadInt16();
		int ReadInt32();
		long ReadInt64();
		sbyte ReadSByte();
		float ReadSingle();
		string ReadString();
		ushort ReadUInt16();
		uint ReadUInt32();
		ulong ReadUInt64();

		long Seek(int offset, SeekOrigin origin);
		void Close();
	}
}
