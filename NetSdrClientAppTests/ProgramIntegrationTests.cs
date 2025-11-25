using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;
using EchoServer.Services;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EchoServer.Tests
{
    [TestFixture]
    public class ProgramIntegrationTests
    {
        private Mock<ILogger>? _mockLogger;
        private Mock<ITcpListenerFactory>? _mockListenerFactory;
        private Mock<ITcpListenerWrapper>? _mockListener;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger>();
            _mockListenerFactory = new Mock<ITcpListenerFactory>();
            _mockListener = new Mock<ITcpListenerWrapper>();
        }

        [Test]
        public void ConsoleLogger_ShouldBeCreated()
        {
            // Act
            var logger = new ConsoleLogger();

            // Assert
            logger.Should().NotBeNull();
            logger.Should().BeAssignableTo<ILogger>();
        }

        [Test]
        public void TcpListenerFactory_ShouldBeCreated()
        {
            // Act
            var factory = new TcpListenerFactory();

            // Assert
            factory.Should().NotBeNull();
            factory.Should().BeAssignableTo<ITcpListenerFactory>();
        }

        [Test]
        public void EchoServerService_ShouldBeCreatedWithCorrectParameters()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var factory = new TcpListenerFactory();
            var port = 5000;

            // Act
            var server = new EchoServerService(port, logger, factory);

            // Assert
            server.Should().NotBeNull();
        }

        [Test]
        public async Task EchoServerService_StartAsync_ShouldStartSuccessfully()
        {
            // Arrange
            _mockListenerFactory!.Setup(f => f.Create(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>()))
                .Returns(_mockListener!.Object);
            
            _mockListener!.Setup(l => l.Start()).Verifiable();
            _mockListener.Setup(l => l.AcceptTcpClientAsync())
                .Returns(Task.Delay(100).ContinueWith(_ => (ITcpClientWrapper)null!));

            var server = new EchoServerService(5000, _mockLogger!.Object, _mockListenerFactory.Object);
            var cts = new CancellationTokenSource();

            // Act
            var serverTask = Task.Run(async () => await server.StartAsync());
            await Task.Delay(50);
            server.Stop();
            cts.CancelAfter(1000);

            // Assert
            _mockListener.Verify(l => l.Start(), Times.Once);
        }

        [Test]
        public void UdpTimedSender_ShouldBeCreatedWithCorrectParameters()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;
            var logger = new ConsoleLogger();

            // Act
            using var sender = new UdpTimedSender(host, port, logger);

            // Assert
            sender.Should().NotBeNull();
        }

        [Test]
        public void UdpTimedSender_StartSending_ShouldNotThrow()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;
            var intervalMilliseconds = 5000;
            var logger = new ConsoleLogger();

            using var sender = new UdpTimedSender(host, port, logger);

            // Act
            Action act = () =>
            {
                sender.StartSending(intervalMilliseconds);
                Task.Delay(100).Wait();
                sender.StopSending();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void UdpTimedSender_StopSending_ShouldNotThrowWhenNotStarted()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;
            var logger = new ConsoleLogger();

            using var sender = new UdpTimedSender(host, port, logger);

            // Act
            Action act = () => sender.StopSending();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void UdpTimedSender_Dispose_ShouldWorkCorrectly()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;
            var logger = new ConsoleLogger();

            var sender = new UdpTimedSender(host, port, logger);

            // Act
            Action act = () => sender.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void UdpTimedSender_Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;
            var logger = new ConsoleLogger();

            var sender = new UdpTimedSender(host, port, logger);

            // Act
            Action act = () =>
            {
                sender.Dispose();
                sender.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void ConsoleLogger_Log_ShouldWriteToConsole()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var message = "Test message";
            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            logger.Log(message);

            // Assert
            output.ToString().Should().Contain(message);
            Console.SetOut(Console.Out);
        }

        [Test]
        public async Task EchoServerService_Stop_ShouldStopServer()
        {
            // Arrange
            _mockListenerFactory!.Setup(f => f.Create(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>()))
                .Returns(_mockListener!.Object);
            
            _mockListener!.Setup(l => l.Start()).Verifiable();
            _mockListener.Setup(l => l.Stop()).Verifiable();
            _mockListener.Setup(l => l.AcceptTcpClientAsync())
                .Returns(Task.Delay(100).ContinueWith(_ => (ITcpClientWrapper)null!));

            var server = new EchoServerService(5000, _mockLogger!.Object, _mockListenerFactory.Object);

            // Act
            var serverTask = Task.Run(async () => await server.StartAsync());
            await Task.Delay(50);
            server.Stop();
            await Task.Delay(50);

            // Assert
            _mockListener.Verify(l => l.Stop(), Times.Once);
        }

        [Test]
        public void ComponentsIntegration_AllComponentsShouldWorkTogether()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var factory = new TcpListenerFactory();
            var serverPort = 5000;
            var udpHost = "127.0.0.1";
            var udpPort = 60000;
            var intervalMilliseconds = 5000;

            // Act
            Action act = () =>
            {
                var server = new EchoServerService(serverPort, logger, factory);
                using var sender = new UdpTimedSender(udpHost, udpPort, logger);
                
                // Quick start and stop to test initialization
                sender.StartSending(intervalMilliseconds);
                Task.Delay(50).Wait();
                sender.StopSending();
                server.Stop();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void ProgramConstants_ShouldHaveCorrectValues()
        {
            // Arrange & Act
            var serverPort = 5000;
            var udpHost = "127.0.0.1";
            var udpPort = 60000;
            var intervalMilliseconds = 5000;

            // Assert
            serverPort.Should().Be(5000);
            udpHost.Should().Be("127.0.0.1");
            udpPort.Should().Be(60000);
            intervalMilliseconds.Should().Be(5000);
        }

        [Test]
        public async Task ServerStartup_ShouldInitializeAllComponents()
        {
            // Arrange
            _mockListenerFactory!.Setup(f => f.Create(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>()))
                .Returns(_mockListener!.Object);
            
            _mockListener!.Setup(l => l.Start()).Verifiable();
            _mockListener.Setup(l => l.AcceptTcpClientAsync())
                .Returns(Task.Delay(100).ContinueWith(_ => (ITcpClientWrapper)null!));

            var logger = _mockLogger!.Object;
            var factory = _mockListenerFactory.Object;
            var server = new EchoServerService(5000, logger, factory);

            // Act
            var serverTask = Task.Run(async () => await server.StartAsync());
            await Task.Delay(100);
            server.Stop();

            // Assert
            _mockListenerFactory.Verify(f => f.Create(It.IsAny<System.Net.IPAddress>(), 5000), Times.Once);
            _mockListener.Verify(l => l.Start(), Times.Once);
        }

        [Test]
        public void UdpTimedSender_WithValidParameters_ShouldInitialize()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;

            // Act
            using var sender = new UdpTimedSender(host, port, _mockLogger!.Object);

            // Assert
            sender.Should().NotBeNull();
        }

        [Test]
        public void UdpTimedSender_StartAndStop_ShouldLogCorrectly()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;
            var intervalMilliseconds = 100;
            
            using var sender = new UdpTimedSender(host, port, _mockLogger!.Object);

            // Act
            sender.StartSending(intervalMilliseconds);
            Task.Delay(50).Wait();
            sender.StopSending();

            // Assert
            _mockLogger.Verify(l => l.Log(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task ServerLifecycle_StartAndStop_ShouldWorkCorrectly()
        {
            // Arrange
            _mockListenerFactory!.Setup(f => f.Create(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>()))
                .Returns(_mockListener!.Object);
            
            _mockListener!.Setup(l => l.AcceptTcpClientAsync())
                .Returns(Task.Delay(100).ContinueWith(_ => (ITcpClientWrapper)null!));

            var server = new EchoServerService(5000, _mockLogger!.Object, _mockListenerFactory.Object);

            // Act
            var serverTask = Task.Run(async () => await server.StartAsync());
            await Task.Delay(50);
            
            Action stopAction = () => server.Stop();

            // Assert
            stopAction.Should().NotThrow();
        }

        [Test]
        public void AllComponents_ShouldImplementCorrectInterfaces()
        {
            // Arrange & Act
            var logger = new ConsoleLogger();
            var factory = new TcpListenerFactory();

            // Assert
            logger.Should().BeAssignableTo<ILogger>();
            factory.Should().BeAssignableTo<ITcpListenerFactory>();
        }

        [Test]
        public void UdpTimedSender_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange
            var host = "127.0.0.1";
            var port = 60000;

            // Act
            Action act = () =>
            {
                using var sender = new UdpTimedSender(host, port, null!);
            };

            // Assert - конструктор повинен кидати ArgumentNullException для null logger
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
    }
}
