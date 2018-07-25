using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DoudizhuSharp.Extensions;
using DoudizhuSharp.Messages;
using DoudizhuSharp.Rules;
using GammaLibrary.Extensions;
using Humanizer;
using Number = System.Numerics.BigInteger;

namespace DoudizhuSharp
{
    public class Commands
    {
        public static Commands Instance { get; } = new Commands();
        //[RegexParser("^(获取|领取)(分数|积分)$")]
        [Strings("我的信息", "我的积分"), CommandDescription("获取你有多少积分。")]
        string GetPoint([Inject] PlayerConfig config)
        {
            return $"你有 {config.Point}点积分.";
        }

        [RequireAdmin]
        [Strings("设置积分"), CommandDescription("设置指定玩家的积分为指定值。")]
        void SetPoint(
            [Inject] Config config,
            [Inject] TargetSender sender,
            [Parameter("设置的对象")] string target, //TODO 使用@ 和regex replace
            [Parameter("目标分数")] Number point)
        {
            if (!config.PlayerConfigs.ContainsKey(target)) sender.Send("玩家不存在，已经新建配置文件.");
            config.GetPlayerConfig(target).Point = point;
            sender.Send("设置完成.");
        }

        [Strings("新建游戏", "创建游戏"), CommandDescription("在当前群创建一个游戏。")]
        string CreateGame(
            [Inject(Injects.GameID)] string gameid,
            [Parameter("游戏类型")] string type = "普通场")
        {
            GamesManager.Games[gameid] = new Game(gameid);
            return "完成";
        }

        [RequireAdmin]
        [Strings("启用调试"), CommandDescription("在一段时间内启用调试模式。")]
        string EnableDebug()
        {
            DebugHelper.EnableDebug = true;
            Task.Delay(5.Minutes())
                .ContinueWith(task => DebugHelper.EnableDebug = false);
            return "在5分钟内启用了调试模式。";
        }

        [Strings("帮助", "命令列表", "指令列表"), CommandDescription("你想要帮助的帮助???")]
        string Help()
        {
            return CommandHelper.AllCommands.Select(c => c.MethodInfo.GetCommandHelp()).StringJoin("\r\n");
        }

        [Strings("保存设置"), CommandDescription("保存当前设置。")]
        string SaveConfig()
        {
            Config.Save();
            return "完成";
        }

        [Strings("加载设置"), CommandDescription("加载当前设置。")]
        string LoadConfig()
        {
            Config.Update();
            return "完成";
        }
    }

    public struct CommandInfo
    {
        public MethodInfo MethodInfo;
        public bool RequireAdmin;
        public Regex Regex;
        public string[] ValidateStrings;

        public CommandInfo(MethodInfo methodInfo, bool requireAdmin, Regex regex, string[] validateStrings)
        {
            MethodInfo = methodInfo;
            RequireAdmin = requireAdmin;
            Regex = regex;
            ValidateStrings = validateStrings;
        }
    }

    public static class CommandHelper
    {
        public static List<CommandInfo> AllCommands = GetAllCommands();

        private static List<CommandInfo> GetAllCommands()
        {
            return GetInfoFromType(typeof(GameCommands))
                .Concat(GetInfoFromType(typeof(Commands))).ToList();

            IEnumerable<CommandInfo> GetInfoFromType(Type type)
            {
                return type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(method => method.GetCustomAttribute<CommandDescriptionAttribute>() != null).Select(method => method.GetCommandInfo());
            }
        }

        public static CommandInfo GetCommandInfo(this MethodInfo methodInfo)
        {
            var reqAdmin = methodInfo.IsAttributeDefined<RequireAdminAttribute>();
            var regex = methodInfo.GetCustomAttribute<RegexParserAttribute>()?.Regex;
            var strings = methodInfo.GetCustomAttribute<StringsAttribute>()?.Strings;

            return new CommandInfo(methodInfo, reqAdmin, regex, strings);
        }

