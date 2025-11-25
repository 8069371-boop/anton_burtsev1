using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;
using FluentAssertions;
using NUnit.Framework;

namespace EchoServer.Tests.Abstractions
{
    [TestFixture]
    public class NetworkStreamWrapperUnitTests
    {
        private NetworkStreamWrapper? _sut;
        private TcpClient? _client;
        private TcpClient? _serverClient;
        private TcpListener? _listener;

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
            _client?.Dispose();
            _serverClient?.Dispose();
            
            if (_listener != null)
            {
                _listener.Stop();
                (_listener as IDisposable)?.Dispose();
            }
        }

        private async Task<(TcpClient client, TcpClient serverClient)> CreateConnectedClientsAsync()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            var acceptTask = _listener.AcceptTcpClientAsync();

            await Task.WhenAll(connectTask, acceptTask);

            return (client, acceptTask.Result);
        }

        [Test]
        public void Constructor_WithValidNetworkStream_ShouldCreateInstance()
        {
            // Arrange
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, port);
            var stream = _client.GetStream();

            // Act
            _sut = new NetworkStreamWrapper(stream);

            // Assert
            _sut.Should().NotBeNull();
            _sut.Should().BeAssignableTo<INetworkStreamWrapper>();
        }

        [Test]
        public void Constructor_WithNullNetworkStream_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () => _sut = new NetworkStreamWrapper(null!);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public async Task ReadAsync_WithValidData_ShouldReadBytes()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            var serverStream = _serverClient.GetStream();
            _sut = new NetworkStreamWrapper(_client.GetStream());

            var testData = Encoding.UTF8.GetBytes("Hello");
            await serverStream.WriteAsync(testData, 0, testData.Length);

            var buffer = new byte[1024];

            // Act
            var bytesRead = await _sut.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

            // Assert
            bytesRead.Should().Be(testData.Length);
            var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            receivedData.Should().Be("Hello");
        }

        [Test]
        public async Task ReadAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            _sut = new NetworkStreamWrapper(_client.GetStream());
            var buffer = new byte[1024];
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await _sut.ReadAsync(buffer, 0, buffer.Length, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task WriteAsync_WithValidData_ShouldWriteBytes()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            var serverStream = _serverClient.GetStream();
            _sut = new NetworkStreamWrapper(_client.GetStream());

            var testData = Encoding.UTF8.GetBytes("World");

            // Act
            await _sut.WriteAsync(testData, 0, testData.Length, CancellationToken.None);

            // Assert
            var buffer = new byte[1024];
            var bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
            bytesRead.Should().Be(testData.Length);
            var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            receivedData.Should().Be("World");
        }

        [Test]
        public async Task WriteAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            _sut = new NetworkStreamWrapper(_client.GetStream());
            var testData = Encoding.UTF8.GetBytes("Test");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await _sut.WriteAsync(testData, 0, testData.Length, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task ReadAsync_WithOffset_ShouldReadIntoCorrectPosition()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            var serverStream = _serverClient.GetStream();
            _sut = new NetworkStreamWrapper(_client.GetStream());

            var testData = Encoding.UTF8.GetBytes("Test");
            await serverStream.WriteAsync(testData, 0, testData.Length);

            var buffer = new byte[1024];
            var offset = 10;

            // Act
            var bytesRead = await _sut.ReadAsync(buffer, offset, buffer.Length - offset, CancellationToken.None);

            // Assert
            bytesRead.Should().Be(testData.Length);
            var receivedData = Encoding.UTF8.GetString(buffer, offset, bytesRead);
            receivedData.Should().Be("Test");
        }

        [Test]
        public async Task WriteAsync_WithOffset_ShouldWriteFromCorrectPosition()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            var serverStream = _serverClient.GetStream();
            _sut = new NetworkStreamWrapper(_client.GetStream());

            var buffer = new byte[1024];
            var testData = Encoding.UTF8.GetBytes("Data");
            var offset = 5;
            Array.Copy(testData, 0, buffer, offset, testData.Length);

            // Act
            await _sut.WriteAsync(buffer, offset, testData.Length, CancellationToken.None);

            // Assert
            var receiveBuffer = new byte[1024];
            var bytesRead = await serverStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
            bytesRead.Should().Be(testData.Length);
            var receivedData = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
            receivedData.Should().Be("Data");
        }

        [Test]
        public async Task ReadAsync_MultipleReads_ShouldWorkCorrectly()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            var serverStream = _serverClient.GetStream();
            _sut = new NetworkStreamWrapper(_client.GetStream());

            var testData1 = Encoding.UTF8.GetBytes("First");
            var testData2 = Encoding.UTF8.GetBytes("Second");
            
            await serverStream.WriteAsync(testData1, 0, testData1.Length);
            await serverStream.WriteAsync(testData2, 0, testData2.Length);

            var buffer1 = new byte[5];
            var buffer2 = new byte[6];

            // Act
            var bytesRead1 = await _sut.ReadAsync(buffer1, 0, buffer1.Length, CancellationToken.None);
            var bytesRead2 = await _sut.ReadAsync(buffer2, 0, buffer2.Length, CancellationToken.None);

            // Assert
            bytesRead1.Should().Be(5);
            bytesRead2.Should().Be(6);
            Encoding.UTF8.GetString(buffer1).Should().Be("First");
            Encoding.UTF8.GetString(buffer2).Should().Be("Second");
        }

        [Test]
        public void Dispose_WhenCalled_ShouldDisposeStream()
        {
            // Arrange
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, port);
            var stream = _client.GetStream();
            _sut = new NetworkStreamWrapper(stream);

            // Act
            _sut.Dispose();

            // Assert
            Action act = () => stream.WriteByte(0);
            act.Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, port);
            var stream = _client.GetStream();
            _sut = new NetworkStreamWrapper(stream);

            // Act
            Action act = () =>
            {
                _sut.Dispose();
                _sut.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public async Task ReadAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, port);
            var stream = _client.GetStream();
            _sut = new NetworkStreamWrapper(stream);
            _sut.Dispose();

            var buffer = new byte[1024];

            // Act
            Func<Task> act = async () => await _sut.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public async Task WriteAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, port);
            var stream = _client.GetStream();
            _sut = new NetworkStreamWrapper(stream);
            _sut.Dispose();

            var buffer = new byte[] { 1, 2, 3 };

            // Act
            Func<Task> act = async () => await _sut.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public void Dispose_WithNullStream_ShouldHandleGracefully()
        {
            // Arrange
            _sut = new NetworkStreamWrapper(null!);

            // Act
            Action act = () => _sut.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public async Task Wrapper_ShouldProperlyWrapNetworkStreamBehavior()
        {
            // Arrange
            var (client, serverClient) = await CreateConnectedClientsAsync();
            _client = client;
            _serverClient = serverClient;

            var serverStream = _serverClient.GetStream();
            _sut = new NetworkStreamWrapper(_client.GetStream());

            var sendData = Encoding.UTF8.GetBytes("Integration");
            var receiveBuffer = new byte[1024];

            // Act
            await _sut.WriteAsync(sendData, 0, sendData.Length, CancellationToken.None);
            var bytesRead = await serverStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);

            // Assert
            bytesRead.Should().Be(sendData.Length);
            var receivedData = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
            receivedData.Should().Be("Integration");
        }
    }
}
