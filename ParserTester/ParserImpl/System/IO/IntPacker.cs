using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace System.IO
{
    //unsafe class IntPacker
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    //    private byte[] _bytes = new byte[16];

    //    public byte Int8
    //    {
    //        get { return _bytes[0]; }
    //        set
    //        {
    //            _bytes[7] = 0;
    //            _bytes[6] = 0;
    //            _bytes[5] = 0;
    //            _bytes[4] = 0;
    //            _bytes[3] = 0;
    //            _bytes[2] = 0;
    //            _bytes[1] = 0;
    //            _bytes[0] = value;
    //        }
    //    }

    //    public short Int16 { get { return BitConverter.ToInt16(_bytes, 0); } set { _bytes.PackInt16(value, 0); } }
    //    public int Int32 { get { return BitConverter.ToInt32(_bytes, 0); } set { _bytes.PackInt32(value, 0); } }
    //    public long Int64 { get { return BitConverter.ToInt64(_bytes, 0); } set { _bytes.PackInt64(value, 0); } }

    //    public int ReadCompactInteger(IBinaryReader r)
    //    {
    //        _bytes[0] = r.ReadByte();

    //    }

    //    public void WriteCompactInteger(BinaryWriter w)
    //    {
    //        var v = _bytes[7];

    //        value &= 0x1fffffff;
    //        if (value < 0x80)
    //        {
    //            w.Write((byte)value);
    //        }
    //        else
    //        {
    //            if (value < 0x4000)
    //            {
    //                w.Write((byte)(value >> 7 & 0x7f | 0x80));
    //                w.Write((byte)(value & 0x7f));
    //            }
    //            else
    //            {
    //                if (value < 0x200000)
    //                {
    //                    w.Write((byte)(value >> 14 & 0x7f | 0x80));
    //                    w.Write((byte)(value >> 7 & 0x7f | 0x80));
    //                    w.Write((byte)(value & 0x7f));
    //                }
    //                else
    //                {
    //                    w.Write((byte)(value >> 22 & 0x7f | 0x80));
    //                    w.Write((byte)(value >> 15 & 0x7f | 0x80));
    //                    w.Write((byte)(value >> 8 & 0x7f | 0x80));
    //                    w.Write((byte)(value & 0xff));
    //                }
    //            }
    //        }
    //    }
    //}
}
