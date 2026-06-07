using FluentAssertions;
using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using Xunit;

namespace server.tests;

public class GlobalChatRoomTests
{
    [Fact]
    public void PostMessage_ValidText_AddsMessage()
    {
        var room = new GlobalChatRoom();
        var senderId = new PlayerId(Guid.NewGuid());

        room.PostMessage(senderId, "こんにちは");

        room.Messages.Should().ContainSingle();
        room.Messages[0].SenderType.Should().Be(ChatMessageSenderType.Player);
        room.Messages[0].SenderId.Should().Be(senderId);
        room.Messages[0].Body.Text.Should().Be("こんにちは");
        room.Messages[0].ChatId.Should().Be(1);
        room.LastChatId.Should().Be(1);
    }

    [Fact]
    public void PostSystemMessage_AddsSystemMessage()
    {
        var room = new GlobalChatRoom();

        room.PostSystemMessage("システムメッセージ");

        room.Messages.Should().ContainSingle();
        room.Messages[0].SenderType.Should().Be(ChatMessageSenderType.System);
        room.Messages[0].SenderId.Should().BeNull();
        room.Messages[0].Body.Text.Should().Be("システムメッセージ");
    }

    [Fact]
    public void PostMessage_IsAlerted_AlwaysFalse()
    {
        var room = new GlobalChatRoom();
        var senderId = new PlayerId(Guid.NewGuid());

        room.PostMessage(senderId, "テスト");

        room.Messages[0].IsAlerted.Should().BeFalse();
    }

    [Fact]
    public void EnforceMessageLimit_Over100Messages_RemovesOldest()
    {
        var room = new GlobalChatRoom();
        var senderId = new PlayerId(Guid.NewGuid());

        for (var i = 0; i < ChatConstants.MessageLimit + 1; i++)
        {
            room.PostMessage(senderId, $"メッセージ{i + 1}");
        }

        room.Messages.Should().HaveCount(ChatConstants.MessageLimit);
        room.Messages[0].Body.Text.Should().Be("メッセージ2");
        room.Messages[^1].Body.Text.Should().Be($"メッセージ{ChatConstants.MessageLimit + 1}");
    }

    [Fact]
    public void GetNextMessageId_StartsAt1()
    {
        var room = new GlobalChatRoom();

        room.GetNextMessageId().Should().Be(1);
    }

    [Fact]
    public void GetNextMessageId_IncrementsAfterPost()
    {
        var room = new GlobalChatRoom();
        var senderId = new PlayerId(Guid.NewGuid());

        room.PostMessage(senderId, "テスト");

        room.GetNextMessageId().Should().Be(2);
    }
}
