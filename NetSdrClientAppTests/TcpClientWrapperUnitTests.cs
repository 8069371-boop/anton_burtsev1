using System;
using System.Net;
using System.Net.Sockets;
using EchoServer.Abstractions;
using FluentAssertions;
using NUnit.Framework;

namespace EchoServer.Tests.Abstractions
{
    [TestFixture]
    public class TcpClientWrapperUnitTests
    {
        private TcpClientWrapper? _sut;
        private TcpClient? _testClient;
        private TcpListener? _testListener;

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
            _testClient?.Dispose();
            
            if (_testListener != null)
            {
                _testListener.Stop();
                _testListener.Server?.Dispose();
                // Виправлення: додано Dispose для _testListener
                (_testListener as IDisposable)?.Dispose();
            }
        }

        [Test]
        public void Constructor_WithValidTcpClient_ShouldCreateInstance()
        {
            // Arrange
            _testClient = new TcpClient();

            // Act
            _sut = new TcpClientWrapper(_testClient);

            // Assert
            _sut.Should().NotBeNull();
            _sut.Should().BeAssignableTo<ITcpClientWrapper>();
        }

        [Test]
        public void Constructor_WithNullTcpClient_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () => _sut = new TcpClientWrapper(null!);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void GetStream_WhenConnected_ShouldReturnNetworkStreamWrapper()
        {
            // Arrange
            _testListener = new TcpListener(IPAddress.Loopback, 0);
            _testListener.Start();
            var port = ((IPEndPoint)_testListener.LocalEndpoint).Port;

            _testClient = new TcpClient();
            _testClient.Connect(IPAddress.Loopback, port);
            _sut = new TcpClientWrapper(_testClient);

            // Act
            var result = _sut.GetStream();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<INetworkStreamWrapper>();
        }

        [Test]
        public void GetStream_CalledMultipleTimes_ShouldReturnNewInstanceEachTime()
        {
            // Arrange
            _testListener = new TcpListener(IPAddress.Loopback, 0);
            _testListener.Start();
            var port = ((IPEndPoint)_testListener.LocalEndpoint).Port;

            _testClient = new TcpClient();
            _testClient.Connect(IPAddress.Loopback, port);
            _sut = new TcpClientWrapper(_testClient);

            // Act
            var stream1 = _sut.GetStream();
            var stream2 = _sut.GetStream();

            // Assert
            stream1.Should().NotBeSameAs(stream2);
        }

        [Test]
        public void Close_WhenCalled_ShouldCloseConnection()
        {
            // Arrange
            _testListener = new TcpListener(IPAddress.Loopback, 0);
            _testListener.Start();
            var port = ((IPEndPoint)_testListener.LocalEndpoint).Port;

            _testClient = new TcpClient();
            _testClient.Connect(IPAddress.Loopback, port);
            _sut = new TcpClientWrapper(_testClient);

            // Act
            _sut.Close();

            // Assert
            _testClient.Connected.Should().BeFalse();
        }

        [Test]
        public void Close_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            _testClient = new TcpClient();
            _sut = new TcpClientWrapper(_testClient);

            // Act
            Action act = () =>
            {
                _sut.Close();
                _sut.Close();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void Dispose_WhenCalled_ShouldDisposeClient()
        {
            // Arrange
            _testListener = new TcpListener(IPAddress.Loopback, 0);
            _testListener.Start();
            var port = ((IPEndPoint)_testListener.LocalEndpoint).Port;

            _testClient = new TcpClient();
            _testClient.Connect(IPAddress.Loopback, port);
            _sut = new TcpClientWrapper(_testClient);
            
            var wasConnected = _testClient.Connected;

            // Act
            _sut.Dispose();

            // Assert
            wasConnected.Should().BeTrue();
            _testClient.Connected.Should().BeFalse();
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            _testClient = new TcpClient();
            _sut = new TcpClientWrapper(_testClient);

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
        public void GetStream_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _testClient = new TcpClient();
            _sut = new TcpClientWrapper(_testClient);
            _sut.Dispose();

            // Act
            Action act = () => _sut.GetStream();

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void Close_AfterDispose_ShouldNotThrow()
        {
            // Arrange
            _testClient = new TcpClient();
            _sut = new TcpClientWrapper(_testClient);
            _sut.Dispose();

            // Act
            Action act = () => _sut.Close();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void GetStream_WhenNotConnected_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _testClient = new TcpClient();
            _sut = new TcpClientWrapper(_testClient);

            // Act
            Action act = () => _sut.GetStream();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Wrapper_ShouldProperlyWrapTcpClientBehavior()
        {
            // Arrange
            _testListener = new TcpListener(IPAddress.Loopback, 0);
            _testListener.Start();
            var port = ((IPEndPoint)_testListener.LocalEndpoint).Port;

            _testClient = new TcpClient();
            
            // Act
            _testClient.Connect(IPAddress.Loopback, port);
            _sut = new TcpClientWrapper(_testClient);
            var stream = _sut.GetStream();

            // Assert
            _testClient.Connected.Should().BeTrue();
            stream.Should().NotBeNull();
        }

        [Test]
        public void Dispose_WithNullClient_ShouldHandleGracefully()
        {
            // Arrange
            _sut = new TcpClientWrapper(null!);

            // Act
            Action act = () => _sut.Dispose();

            // Assert
            act.Should().NotThrow();
        }
    }
}
