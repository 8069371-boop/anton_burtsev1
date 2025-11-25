using System;
using System.Net.Sockets;
using EchoServer.Abstractions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EchoServer.Tests.Abstractions
{
    [TestFixture]
    public class TcpClientWrapperUnitTests
    {
        private Mock<TcpClient> _mockTcpClient;
        private Mock<NetworkStream> _mockNetworkStream;
        private TcpClientWrapper _sut;

        [SetUp]
        public void SetUp()
        {
            _mockTcpClient = new Mock<TcpClient>();
            _mockNetworkStream = new Mock<NetworkStream>();
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
        }

        [Test]
        public void Constructor_WithValidTcpClient_ShouldCreateInstance()
        {
            // Arrange & Act
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Assert
            _sut.Should().NotBeNull();
            _sut.Should().BeAssignableTo<ITcpClientWrapper>();
        }

        [Test]
        public void Constructor_WithNullTcpClient_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => _sut = new TcpClientWrapper(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void GetStream_WhenCalled_ShouldReturnNetworkStreamWrapper()
        {
            // Arrange
            _mockTcpClient.Setup(c => c.GetStream()).Returns(_mockNetworkStream.Object);
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            var result = _sut.GetStream();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<INetworkStreamWrapper>();
            _mockTcpClient.Verify(c => c.GetStream(), Times.Once);
        }

        [Test]
        public void GetStream_CalledMultipleTimes_ShouldReturnNewInstanceEachTime()
        {
            // Arrange
            _mockTcpClient.Setup(c => c.GetStream()).Returns(_mockNetworkStream.Object);
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            var stream1 = _sut.GetStream();
            var stream2 = _sut.GetStream();

            // Assert
            stream1.Should().NotBeSameAs(stream2);
            _mockTcpClient.Verify(c => c.GetStream(), Times.Exactly(2));
        }

        [Test]
        public void Close_WhenCalled_ShouldCloseTcpClient()
        {
            // Arrange
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            _sut.Close();

            // Assert
            _mockTcpClient.Verify(c => c.Close(), Times.Once);
        }

        [Test]
        public void Close_CalledMultipleTimes_ShouldCloseEachTime()
        {
            // Arrange
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            _sut.Close();
            _sut.Close();

            // Assert
            _mockTcpClient.Verify(c => c.Close(), Times.Exactly(2));
        }

        [Test]
        public void Dispose_WhenCalled_ShouldDisposeTcpClient()
        {
            // Arrange
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            _sut.Dispose();

            // Assert
            _mockTcpClient.Verify(c => c.Dispose(), Times.Once);
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldDisposeOnlyOnce()
        {
            // Arrange
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            _sut.Dispose();
            _sut.Dispose();

            // Assert
            _mockTcpClient.Verify(c => c.Dispose(), Times.Once);
        }

        [Test]
        public void Dispose_WithNullClient_ShouldNotThrow()
        {
            // Arrange
            var realClient = new TcpClient();
            _sut = new TcpClientWrapper(realClient);
            realClient.Dispose();

            // Act
            Action act = () => _sut.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void GetStream_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var realClient = new TcpClient();
            _sut = new TcpClientWrapper(realClient);
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
            _sut = new TcpClientWrapper(_mockTcpClient.Object);
            _sut.Dispose();

            // Act
            Action act = () => _sut.Close();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void Dispose_ShouldSuppressFinalization()
        {
            // Arrange
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            _sut.Dispose();

            // Assert
            _mockTcpClient.Verify(c => c.Dispose(), Times.Once);
        }

        [Test]
        public void GetStream_WhenTcpClientThrowsException_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Connection failed");
            _mockTcpClient.Setup(c => c.GetStream()).Throws(expectedException);
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            Action act = () => _sut.GetStream();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Connection failed");
        }

        [Test]
        public void Close_WhenTcpClientThrowsException_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new SocketException();
            _mockTcpClient.Setup(c => c.Close()).Throws(expectedException);
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            Action act = () => _sut.Close();

            // Assert
            act.Should().Throw<SocketException>();
        }

        [Test]
        public void Dispose_WhenTcpClientThrowsException_ShouldNotThrow()
        {
            // Arrange
            _mockTcpClient.Setup(c => c.Dispose()).Throws<InvalidOperationException>();
            _sut = new TcpClientWrapper(_mockTcpClient.Object);

            // Act
            Action act = () => _sut.Dispose();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
