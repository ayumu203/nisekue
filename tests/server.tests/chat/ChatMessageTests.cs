using FluentAssertions;
using server.domain.chat;
using server.domain.player;
using Xunit;

namespace server.tests;

public class ChatMessageTests
{
    [Fact]
    public void CanBeDeletedBy_Sender_ReturnsTrue()
    {
        var senderId = new PlayerId(Guid.NewGuid());
        var message = CreatePlayerMessage(senderId);

        message.CanBeDeletedBy(senderId).Should().BeTrue();
    }

    [Fact]
    public void CanBeDeletedBy_OtherPlayer_ReturnsFalse()
    {
        var message = CreatePlayerMessage(new PlayerId(Guid.NewGuid()));

        message.CanBeDeletedBy(new PlayerId(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public void CanBeDeletedBy_SystemMessage_ReturnsFalse()
    {
        var message = new ChatMessage(
            ChatMessageSenderType.System, senderId: null, chatId: 1, new ChatText("システム通知"), DateTimeOffset.UtcNow, isAlerted: false);

        message.CanBeDeletedBy(new PlayerId(Guid.NewGuid())).Should().BeFalse();
    }

    private static ChatMessage CreatePlayerMessage(PlayerId senderId) =>
        new(ChatMessageSenderType.Player, senderId, chatId: 1, new ChatText("こんにちは"), DateTimeOffset.UtcNow, isAlerted: false);
}
