using Hue_travel.Api.Chat;

namespace Hue_travel.Api.Tests;

public sealed class DeepSeekSessionTests
{
    [Fact]
    public void SessionId_StaysTheSameThroughoutAConversation_AndDiffersBetweenConversations()
    {
        var first = new List<ModelMessage> { ModelMessage.System("s"), new("user", "Đại Nội mở cửa mấy giờ?") };
        var later = new List<ModelMessage>(first) { new("assistant", "8h"), new("user", "còn giá vé?") };
        var other = new List<ModelMessage> { ModelMessage.System("s"), new("user", "gợi ý lịch trình 1 ngày") };

        Assert.Equal(DeepSeekClient.SessionId(first), DeepSeekClient.SessionId(later));
        Assert.NotEqual(DeepSeekClient.SessionId(first), DeepSeekClient.SessionId(other));
    }

    [Fact]
    public void SessionId_DoesNotContainWhatThePersonSaid()
    {
        string id = DeepSeekClient.SessionId(new List<ModelMessage> { new("user", "Đại Nội") });
        Assert.DoesNotContain("Đại", id);
        Assert.Matches("^hue-[0-9a-f]{24}$", id);
    }
}
