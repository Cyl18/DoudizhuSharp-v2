using System;
using System.Collections.Generic;
using System.Text;

namespace DoudizhuSharp.Messages
{
    public class Message
    {
        public string Group { get; }
        public string Sender { get; }
        public string Content { get; }
        public string ID { get; }

        public Message(string @group, string sender, string content, string id = "")
        {
            Group = @group;
            Sender = sender;
            Content = content;
            ID = id;
        }
    }

    public static class MessageExtensions
    {
        public static TargetSender GetGroupSender(this Message message)
        {
            return new GroupSender(message.Group);
        }

        public static TargetSender GetPrivateSender(this Message message)
        {
            return new PrivateSender(message.Sender);
        }

        public static TargetSender GetGroupSender(this string id)
        {
            return new GroupSender(id);
        }

        public static TargetSender GetPrivateSender(this string id)
        {
            return new PrivateSender(id);
        }
    }
}
