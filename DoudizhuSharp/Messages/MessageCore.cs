//#define Test
using System;
using System.Collections.Generic;
using System.Text;

namespace DoudizhuSharp.Messages
{
    public class MessageCore
    {
        public static Action<string, string> PrivateSender { get; set; }
        public static Action<string, string> GroupSender { get; set; }
        public static Func<string, string> AtCoder { get; set; }
        public static Func<string, string> RemoveAtCoder { get; set; }
        public static void ProcessMessage(Message message)
        {
#if !Test
            try
            {
#endif

                if (!CommandHelper.Invoke(message) && GamesManager.Games.ContainsKey(message.Group))
                {
                    var game = GamesManager.Games[message.Group];
                    if (game.ContainsPlayer(message.Sender) && game.CurrentPlayer.PlayerID == message.Sender && game.State == GameState.Gaming)
                    {
                        game.PlayerSendCard(game.CurrentPlayer, message.ParseCards());
                    }
                }
#if !Test

            }
            catch (DoudizhuCardParseException e)
            {
                message.Group.GetGroupSender().Send($"错误:{e.Message}");

            }
            catch (DoudizhuCommandParseException e)
            {
                message.Group.GetGroupSender().Send($"错误:{e.Message}");

            }
            catch (Exception e)
            {
                message.Group.GetGroupSender().Send($"错误:{e}");
            }
#endif

        }

    }
    public abstract class TargetSender
    {
        public abstract void Send(string content);
    }

    public class GroupSender : TargetSender
    {
        public string Target { get; }

        public GroupSender(string target)
        {
            Target = target;
        }

        public override void Send(string content)
        {
            MessageCore.GroupSender(Target, content);
        }
    }

    public class PrivateSender : TargetSender
    {
        public string Target { get; }

        public PrivateSender(string target)
        {
            Target = target;
        }

        public override void Send(string content)
        {
            MessageCore.PrivateSender(Target, content);
        }
    }

    public abstract class Sender
    {
        public abstract void Send(string target, string content);
    }
}
