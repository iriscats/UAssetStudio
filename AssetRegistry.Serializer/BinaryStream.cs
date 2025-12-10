using System;
using System.IO;
using System.Text;

namespace AssetRegistry.Serializer
{
    // Minimal BinaryStream wrapper to satisfy AssetRegistry.Serializer needs
    public class BinaryStream : IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        public BinaryStream(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
            _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        }

        public byte[] ReadBytes(int count) => _reader.ReadBytes(count);
        public uint ReadUInt32() => _reader.ReadUInt32();
        public int ReadInt32() => _reader.ReadInt32();
        public long ReadInt64() => _reader.ReadInt64();
        public ulong ReadUInt64() => _reader.ReadUInt64();

        public void Write(byte[] data)
        {
            if (data == null) return;
            _writer.Write(data);
        }

        public void Write(uint value) => _writer.Write(value);
        public void Write(int value) => _writer.Write(value);
        public void Write(long value) => _writer.Write(value);
        public void Write(short value) => _writer.Write(value);
        public void Write(byte value) => _writer.Write(value);

        public void Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public long Tell() => _stream.Position;

        public void Close()
        {
            _writer.Flush();
            // Keep stream open; caller manages underlying MemoryStream lifecycle
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _reader?.Dispose();
        }
    }
}