        public static string GetCommandHelp(this MethodInfo info)
        {
            return info.GetCustomAttribute<StringsAttribute>().Strings[0] + " " + string.Concat(
                       info.GetParameters()
                           .SkipInject()
                           .Select(GetParamHelp)) + info.GetCustomAttribute<CommandDescriptionAttribute>().Description;


        }
        static string GetParamHelp(ParameterInfo pinfo)
        {
            var optional = pinfo.IsOptional;
            var description = pinfo.GetCustomAttribute<ParameterAttribute>().Description;
            return $" {(optional ? "(" : "[")}{description}:{pinfo.ParameterType.TypeToString()}{(optional ? ")" : "]")}";
        }
        public static string TypeToString(this Type type)
        {
            if (type == typeof(string)) return "文本";
            if (type == typeof(Number)) return "数字";
            //if (type == typeof(bool)) return "布尔";
            

            throw new NotSupportedException("Fork 您");
        }

        public static IEnumerable<ParameterInfo> SkipInject(this IEnumerable<ParameterInfo> infos)
        {
            return infos.Where(p => !p.IsAttributeDefined<InjectAttribute>());
        }

        public static bool Invoke(Message message)
        {
            var split = message.Content.Split(' ');
            var root = split[0];

            foreach (var info in AllCommands)
            {
                if (info.ValidateStrings?.Any(s => s == root) == true ||
                    info.Regex?.IsMatch(root) == true ||
                    info.MethodInfo.Name == root)
                {

                    if (info.RequireAdmin && !Config.Instance.GetPlayerConfig(message.Sender).Admin)
                        throw new DoudizhuCommandParseException("你不是管理");

                    var reqParameters = info.MethodInfo.GetParameters().SkipInject().ToArray();
                    var parameters = split.Skip(1).ToArray();
                    if (parameters.Length > reqParameters.Length) throw new DoudizhuCommandParseException("参数数量过多");
                    if (parameters.Length < reqParameters.Length - reqParameters.Count(p => p.IsOptional)) throw new DoudizhuCommandParseException("参数不全");

                    // input check
                    for (var index = 0; index < parameters.Length; index++)
                    {
                        var param = parameters[index];
                        var reqparam = reqParameters[index];
                        if (string.IsNullOrWhiteSpace(param)) throw new DoudizhuCommandParseException($"参数{index}为空, 需求为{GetParamHelp(reqparam)}");
                        if (reqparam.ParameterType == typeof(Number) && !Number.TryParse(param, out _)) throw new DoudizhuCommandParseException($"参数{index}不是数字, 需求为{GetParamHelp(reqparam)}");
                    }
                    var game = new Lazy<Game>(() => GamesManager.Games[message.Group]);
                    var currentPlayer = new Lazy<Player>(() => game.Value.CurrentPlayer);
                    var senderPlayer = new Lazy<Player>(() => game.Value.GetPlayer(message.Sender));
                    // attrib check

                    var playerOnly = info.MethodInfo.GetCustomAttribute<PlayerOnlyAttribute>();
                    if (playerOnly != null)
                    {
                        switch (playerOnly.PlayerState)
                        {
                            case PlayerState.InGame:
                                if (!game.Value.ContainsPlayer(currentPlayer.Value)) throw new DoudizhuCommandParseException("该命令需求你在游戏中.");
                                break;
                            case PlayerState.CurrentPlayer:
                                if (currentPlayer.Value != senderPlayer.Value) throw new DoudizhuCommandParseException("该命令需求你是当前玩家.");
                                break;
                            case PlayerState.ChooseLandlordPlayer:
                                if (senderPlayer.Value != game.Value.GetPlayerByIndex(((ChooseLandlordData)game.Value.StateData).LandlordIndex)) throw new DoudizhuCommandParseException("该命令需求你是当前玩家.");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    var stateOnly = info.MethodInfo.GetCustomAttribute<StateOnlyAttribute>();
                    if (stateOnly != null)
                    {
                        if (stateOnly.GameState != game.Value.State) throw new DoudizhuCommandParseException($"该命令需求游戏在以下状态{stateOnly.GameState}."); //TODO
                    }
                    // build params
                    var iReqParams = info.MethodInfo.GetParameters();
                    Debug.Assert(iReqParams.Count(obj => obj.IsAttributeDefined<InjectAttribute>()) == 0 ||
                                 iReqParams.Length == 0 ||
                                 (iReqParams.First().IsAttributeDefined<InjectAttribute>() &&
                                  iReqParams.Select((param, index) => new { param, index })
                                      .Where(obj => obj.param.IsAttributeDefined<InjectAttribute>())
                                      .Select(obj => obj.index)
                                      .ToArray().IsSequential(i => i)));


                    var list = new List<object>();
                    var callbacks = new HashSet<Action>();

                    foreach (var pinfo in iReqParams.Where(p => p.IsAttributeDefined<InjectAttribute>()))
                    {
                        if (pinfo.ParameterType == typeof(Config))
                        {
                            list.Add(Config.Instance);
                            callbacks.Add(Config.Save);
                            continue;
                        }
                        if (pinfo.ParameterType == typeof(Game))
                        {
                            if (!GamesManager.Games.ContainsKey(message.Group)) throw new DoudizhuCommandParseException("该群没有游戏, 请先使用[创建游戏].");
                            list.Add(game.Value);
                            continue;
                        }

                        if (pinfo.ParameterType == typeof(PlayerConfig))
                        {
                             list.Add(Config.Instance.GetPlayerConfig(message.Sender));
                             callbacks.Add(Config.Save);
                             continue;
                        }

                        if (pinfo.ParameterType.IsSubclassOf(typeof(StateData)))
                        {
                            list.Add(game.Value.StateData);
                            continue;
                        }

                        if (pinfo.ParameterType == typeof(TargetSender))
                        {
                            list.Add(message.Group.GetGroupSender());
                            continue;
                        }

                        if (pinfo.ParameterType == typeof(Player))
                        {
                            list.Add(senderPlayer.Value);
                            continue;
                        }
                        var attrib = pinfo.GetCustomAttribute<InjectAttribute>();
                        switch (attrib.Injectwhat)
                        {
                            case Injects.PlayerID:
                                list.Add(message.Sender);
                                continue;
                            case Injects.GameID:
                                list.Add(message.Group);
                                continue;
                        }

                        Debug.Assert(1 + 1 != 2);
                    }


                    for (var index = 0; index < reqParameters.Length; index++)
                    {
                        var param = parameters.SafeGet(index);
                        var reqparam = reqParameters[index];
                        if (reqparam.IsOptional && param == null)
                        {
                            list.Add(Type.Missing);
                            continue;
                        }
                        if (reqparam.ParameterType == typeof(Number))
                        {
                            list.Add(Number.Parse(param));
                            continue;
                        }
                        if (reqparam.ParameterType == typeof(string))
                        {
                            list.Add(param);
                            continue;
                        }

                        throw new NotSupportedException("Fork 您");
                    }

                    var result = info.MethodInfo.Invoke(Activator.CreateInstance(info.MethodInfo.DeclaringType), list.ToArray());
                    if (result == null) return true;
                    
                    switch (result)
                    {
                        case string s:
                            message.Group.GetGroupSender().Send(s);
                            break;
                        default:
                            throw new NotSupportedException("Fork 您");
                    }
                    callbacks.ForEach(c => c());
                    // TODO
                    return true;
                }
            }

            return false;
        }

    }

    public static class Injects
    {
        public const string PlayerID = "PlayerID";
        public const string GameID = "GameID";
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class InjectAttribute : Attribute
    {
        public string Injectwhat { get; }
        public InjectAttribute(string injectwhat = "")
        {
            Injectwhat = injectwhat;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ParameterAttribute : Attribute
    {
        public string Description { get; set; }

        public ParameterAttribute(string description)
        {
            Description = description;
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireAdminAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RegexParserAttribute : Attribute
    {
        public Regex Regex { get; }

        public RegexParserAttribute(string regexString)
        {
            Regex = new Regex(regexString, RegexOptions.Compiled);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StringsAttribute : Attribute
    {
        public string[] Strings { get; }

        public StringsAttribute(params string[] strings)
        {
            Strings = strings;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public CommandDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
