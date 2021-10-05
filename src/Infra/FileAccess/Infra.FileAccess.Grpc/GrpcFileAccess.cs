using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Infra.Core.FileAccess.Abstractions;
using Infra.Core.FileAccess.Models;
using Infra.FileAccess.Grpc.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infra.FileAccess.Grpc
{
    public class GrpcFileAccess : IFileAccess
    {
        private readonly ILogger<GrpcFileAccess> _logger;
        private readonly Env _env;

        #region Constructor

        public GrpcFileAccess(
            ILogger<GrpcFileAccess> logger,
            IConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = new Env(config);
        }

        #endregion

        #region Sync Method

        #region Directory

        public void CreateDirectory(string directoryPath) => throw new NotImplementedException();

        public bool DirectoryExists(string directoryPath) => throw new NotImplementedException();

        public string[] GetFiles(string directoryPath) => throw new NotImplementedException();

        public string[] GetFiles(string directoryPath, string searchPattern) => throw new NotImplementedException();

        public string[] GetFiles(string directoryPath, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();

        public void DeleteDirectory(string directoryPath) => throw new NotImplementedException();

        public void DeleteDirectory(string directoryPath, bool recursive) => throw new NotImplementedException();

        public string[] GetSubDirectories(string directoryPath) => throw new NotImplementedException();

        public string[] GetSubDirectories(string directoryPath, string searchPattern) => throw new NotImplementedException();

        public string[] GetSubDirectories(string directoryPath, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();

        public void DirectoryCompress(string directoryPath, string zipFilePath) => throw new NotImplementedException();

        public string GetParentPath(string directoryPath) => throw new NotImplementedException();

        public string GetCurrentDirectoryName(string directoryPath) => throw new NotImplementedException();

        #endregion

        public bool FileExists(string filePath) => throw new NotImplementedException();

        public void SaveFile(string filePath, string content) => throw new NotImplementedException();

        public void SaveFile(string filePath, string content, Encoding encoding) => throw new NotImplementedException();

        public void SaveFile(string filePath, byte[] bytes) => throw new NotImplementedException();

        public void DeleteFile(string filePath) => throw new NotImplementedException();

        public long GetFileSize(string filePath) => throw new NotImplementedException();

        public string ReadTextFile(string filePath) => throw new NotImplementedException();

        public string ReadTextFile(string filePath, Encoding encoding) => throw new NotImplementedException();

        public byte[] ReadFile(string filePath) => throw new NotImplementedException();

        public void MoveFile(string sourceFilePath, string destFilePath) => throw new NotImplementedException();

        public void MoveFile(string sourceFilePath, string destFilePath, bool overwrite) => throw new NotImplementedException();

        public void CopyFile(string sourceFilePath, string destFilePath) => throw new NotImplementedException();

        public void CopyFile(string sourceFilePath, string destFilePath, bool overwrite) => throw new NotImplementedException();

        #endregion

        #region Async Method

        public Task SaveFileAsync(string filePath, string content, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task SaveFileAsync(string filePath, string content, Encoding encoding, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task SaveFileAsync(string filePath, byte[] bytes, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public async Task SaveFileAsync(string filePath, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default)
        {
            FileStream fs = null;
            var mark = $"{Guid.NewGuid()}";
            var startTime = DateTime.Now;
            var chunkSize = _env.ChunkSize;
            var buffer = new byte[chunkSize];
            var (channel, client) = GetClient();
            var progressInfo = new ProgressInfo();

            try
            {
                using var call = client.Upload(default);

                var request = new UploadRequest()
                {
                    Filename = Path.GetFileName(filePath),
                    Mark = mark
                };

                fs = new FileStream(
                    filePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read, chunkSize, useAsync: true);

                var readTimes = 0;
                var uploadedSize = 0;

                while (true)
                {
                    // Initiative cancel.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Block = -1; // -1 means file transfer canceled.
                        request.Content = Google.Protobuf.ByteString.Empty;
                        await call.RequestStream.WriteAsync(request);

                        progressInfo.IsCompleted = false;
                        progressInfo.Message = $"File【{filePath}】upload canceled. SpentTime:{DateTime.Now - startTime}";
                        _logger.LogInformation(progressInfo.Message);
                        progressCallBack?.Invoke(progressInfo);
                        break;
                    }

                    var readSize = fs.Read(buffer, 0, buffer.Length);

                    // Transfer file chunk to server.
                    if (readSize > 0)
                    {
                        request.Block = ++readTimes;
                        request.Content = Google.Protobuf.ByteString.CopyFrom(buffer, 0, readSize);
                        await call.RequestStream.WriteAsync(request);

                        uploadedSize += readSize;
                        progressInfo.Message = $"File【{filePath}】current upload progress【{uploadedSize}/{fs.Length}】bytes.";
                        progressCallBack?.Invoke(progressInfo);
                    }
                    // Transfer is completed.
                    else
                    {
                        request.Block = 0;
                        request.Content = Google.Protobuf.ByteString.Empty;
                        await call.RequestStream.WriteAsync(request);

                        // Waiting server response.
                        await call.ResponseStream.MoveNext(cancellationToken);

                        if (call.ResponseStream.Current != null && call.ResponseStream.Current.Mark == mark)
                        {
                            progressInfo.IsCompleted = true;
                            progressInfo.Message = $"File【{filePath}】upload completed. SpentTime:{DateTime.Now - startTime}";
                            progressInfo.FilePath = filePath;
                            _logger.LogInformation(progressInfo.Message);
                            progressCallBack?.Invoke(progressInfo);
                        }

                        break;
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await call.RequestStream.WriteAsync(new UploadRequest
                    {
                        Block = -2, // -2 means all file chunk transfer completed.
                        Mark = mark
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"File【{filePath}】upload unexpected exception happened.({ex.GetType()}):{ex.Message}");
                throw;
            }
            finally
            {
                fs?.Close();
                fs?.Dispose();

                // Shutdown the channel.
                await channel?.ShutdownAsync();
            }
        }

        public Task<string> ReadTextFileAsync(string filePath, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> ReadTextFileAsync(string filePath, Encoding encoding, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public async Task<byte[]> ReadFileAsync(string filePath, Action<ProgressInfo> progressCallBack = null, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            var mark = $"{Guid.NewGuid()}";
            var startTime = DateTime.Now;
            var (channel, client) = GetClient();
            var progressInfo = new ProgressInfo();
            var fileName = Path.GetFileName(filePath);

            try
            {
                var request = new DownloadRequest()
                {
                    Filename = fileName,
                    Mark = mark
                };

                using var call = client.Download(request, default);

                var fileContents = new List<DownloadResponse>();
                var reaponseStream = call.ResponseStream;

                while (await reaponseStream.MoveNext(cancellationToken))
                {
                    // Initiative cancel.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        progressInfo.IsCompleted = false;
                        progressInfo.Message = $"File【{fileName}】download canceled. SpentTime:{DateTime.Now - startTime}";
                        _logger.LogInformation(progressInfo.Message);
                        progressCallBack?.Invoke(progressInfo);
                        break;
                    }

                    // All file transfer completed. (Block = -2)
                    if (reaponseStream.Current.Block == -2)
                    {
                        // -2 means all file chunk transfer completed.
                        break;
                    }
                    // file transfer canceled or error happened. (Block = -1)
                    else if (reaponseStream.Current.Block == -1)
                    {
                        progressInfo.IsCompleted = false;
                        progressInfo.Message = $"File【{fileName}】download transfer failed. SpentTime:{DateTime.Now - startTime}";
                        _logger.LogInformation(progressInfo.Message);
                        progressCallBack?.Invoke(progressInfo);
                        fileContents.Clear();
                    }
                    // file transfer completed. (Block = 0)
                    else if (reaponseStream.Current.Block == 0)
                    {
                        // if file chunk exists, then write into stream.
                        if (fileContents.Any())
                        {
                            fileContents.OrderBy(c => c.Block).ToList().ForEach(c => c.Content.WriteTo(ms));
                            progressInfo.Message = $"File【{fileName}】current download progress【{ms.Length}】bytes.";
                            progressCallBack?.Invoke(progressInfo);
                            fileContents.Clear();
                        }

                        progressInfo.IsCompleted = true;
                        progressInfo.Message = $"File【{fileName}】download completed. SpentTime:{DateTime.Now - startTime}";
                        progressInfo.FileName = fileName;
                        _logger.LogInformation(progressInfo.Message);
                        progressCallBack?.Invoke(progressInfo);
                    }
                    else
                    {
                        // Add file chunk to list.
                        fileContents.Add(reaponseStream.Current);

                        // Collect file chunks then write into stream. (file chunk size decide by server code...)
                        if (fileContents.Count >= _env.ChunkBufferCount)
                        {
                            fileContents.OrderBy(c => c.Block).ToList().ForEach(c => c.Content.WriteTo(ms));
                            progressInfo.Message = $"File【{fileName}】current download progress【{ms.Length}】bytes.";
                            progressCallBack?.Invoke(progressInfo);
                            fileContents.Clear();
                        }
                    }
                }

                return progressInfo.IsCompleted ? ms.ToArray() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"File【{fileName}】download unexpected exception happened.({ex.GetType()}):{ex.Message}");
                throw;
            }
            finally
            {
                // Shutdown the channel.
                await channel?.ShutdownAsync();
            }
        }

        #endregion

        #region Private Method

        private (GrpcChannel, FileTransfer.FileTransferClient) GetClient()
        {
            var channel = GrpcChannel.ForAddress(_env.ServerAddress);
            var client = new FileTransfer.FileTransferClient(channel);
            return (channel, client);
        }

        #endregion
    }
}
