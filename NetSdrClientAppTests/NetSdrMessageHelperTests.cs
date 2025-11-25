using NetSdrClientApp.Messages;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

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
            var type = MsgTypes.Ack;
            var code = ControlItemCodes.ReceiverState;
            int parametersLength = 7500;
            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);
            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());
            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes, Has.Count.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(actualCode, Is.EqualTo((short)code));
                Assert.That(parametersBytes, Has.Count.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = MsgTypes.DataItem2;
            int parametersLength = 7500;
            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);
            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes, Has.Count.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(parametersBytes, Has.Count.EqualTo(parametersLength));
            });
        }

        [Test]
        public void TranslateMessage_WithControlItem_ShouldDecodeCorrectly()
        {
            // Arrange
            var expectedType = MsgTypes.SetControlItem;
            var expectedItemCode = ControlItemCodes.ReceiverFrequency;
            var parameters = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var message = NetSdrMessageHelper.GetControlItemMessage(expectedType, expectedItemCode, parameters);

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                message, 
                out MsgTypes actualType, 
                out ControlItemCodes actualItemCode, 
                out ushort sequenceNumber, 
                out byte[] body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualType, Is.EqualTo(expectedType));
                Assert.That(actualItemCode, Is.EqualTo(expectedItemCode));
                Assert.That(body, Is.EqualTo(parameters));
            });
        }

        [Test]
        public void TranslateMessage_WithDataItem_ShouldDecodeCorrectly()
        {
            // Arrange
            var expectedType = MsgTypes.DataItem1;
            var parameters = new byte[] { 0x01, 0x00, 0x03, 0x04, 0x05, 0x06 }; // перші 2 байти - sequence number
            var message = NetSdrMessageHelper.GetDataItemMessage(expectedType, parameters);

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                message,
                out MsgTypes actualType,
                out ControlItemCodes itemCode,
                out ushort sequenceNumber,
                out byte[] body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualType, Is.EqualTo(expectedType));
                Assert.That(itemCode, Is.EqualTo(ControlItemCodes.None));
                // Перші 2 байти стають sequence number, решта - body
                Assert.That(body, Has.Length.EqualTo(parameters.Length - 2));
            });
        }

        [Test]
        public void TranslateMessage_RoundTrip_AllControlItemCodes()
        {
            // Arrange
            var controlItemCodes = new[]
            {
                ControlItemCodes.IQOutputDataSampleRate,
                ControlItemCodes.RFFilter,
                ControlItemCodes.ADModes,
                ControlItemCodes.ReceiverState,
                ControlItemCodes.ReceiverFrequency
            };

            foreach (var expectedCode in controlItemCodes)
            {
                var parameters = new byte[] { 0xAA, 0xBB };
                var message = NetSdrMessageHelper.GetControlItemMessage(
                    MsgTypes.CurrentControlItem, 
                    expectedCode, 
                    parameters);

                // Act
                var result = NetSdrMessageHelper.TranslateMessage(
                    message,
                    out MsgTypes type,
                    out ControlItemCodes actualCode,
                    out ushort sequenceNumber,
                    out byte[] body);

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.True, $"Failed for code: {expectedCode}");
                    Assert.That(actualCode, Is.EqualTo(expectedCode), $"Code mismatch for: {expectedCode}");
                    Assert.That(body, Is.EqualTo(parameters), $"Body mismatch for: {expectedCode}");
                });
            }
        }

        [Test]
        public void GetControlItemMessage_WithEmptyParameters_ShouldWork()
        {
            // Arrange
            var type = MsgTypes.SetControlItem;
            var code = ControlItemCodes.ReceiverState;

            // Act
            var message = NetSdrMessageHelper.GetControlItemMessage(type, code, Array.Empty<byte>());

            // Assert
            Assert.That(message, Has.Length.EqualTo(4)); // 2 header + 2 item code
        }

        [Test]
        public void GetDataItemMessage_WithEmptyParameters_ShouldWork()
        {
            // Arrange
            var type = MsgTypes.DataItem0;

            // Act
            var message = NetSdrMessageHelper.GetDataItemMessage(type, Array.Empty<byte>());

            // Assert
            Assert.That(message, Has.Length.EqualTo(2)); // Only header
        }

        [Test]
        public void GetControlItemMessage_WithMaxLength_ShouldNotThrow()
        {
            // Arrange
            var type = MsgTypes.SetControlItem;
            var code = ControlItemCodes.ReceiverState;
            // 8191 max - 2 header - 2 itemCode = 8187
            var parameters = new byte[8187];

            // Act & Assert
            Assert.DoesNotThrow(() => 
                NetSdrMessageHelper.GetControlItemMessage(type, code, parameters));
        }

        [Test]
        public void GetControlItemMessage_ExceedsMaxLength_ShouldThrow()
        {
            // Arrange
            var type = MsgTypes.SetControlItem;
            var code = ControlItemCodes.ReceiverState;
            // 8191 max - 2 header - 2 itemCode + 1 = 8188 (перевищує ліміт)
            var parameters = new byte[8188];

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                NetSdrMessageHelper.GetControlItemMessage(type, code, parameters));
        }

        [Test]
        public void GetDataItemMessage_WithMaxLength_ShouldHandleEdgeCase()
        {
            // Arrange
            var type = MsgTypes.DataItem0;
            var parameters = new byte[8192]; // Max for data item

            // Act
            var message = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Assert
            Assert.That(message, Has.Length.EqualTo(8194)); // 8192 + 2 header
        }

        [Test]
        public void TranslateMessage_WithMaxLengthDataItem_ShouldDecodeCorrectly()
        {
            // Arrange
            var type = MsgTypes.DataItem3;
            var parameters = new byte[8192];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = (byte)(i % 256);
            }
            var message = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                message,
                out MsgTypes actualType,
                out ControlItemCodes itemCode,
                out ushort sequenceNumber,
                out byte[] body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualType, Is.EqualTo(type));
                // Body має бути 8190 тому що 2 байти йдуть на sequence number
                Assert.That(body, Has.Length.EqualTo(8190));
            });
        }

        [Test]
        public void TranslateMessage_WithInvalidMessage_ShouldReturnFalse()
        {
            // Arrange - створюємо дуже короткий масив який не може бути валідним
            var invalidMessage = new byte[] { 0xFF };

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                invalidMessage,
                out MsgTypes type,
                out ControlItemCodes itemCode,
                out ushort sequenceNumber,
                out byte[] body);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TranslateMessage_WithInvalidControlItemCode_ShouldReturnFalse()
        {
            // Arrange
            var type = MsgTypes.SetControlItem;
            var invalidCode = (ushort)0xFFFF; // Invalid code
            var parameters = new byte[] { 0x01, 0x02 };
            
            // Manually create message with invalid code
            var header = BitConverter.GetBytes((ushort)(4 + parameters.Length + ((int)type << 13)));
            var codeBytes = BitConverter.GetBytes(invalidCode);
            var message = header.Concat(codeBytes).Concat(parameters).ToArray();

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                message,
                out MsgTypes actualType,
                out ControlItemCodes itemCode,
                out ushort sequenceNumber,
                out byte[] body);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetSamples_With8BitSamples_ShouldReturnCorrectValues()
        {
            // Arrange
            ushort sampleSize = 8; // 8 bits = 1 byte
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Count.EqualTo(4));
                Assert.That(samples[0], Is.EqualTo(1));
                Assert.That(samples[1], Is.EqualTo(2));
                Assert.That(samples[2], Is.EqualTo(3));
                Assert.That(samples[3], Is.EqualTo(4));
            });
        }

        [Test]
        public void GetSamples_With16BitSamples_ShouldReturnCorrectValues()
        {
            // Arrange
            ushort sampleSize = 16; // 16 bits = 2 bytes
            var body = new byte[] { 0x01, 0x00, 0x02, 0x00 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Count.EqualTo(2));
                Assert.That(samples[0], Is.EqualTo(1));
                Assert.That(samples[1], Is.EqualTo(2));
            });
        }

        [Test]
        public void GetSamples_With24BitSamples_ShouldReturnCorrectValues()
        {
            // Arrange
            ushort sampleSize = 24; // 24 bits = 3 bytes
            var body = new byte[] { 0x01, 0x00, 0x00, 0x02, 0x00, 0x00 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetSamples_With32BitSamples_ShouldReturnCorrectValues()
        {
            // Arrange
            ushort sampleSize = 32; // 32 bits = 4 bytes
            var body = BitConverter.GetBytes(12345).Concat(BitConverter.GetBytes(67890)).ToArray();

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Count.EqualTo(2));
                Assert.That(samples[0], Is.EqualTo(12345));
                Assert.That(samples[1], Is.EqualTo(67890));
            });
        }

        [Test]
        public void GetSamples_WithInvalidSampleSize_ShouldThrowException()
        {
            // Arrange
            ushort sampleSize = 40; // 40 bits = 5 bytes (exceeds max 4)
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetSdrMessageHelper.GetSamples(sampleSize, body).ToList());
        }

        [Test]
        public void GetSamples_WithPartialSample_ShouldIgnoreIncompleteData()
        {
            // Arrange
            ushort sampleSize = 16; // 16 bits = 2 bytes
            var body = new byte[] { 0x01, 0x00, 0x02 }; // Last byte is incomplete

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples, Has.Count.EqualTo(1)); // Only one complete sample
        }

        [Test]
        public void TranslateMessage_LargeBody_ShouldHandleCorrectly()
        {
            // Arrange
            var type = MsgTypes.CurrentControlItem;
            var code = ControlItemCodes.IQOutputDataSampleRate;
            var parameters = new byte[5000];
            new Random().NextBytes(parameters);
            var message = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                message,
                out MsgTypes actualType,
                out ControlItemCodes actualCode,
                out ushort sequenceNumber,
                out byte[] body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualType, Is.EqualTo(type));
                Assert.That(actualCode, Is.EqualTo(code));
                Assert.That(body, Has.Length.EqualTo(parameters.Length));
            });
        }

        [Test]
        public void GetControlItemMessage_AllMsgTypes_ShouldWork()
        {
            // Arrange & Act & Assert
            var types = new[] 
            { 
                MsgTypes.SetControlItem, 
                MsgTypes.CurrentControlItem, 
                MsgTypes.ControlItemRange, 
                MsgTypes.Ack 
            };

            foreach (var type in types)
            {
                var message = NetSdrMessageHelper.GetControlItemMessage(
                    type, 
                    ControlItemCodes.ReceiverState, 
                    new byte[] { 0x01 });
                
                Assert.That(message, Has.Length.GreaterThan(0), $"Failed for type: {type}");
            }
        }

        [Test]
        public void GetDataItemMessage_AllDataItemTypes_ShouldWork()
        {
            // Arrange & Act & Assert
            var types = new[] 
            { 
                MsgTypes.DataItem0, 
                MsgTypes.DataItem1, 
                MsgTypes.DataItem2, 
                MsgTypes.DataItem3 
            };

            foreach (var type in types)
            {
                var message = NetSdrMessageHelper.GetDataItemMessage(
                    type, 
                    new byte[] { 0x01, 0x02 });
                
                Assert.That(message, Has.Length.GreaterThan(0), $"Failed for type: {type}");
            }
        }

        [Test]
        public void TranslateMessage_DataItemWithSequenceNumber_ShouldDecodeSequenceNumber()
        {
            // Arrange
            var type = MsgTypes.DataItem2;
            ushort expectedSequenceNumber = 1234;
            var parameters = BitConverter.GetBytes(expectedSequenceNumber).Concat(new byte[] { 0xAA, 0xBB }).ToArray();
            var message = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Act
            var result = NetSdrMessageHelper.TranslateMessage(
                message,
                out MsgTypes actualType,
                out ControlItemCodes itemCode,
                out ushort actualSequenceNumber,
                out byte[] body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualSequenceNumber, Is.EqualTo(expectedSequenceNumber));
            });
        }

        [Test]
        public void GetSamples_EmptyBody_ShouldReturnEmptyEnumerable()
        {
            // Arrange
            ushort sampleSize = 16;
            var body = Array.Empty<byte>();

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples, Has.Count.EqualTo(0));
        }
    }
}
