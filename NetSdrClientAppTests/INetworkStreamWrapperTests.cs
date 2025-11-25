using System.IO;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using EchoTcpServer.Abstractions;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetworkStreamWrapperTests
    {
        private Mock<Stream> _mockStream = null!;
        private NetworkStreamWrapper _wrapper = null!;

        [SetUp]
        public void Setup()
        {
            _mockStream = new Mock<Stream>();
            _wrapper = new NetworkStreamWrapper(_mockStream.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _wrapper.Dispose();
        }

        [Test]
        public async Task ReadAsync_Should_Call_Underlying_Stream()
        {
            var buffer = new byte[10];
            _mockStream.Setup(s => s.ReadAsync(buffer, 0, buffer.Length, default))
                       .ReturnsAsync(buffer.Length)
                       .Verifiable();

            var result = await _wrapper.ReadAsync(buffer, 0, buffer.Length);

            Assert.AreEqual(buffer.Length, result);
            _mockStream.Verify();
        }

        [Test]
        public async Task WriteAsync_Should_Call_Underlying_Stream()
        {
            var buffer = new byte[10];
            _mockStream.Setup(s => s.WriteAsync(buffer, 0, buffer.Length, default))
                       .Returns(Task.CompletedTask)
                       .Verifiable();

            await _wrapper.WriteAsync(buffer, 0, buffer.Length);

            _mockStream.Verify();
        }

        [Test]
        public void Dispose_Should_Dispose_Inner_Stream()
        {
            _wrapper.Dispose();
            _mockStream.Verify(s => s.Dispose(), Times.Once);
        }
    }
}
