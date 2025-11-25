using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;
            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);
            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());
            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(actualCode, Is.EqualTo((short)code));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;
            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);
            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetControlItemMessage_WithEmptyParameters_ShouldWork()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            var parameters = Array.Empty<byte>();

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            //Assert
            Assert.That(msg.Length, Is.EqualTo(4)); // Header (2) + ItemCode (2)
        }

        [Test]
        public void GetDataItemMessage_WithEmptyParameters_ShouldWork()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var parameters = Array.Empty<byte>();

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            //Assert
            Assert.That(msg.Length, Is.EqualTo(2)); // Only header
        }

        [Test]
        public void TranslateMessage_WithControlItem_ShouldDecodeCorrectly()
        {
            //Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var originalCode = NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate;
            var originalParams = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var message = NetSdrMessageHelper.GetControlItemMessage(originalType, originalCode, originalParams);

            //Act
            bool success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var type,
                out var itemCode,
                out var sequenceNumber,
                out var body);

            //Assert
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(originalType));
            Assert.That(itemCode, Is.EqualTo(originalCode));
            Assert.That(body, Is.EqualTo(originalParams));
        }

        [Test]
        public void TranslateMessage_WithDataItem_ShouldDecodeCorrectly()
        {
            //Arrange
            var originalType = NetSdrMessageHelper.MsgTypes.DataItem1;
            var originalParams = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            var message = NetSdrMessageHelper.GetDataItemMessage(originalType, originalParams);

            //Act
            bool success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var type,
                out var itemCode,
                out var sequenceNumber,
                out var body);

            //Assert
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(originalType));
            Assert.That(itemCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            Assert.That(sequenceNumber, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TranslateMessage_RoundTrip_AllControlItemCodes()
        {
            //Arrange
            var codes = new[]
            {
                NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate,
                NetSdrMessageHelper.ControlItemCodes.RFFilter,
                NetSdrMessageHelper.ControlItemCodes.ADModes,
                NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency
            };

            foreach (var code in codes)
            {
                var originalParams = new byte[] { 0x01, 0x02 };
                var message = NetSdrMessageHelper.GetControlItemMessage(
                    NetSdrMessageHelper.MsgTypes.CurrentControlItem,
                    code,
                    originalParams);

                //Act
                bool success = NetSdrMessageHelper.TranslateMessage(
                    message,
                    out var type,
                    out var itemCode,
                    out var sequenceNumber,
                    out var body);

                //Assert
                Assert.That(success, Is.True);
                Assert.That(itemCode, Is.EqualTo(code));
                Assert.That(body, Is.EqualTo(originalParams));
            }
        }

        [Test]
        public void TranslateMessage_RoundTrip_AllDataItemTypes()
        {
            //Arrange
            var types = new[]
            {
                NetSdrMessageHelper.MsgTypes.DataItem0,
                NetSdrMessageHelper.MsgTypes.DataItem1,
                NetSdrMessageHelper.MsgTypes.DataItem2,
                NetSdrMessageHelper.MsgTypes.DataItem3
            };

            foreach (var dataType in types)
            {
                var originalParams = new byte[] { 0x10, 0x20, 0x30 };
                var message = NetSdrMessageHelper.GetDataItemMessage(dataType, originalParams);

                //Act
                bool success = NetSdrMessageHelper.TranslateMessage(
                    message,
                    out var type,
                    out var itemCode,
                    out var sequenceNumber,
                    out var body);

                //Assert
                Assert.That(success, Is.True);
                Assert.That(type, Is.EqualTo(dataType));
            }
        }

        [Test]
        public void GetSamples_With8BitSamples_ShouldWork()
        {
            //Arrange
            ushort sampleSize = 8;
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(4));
            Assert.That(samples[0], Is.EqualTo(0x01));
            Assert.That(samples[1], Is.EqualTo(0x02));
            Assert.That(samples[2], Is.EqualTo(0x03));
            Assert.That(samples[3], Is.EqualTo(0x04));
        }

        [Test]
        public void GetSamples_With16BitSamples_ShouldWork()
        {
            //Arrange
            ushort sampleSize = 16;
            var body = new byte[] { 0x01, 0x00, 0x02, 0x00, 0x03, 0x00 };

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(3));
            Assert.That(samples[0], Is.EqualTo(0x0001));
            Assert.That(samples[1], Is.EqualTo(0x0002));
            Assert.That(samples[2], Is.EqualTo(0x0003));
        }

        [Test]
        public void GetSamples_With24BitSamples_ShouldWork()
        {
            //Arrange
            ushort sampleSize = 24;
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetSamples_With32BitSamples_ShouldWork()
        {
            //Arrange
            ushort sampleSize = 32;
            var body = new byte[] { 0x01, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00 };

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(0x00000001));
            Assert.That(samples[1], Is.EqualTo(0x000000FF));
        }

        [Test]
        public void GetSamples_WithInvalidSampleSize_ShouldThrow()
        {
            //Arrange
            ushort sampleSize = 40; // > 32 bits
            var body = new byte[] { 0x01, 0x02 };

            //Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetSdrMessageHelper.GetSamples(sampleSize, body).ToList());
        }

        [Test]
        public void GetSamples_WithEmptyBody_ShouldReturnEmpty()
        {
            //Arrange
            ushort sampleSize = 16;
            var body = Array.Empty<byte>();

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetSamples_WithPartialSample_ShouldIgnoreIncomplete()
        {
            //Arrange
            ushort sampleSize = 16; // 2 bytes per sample
            var body = new byte[] { 0x01, 0x02, 0x03 }; // 1.5 samples

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetControlItemMessage_MaxLength_ShouldThrow()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            var parameters = new byte[8200]; // Exceeds max

            //Act & Assert
            Assert.Throws<ArgumentException>(() =>
                NetSdrMessageHelper.GetControlItemMessage(type, code, parameters));
        }

        [Test]
        public void GetDataItemMessage_MaxLength_EdgeCase_ShouldWork()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var parameters = new byte[8192]; // Max for data items

            //Act
            var msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            //Assert
            Assert.That(msg, Is.Not.Null);
        }

        [Test]
        public void TranslateMessage_WithMaxLengthDataItem_ShouldDecodeCorrectly()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem3;
            var parameters = new byte[8192];
            Array.Fill(parameters, (byte)0xAB);
            var message = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            //Act
            bool success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var decodedType,
                out var itemCode,
                out var sequenceNumber,
                out var body);

            //Assert
            Assert.That(success, Is.True);
            Assert.That(decodedType, Is.EqualTo(type));
            Assert.That(body.Length, Is.EqualTo(8192));
        }

        [Test]
        public void TranslateMessage_WithInvalidMessage_ShouldReturnFalse()
        {
            //Arrange
            var invalidMessage = new byte[] { 0x00 }; // Too short

            //Act
            bool success = NetSdrMessageHelper.TranslateMessage(
                invalidMessage,
                out var type,
                out var itemCode,
                out var sequenceNumber,
                out var body);

            //Assert
            Assert.That(success, Is.False);
        }

        [Test]
        public void GetControlItemMessage_AllMessageTypes_ShouldWork()
        {
            //Arrange
            var types = new[]
            {
                NetSdrMessageHelper.MsgTypes.SetControlItem,
                NetSdrMessageHelper.MsgTypes.CurrentControlItem,
                NetSdrMessageHelper.MsgTypes.ControlItemRange,
                NetSdrMessageHelper.MsgTypes.Ack
            };

            foreach (var type in types)
            {
                var parameters = new byte[] { 0x01, 0x02 };

                //Act
                var msg = NetSdrMessageHelper.GetControlItemMessage(
                    type,
                    NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                    parameters);

                //Assert
                Assert.That(msg, Is.Not.Null);
                Assert.That(msg.Length, Is.GreaterThan(0));
            }
        }

        [Test]
        public void GetSamples_ConsecutiveCalls_ShouldBeConsistent()
        {
            //Arrange
            ushort sampleSize = 16;
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            //Act
            var samples1 = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();
            var samples2 = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples1, Is.EqualTo(samples2));
        }

        [Test]
        public void TranslateMessage_LargeBody_ShouldHandleCorrectly()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.CurrentControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.RFFilter;
            var parameters = new byte[5000];
            Array.Fill(parameters, (byte)0x55);
            var message = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            //Act
            bool success = NetSdrMessageHelper.TranslateMessage(
                message,
                out var decodedType,
                out var decodedCode,
                out var sequenceNumber,
                out var body);

            //Assert
            Assert.That(success, Is.True);
            Assert.That(decodedType, Is.EqualTo(type));
            Assert.That(decodedCode, Is.EqualTo(code));
            Assert.That(body.Length, Is.EqualTo(5000));
            Assert.That(body.All(b => b == 0x55), Is.True);
        }

        [Test]
        public void GetSamples_DifferentSampleSizes_ShouldProcessCorrectly()
        {
            //Arrange
            var sampleSizes = new ushort[] { 8, 16, 24, 32 };
            var body = new byte[96]; // Divisible by 1, 2, 3, and 4

            foreach (var sampleSize in sampleSizes)
            {
                //Act
                var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

                //Assert
                int expectedCount = 96 / (sampleSize / 8);
                Assert.That(samples.Count, Is.EqualTo(expectedCount));
            }
        }

        [Test]
        public void ControlItemCodes_Values_ShouldBeCorrect()
        {
            //Assert
            Assert.That((ushort)NetSdrMessageHelper.ControlItemCodes.None, Is.EqualTo(0));
            Assert.That((ushort)NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate, Is.EqualTo(0x00B8));
            Assert.That((ushort)NetSdrMessageHelper.ControlItemCodes.RFFilter, Is.EqualTo(0x0044));
            Assert.That((ushort)NetSdrMessageHelper.ControlItemCodes.ADModes, Is.EqualTo(0x008A));
            Assert.That((ushort)NetSdrMessageHelper.ControlItemCodes.ReceiverState, Is.EqualTo(0x0018));
            Assert.That((ushort)NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency, Is.EqualTo(0x0020));
        }
    }
}
