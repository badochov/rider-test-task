using System;
using System.IO;

namespace RiderTestTask
{
    public class FileReader : IDisposable
    {
        private readonly FileStream fileStream;
        private int inBuffer = 0;
        private int position = 0;
        private byte[] buffer;

        public FileReader(string fileName, int bufSize = 8096)
        {
            fileStream = File.OpenRead(fileName);
            buffer = new byte[bufSize];
        }

        public byte ReadChar()
        {
            if (position >= inBuffer)
            {
                inBuffer = fileStream.Read(buffer, 0, buffer.Length);
            }

            return buffer[position++];
        }

        public byte? TryReadChar()
        {
            if (!CanRead())
            {
                return null;
            }

            if (position >= inBuffer)
            {
                inBuffer = fileStream.Read(buffer, 0, buffer.Length);
            }

            return buffer[position++];
        }

        public void UnReadChar()
        {
            position--;
        }

        public bool CanRead()
        {
            return fileStream.CanRead || position != inBuffer;
        }

        public void Dispose()
        {
            fileStream.Dispose();
            GC.SuppressFinalize(this);
        }

        ~FileReader()
        {
            fileStream.Dispose();
        }
    }
}