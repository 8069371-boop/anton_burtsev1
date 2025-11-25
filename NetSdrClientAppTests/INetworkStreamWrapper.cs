using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;

[TestClass]
public class NetworkStreamWrapperTests
{
    [TestMethod]
    public async Task ReadAsync_Should_Call_Underlying_Stream()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        byte[] buffer = new byte[4];

        mockStream
            .Setup(s => s.ReadAsync(buffer, 0, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        int result = await wrapper.ReadAsync(buffer, 0, 4, CancellationToken.None);

        // Assert
        Assert.AreEqual(4, result);
        mockStream.Verify(s => s.ReadAsync(buffer, 0, 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task WriteAsync_Should_Call_Underlying_Stream()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        byte[] buffer = new byte[4];

        mockStream
            .Setup(s => s.WriteAsync(buffer, 0, 4, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        await wrapper.WriteAsync(buffer, 0, 4, CancellationToken.None);

        // Assert
        mockStream.Verify(s => s.WriteAsync(buffer, 0, 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void Dispose_Should_Dispose_Inner_Stream()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);

        mockStream.Setup(s => s.Dispose());

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        wrapper.Dispose();

        // Assert
        mockStream.Verify(s => s.Dispose(), Times.Once);
    }
}
