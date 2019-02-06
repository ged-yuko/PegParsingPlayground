using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.IO
{
    //public class ExpandableMemoryStream : Stream
    //{
    //    public override bool CanRead
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override bool CanSeek
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override bool CanWrite
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override void Flush()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override long Length
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override long Position
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //        set
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    byte[] _buff;

    //    public ExpandableMemoryStream(byte[] initialBuffer)
    //    {
    //        _buff = initialBuffer;
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void SetLength(long value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public byte[] GetBuffer()
    //    {
    //        return _buff;
    //    }
    //}

    [Serializable]
    public class ExpandableMemoryStream : Stream
    {
        // Fields
        private byte[] _buffer;
        private int _capacity;
        private bool _exposable;
        private bool _isOpen;
        [NonSerialized]
        private int _length;
        private int _origin;
        private int _position;
        private bool _writable;
        private const int MemStreamMaxLength = 2147483647;

        public ExpandableMemoryStream()
            : this(0)
        {
        }

        public ExpandableMemoryStream(byte[] buffer)
            : this(buffer, true)
        {
        }

        public ExpandableMemoryStream(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", MyEnvironment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
            }
            this._buffer = new byte[capacity];
            this._capacity = capacity;
            this._writable = true;
            this._exposable = true;
            this._origin = 0;
            this._isOpen = true;
        }

        public ExpandableMemoryStream(byte[] buffer, bool writable)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", MyEnvironment.GetResourceString("ArgumentNull_Buffer"));
            }
            this._buffer = buffer;
            this._length = this._capacity = buffer.Length;
            this._writable = writable;
            this._exposable = false;
            this._origin = 0;
            this._isOpen = true;
        }

        public ExpandableMemoryStream(byte[] buffer, int index, int count)
            : this(buffer, index, count, true, false)
        {
        }

        public ExpandableMemoryStream(byte[] buffer, int index, int count, bool writable)
            : this(buffer, index, count, writable, false)
        {
        }

        public ExpandableMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", MyEnvironment.GetResourceString("ArgumentNull_Buffer"));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if ((buffer.Length - index) < count)
            {
                throw new ArgumentException(MyEnvironment.GetResourceString("Argument_InvalidOffLen"));
            }
            this._buffer = buffer;
            this._origin = this._position = index;
            this._length = this._capacity = index + count;
            this._writable = writable;
            this._exposable = publiclyVisible;
            this._isOpen = true;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this._isOpen = false;
                    this._writable = false;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private bool EnsureCapacity(int value)
        {
            if (value < 0)
            {
                throw new IOException(MyEnvironment.GetResourceString("IO.IO_StreamTooLong"));
            }
            if (value <= this._capacity)
            {
                return false;
            }
            int num = value;
            if (num < 256)
            {
                num = 256;
            }
            if (num < (this._capacity * 2))
            {
                num = this._capacity * 2;
            }
            this.Capacity = num;
            return true;
        }

        private void EnsureWriteable()
        {
            if (!this.CanWrite)
            {
                //__Error.WriteNotSupported();
                throw new NotImplementedException("");
            }
        }

        public override void Flush()
        {
        }
        
        public virtual byte[] GetBuffer()
        {
            if (!this._exposable)
            {
                throw new UnauthorizedAccessException(MyEnvironment.GetResourceString("UnauthorizedAccess_MemStreamBuffer"));
            }
            return this._buffer;
        }

        internal int InternalEmulateRead(int count)
        {
            if (!this._isOpen)
            {
                //__Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            int num = this._length - this._position;
            if (num > count)
            {
                num = count;
            }
            if (num < 0)
            {
                num = 0;
            }
            this._position += num;
            return num;
        }

        internal byte[] InternalGetBuffer()
        {
            return this._buffer;
        }

        internal void InternalGetOriginAndLength(out int origin, out int length)
        {
            if (!this._isOpen)
            {
                //__Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            origin = this._origin;
            length = this._length;
        }

        internal int InternalGetPosition()
        {
            if (!this._isOpen)
            {
                //__Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            return this._position;
        }

        internal int InternalReadInt32()
        {
            if (!this._isOpen)
            {
                //__Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            int num = this._position += 4;
            if (num > this._length)
            {
                this._position = this._length;
                //__Error.EndOfFile();
                throw new NotImplementedException("");
            }
            return (((this._buffer[num - 4] | (this._buffer[num - 3] << 8)) | (this._buffer[num - 2] << 16)) | (this._buffer[num - 1] << 24));
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", MyEnvironment.GetResourceString("ArgumentNull_Buffer"));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if ((buffer.Length - offset) < count)
            {
                throw new ArgumentException(MyEnvironment.GetResourceString("Argument_InvalidOffLen"));
            }
            if (!this._isOpen)
            {
                //__Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            int byteCount = this._length - this._position;
            if (byteCount > count)
            {
                byteCount = count;
            }
            if (byteCount <= 0)
            {
                return 0;
            }
            if (byteCount <= 8)
            {
                int num2 = byteCount;
                while (--num2 >= 0)
                {
                    buffer[offset + num2] = this._buffer[this._position + num2];
                }
            }
            else
            {
                Buffer.BlockCopy(this._buffer, this._position, buffer, offset, byteCount);
            }
            this._position += byteCount;
            return byteCount;
        }
        
        public override int ReadByte()
        {
            if (!this._isOpen)
            {
                // __Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            if (this._position >= this._length)
            {
                return -1;
            }
            return this._buffer[this._position++];
        }
        
        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!this._isOpen)
            {
                // __Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            if (offset > 2147483647L)
            {
                throw new ArgumentOutOfRangeException("offset", MyEnvironment.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }
            switch (loc)
            {
                case SeekOrigin.Begin:
                    {
                        int num = this._origin + ((int)offset);
                        if ((offset < 0L) || (num < this._origin))
                        {
                            throw new IOException(MyEnvironment.GetResourceString("IO.IO_SeekBeforeBegin"));
                        }
                        this._position = num;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        int num2 = this._position + ((int)offset);
                        if (((this._position + offset) < this._origin) || (num2 < this._origin))
                        {
                            throw new IOException(MyEnvironment.GetResourceString("IO.IO_SeekBeforeBegin"));
                        }
                        this._position = num2;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        int num3 = this._length + ((int)offset);
                        if (((this._length + offset) < this._origin) || (num3 < this._origin))
                        {
                            throw new IOException(MyEnvironment.GetResourceString("IO.IO_SeekBeforeBegin"));
                        }
                        this._position = num3;
                        break;
                    }
                default:
                    throw new ArgumentException(MyEnvironment.GetResourceString("Argument_InvalidSeekOrigin"));
            }
            return (long)this._position;
        }

        public override void SetLength(long value)
        {
            if ((value < 0L) || (value > 2147483647L))
            {
                throw new ArgumentOutOfRangeException("value", MyEnvironment.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }
            this.EnsureWriteable();
            if (value > (2147483647 - this._origin))
            {
                throw new ArgumentOutOfRangeException("value", MyEnvironment.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }
            int num = this._origin + ((int)value);
            if (!this.EnsureCapacity(num) && (num > this._length))
            {
                Array.Clear(this._buffer, this._length, num - this._length);
            }
            this._length = num;
            if (this._position > num)
            {
                this._position = num;
            }
        }

        public virtual byte[] ToArray()
        {
            byte[] dst = new byte[this._length - this._origin];
            Buffer.BlockCopy(this._buffer, this._origin, dst, 0, this._length - this._origin);
            return dst;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", MyEnvironment.GetResourceString("ArgumentNull_Buffer"));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if ((buffer.Length - offset) < count)
            {
                throw new ArgumentException(MyEnvironment.GetResourceString("Argument_InvalidOffLen"));
            }
            if (!this._isOpen)
            {
                // __Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            this.EnsureWriteable();
            int num = this._position + count;
            if (num < 0)
            {
                throw new IOException(MyEnvironment.GetResourceString("IO.IO_StreamTooLong"));
            }
            if (num > this._length)
            {
                bool flag = this._position > this._length;
                if ((num > this._capacity) && this.EnsureCapacity(num))
                {
                    flag = false;
                }
                if (flag)
                {
                    Array.Clear(this._buffer, this._length, num - this._length);
                }
                this._length = num;
            }
            if ((count <= 8) && (buffer != this._buffer))
            {
                int num2 = count;
                while (--num2 >= 0)
                {
                    this._buffer[this._position + num2] = buffer[offset + num2];
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, this._buffer, this._position, count);
            }
            this._position = num;
        }

        public override void WriteByte(byte value)
        {
            if (!this._isOpen)
            {
                // __Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            this.EnsureWriteable();
            if (this._position >= this._length)
            {
                int num = this._position + 1;
                bool flag = this._position > this._length;
                if ((num >= this._capacity) && this.EnsureCapacity(num))
                {
                    flag = false;
                }
                if (flag)
                {
                    Array.Clear(this._buffer, this._length, this._position - this._length);
                }
                this._length = num;
            }
            this._buffer[this._position++] = value;
        }

        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream", MyEnvironment.GetResourceString("ArgumentNull_Stream"));
            }
            if (!this._isOpen)
            {
                //__Error.StreamIsClosed();
                throw new NotImplementedException("");
            }
            stream.Write(this._buffer, this._origin, this._length - this._origin);
        }

        // Properties

        public override bool CanRead
        {
            get
            {
                return this._isOpen;
            }
        }


        public override bool CanSeek
        {
            get
            {
                return this._isOpen;
            }
        }


        public override bool CanWrite
        {
            get
            {
                return this._writable;
            }
        }


        public virtual int Capacity
        {
    
            get
            {
                if (!this._isOpen)
                {
                    // __Error.StreamIsClosed();
                    throw new NotImplementedException("");
                }
                return (this._capacity - this._origin);
            }
    
            set
            {
                if (value < this.Length)
                {
                    throw new ArgumentOutOfRangeException("value", MyEnvironment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                if (!this._isOpen)
                {
                    // __Error.StreamIsClosed();
                    throw new NotImplementedException("");
                }
                //if (value != this.Capacity)
                //{
                //    // __Error.MemoryStreamNotExpandable();
                //    throw new NotImplementedException("");
                //}
                if (value != this._capacity)
                {
                    if (value > 0)
                    {
                        byte[] dst = new byte[value];
                        if (this._length > 0)
                        {
                            Buffer.BlockCopy(this._buffer, 0, dst, 0, this._length);
                        }
                        this._buffer = dst;
                    }
                    else
                    {
                        this._buffer = null;
                    }
                    this._capacity = value;
                }
            }
        }


        public override long Length
        {
            get
            {
                if (!this._isOpen)
                {
                    // __Error.StreamIsClosed();
                    throw new NotImplementedException("");
                }
                return (long)(this._length - this._origin);
            }
        }


        public override long Position
        {
            get
            {
                if (!this._isOpen)
                {
                    //__Error.StreamIsClosed();
                    throw new NotImplementedException("");
                }
                return (long)(this._position - this._origin);
            }
    
            set
            {
                if (value < 0L)
                {
                    throw new ArgumentOutOfRangeException("value", MyEnvironment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                }
                if (!this._isOpen)
                {
                    //__Error.StreamIsClosed();
                    throw new NotImplementedException("");
                }
                if (value > 2147483647L)
                {
                    throw new ArgumentOutOfRangeException("value", MyEnvironment.GetResourceString("ArgumentOutOfRange_StreamLength"));
                }
                this._position = this._origin + ((int)value);
            }
        }
    }

}
