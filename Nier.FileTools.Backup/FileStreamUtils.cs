using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Nier.FileTools.Backup
{
    public interface IFileStreamBufferConsumer : IAsyncDisposable
    {
        Task ConsumeAsync(byte[] buffer, int bufferOffset, int count);

        Task ConsumeLastBlockAsync(byte[] buffer, int bufferOffset, int count)
        {
            return ConsumeAsync(buffer, bufferOffset, count);
        }
    }

    public class ByteArrayFileStreamBufferConsumer : IFileStreamBufferConsumer
    {
        private readonly byte[] _bytes;
        private int _offset;

        public ByteArrayFileStreamBufferConsumer(byte[] bytes)
        {
            _bytes = bytes;
        }

        public Task ConsumeAsync(byte[] buffer, int bufferOffset, int count)
        {
            Array.Copy(buffer, bufferOffset, _bytes, _offset, count);
            _offset += count;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class ShaFileStreamBufferConsumer : IFileStreamBufferConsumer
    {
        private readonly SHA256 _sha = SHA256.Create();

        public Task ConsumeAsync(byte[] buffer, int bufferOffset, int count)
        {
            _ = _sha.TransformBlock(buffer, bufferOffset, count, null, 0);
            return Task.CompletedTask;
        }

        public Task ConsumeLastBlockAsync(byte[] buffer, int bufferOffset, int count)
        {
            _sha.TransformFinalBlock(buffer, bufferOffset, count);
            return Task.CompletedTask;
        }

        public byte[] GetHash()
        {
            return _sha.Hash;
        }

        public ValueTask DisposeAsync()
        {
            _sha?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    public class WriteFileFileStreamBufferConsumer : IFileStreamBufferConsumer
    {
        private readonly FileStream _outputFs;

        public WriteFileFileStreamBufferConsumer(string outputFileName)
        {
            _outputFs = new FileStream(outputFileName, FileMode.OpenOrCreate);
        }

        public Task ConsumeAsync(byte[] buffer, int bufferOffset, int count)
        {
            return _outputFs.WriteAsync(buffer, bufferOffset, count);
        }

        public ValueTask DisposeAsync()
        {
            return _outputFs?.DisposeAsync() ?? ValueTask.CompletedTask;
        }
    }

    public class AggregatedFileStreamBufferConsumer : IFileStreamBufferConsumer
    {
        private readonly IList<IFileStreamBufferConsumer> _consumers;

        public AggregatedFileStreamBufferConsumer(IList<IFileStreamBufferConsumer> consumers)
        {
            _consumers = consumers;
        }

        public Task ConsumeAsync(byte[] buffer, int bufferOffset, int count)
        {
            return Task.WhenAll(_consumers.Select(c => c.ConsumeAsync(buffer, bufferOffset, count)));
        }

        public Task ConsumeLastBlockAsync(byte[] buffer, int bufferOffset, int count)
        {
            return Task.WhenAll(_consumers.Select(c => c.ConsumeLastBlockAsync(buffer, bufferOffset, count)));
        }

        public async ValueTask DisposeAsync()
        {
            foreach (IFileStreamBufferConsumer consumer in _consumers)
            {
                await consumer.DisposeAsync();
            }
        }
    }

    public static class FileStreamUtils
    {
        public static async Task ReadAsync(FileStream input, long offset, int count,
            IFileStreamBufferConsumer bufferConsumer, int maxBufferSize = 16 * 1024 * 1024)
        {
            int bufferSize = count;
            if (bufferSize > maxBufferSize)
            {
                bufferSize = maxBufferSize;
            }

            byte[] buffer = new byte[bufferSize];
            input.Seek(offset, SeekOrigin.Begin);
            int toRead = count;
            while (toRead > 0)
            {
                int bytesToReadInBuffer = toRead;
                if (bytesToReadInBuffer > bufferSize)
                {
                    bytesToReadInBuffer = bufferSize;
                }

                int read = await input.ReadAsync(buffer, 0, bytesToReadInBuffer);
                toRead -= read;
                if (toRead > 0)
                {
                    await bufferConsumer.ConsumeAsync(buffer, 0, read);
                }
                else
                {
                    await bufferConsumer.ConsumeLastBlockAsync(buffer, 0, read);
                }
            }
        }
    }
}
