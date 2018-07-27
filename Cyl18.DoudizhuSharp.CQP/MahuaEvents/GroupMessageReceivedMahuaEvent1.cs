using Newbe.Mahua.MahuaEvents;
using System;
using DoudizhuSharp.Messages;
using Newbe.Mahua;

namespace Cyl18.DoudizhuSharp.CQP.MahuaEvents
{
    /// <summary>
    /// 群消息接收事件
    /// </summary>
    public class GroupMessageReceivedMahuaEvent1
        : IGroupMessageReceivedMahuaEvent
    {
        private readonly IMahuaApi _mahuaApi;

        public GroupMessageReceivedMahuaEvent1(
            IMahuaApi mahuaApi)
        {
            _mahuaApi = mahuaApi;
            MessageCore.AtCoder = qq => $"[CQ:at,qq={qq}]";
            MessageCore.GroupSender = (target, content) =>
            {
                using (var robotSession = MahuaRobotManager.Instance.CreateSession())
                {
                    var api = robotSession.MahuaApi;
                    api.SendGroupMessage(target, content);
                }
            };
            MessageCore.PrivateSender = (target, content) =>
            {
                using (var robotSession = MahuaRobotManager.Instance.CreateSession())
                {
                    var api = robotSession.MahuaApi;
                    api.SendPrivateMessage(target, content);
                }
            };
        }

        public void ProcessGroupMessage(GroupMessageReceivedContext context)
        {
            MessageCore.ProcessMessage(new Message(context.FromGroup, context.FromQq, context.Message));
        }
    }
}
