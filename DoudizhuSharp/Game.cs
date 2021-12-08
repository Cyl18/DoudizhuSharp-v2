using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DoudizhuSharp.Messages;
using DoudizhuSharp.Rules;
using GammaLibrary.Extensions;
using Number = System.Numerics.BigInteger;

namespace DoudizhuSharp
{
    public class GamesManager
    {
        public static Dictionary<string, Game> Games { get; } = new Dictionary<string, Game>();

        public static Game GetGame(string id)
        {
            return Games.ContainsKey(id) ? Games[id] : null;
        }
    }
    
    public class Game
    {
        public Value AnyCardValue = Value.Three;

        public GameInitSettings GameSettings { get; } = new GameInitSettings();
        public Cycle<Player> Cycle { get; } = new Cycle<Player>();
        public ICollection<Player> Players => Cycle.List;
        public GameState State { get; private set; } = GameState.Prepairing;

        public StateData StateData
        {
            get => States[State];
            set => States[State] = value;
        }

        private Dictionary<GameState, StateData> States { get; } = new()
        {
            { GameState.ChooseLandlord, new ChooseLandlordData() }
        };
        public Rule LastRule { get; internal set; }
        public TargetSender GroupSender { get; }
        public string GameID { get; }
        public int LastSendIndex { get; private set; }
        public Player CurrentPlayer => Cycle.Current;

        public Game(string gameID)
        {
            GameID = gameID;
            GroupSender = gameID.GetGroupSender();
        }

        public void StartGame()
        {
            Cycle.List.Shuffle(); // 洗玩家
            State = GameState.ChooseLandlord;
            SendCards();
            SendChooseLandlordInfo();
        }

        public Player GetPlayerByIndex(int index) => Cycle.List[index];
        public void SendChooseLandlordInfo()
        {
            GroupSender.Send($"{GetPlayerByIndex(((ChooseLandlordData)StateData).LandlordIndex).ToAtCode()} 你要抢地主吗?");
        }

        public void SendCards()
        {
            var decks = GameSettings.Deck * CardHelper.GetDeck().Count;

            var hideCardCount = (int)decks % Players.Count + Players.Count;
            var deck = Enumerable.Repeat(CardHelper.GetDeck(), (int)GameSettings.Deck).SelectMany(card => card).ToArray();
            deck.Shuffle(); // 洗牌
            
            // 底牌
            var hideCards = deck.Take(hideCardCount).ToArray();
            deck = deck.Skip(hideCardCount).ToArray();

            Debug.Assert(deck.Length % Players.Count == 0); // 应该 会被 整除吧?

            var pcardcount = deck.Length / Players.Count; // 每个玩家的牌数
            
            foreach (var player in Players)
            {
                var pcard = deck.Take(pcardcount).ToArray();
                deck = deck.Skip(pcardcount).ToArray();
                player.SendCard(pcard);
            }

            ((ChooseLandlordData) StateData).HiddenCards = hideCards;
        }


        public void WantLandlord(Player player)
        {
            player.Role = Role.Landlord;
            var hiddenCards = ((ChooseLandlordData)StateData).HiddenCards;
            player.SendCard(hiddenCards);
            GroupSender.Send($"底牌有{hiddenCards.ToCardString()}.");
            State = GameState.Gaming;
            Cycle.CurrentIndex = Cycle.List.FindIndex(p => p.PlayerID == player.PlayerID);
            Tick();
        }

        public void Tick()
        {
            var winner = Players.FirstOrDefault(player => player.Cards.Count == 0);
            if (winner != null)
            {
                GroupSender.Send($"{winner.ToAtCode()} 你赢了.");
                State = GameState.Finished;
                GamesManager.Games.Remove(this.GameID);
                return;
            }

            if (LastSendIndex == Cycle.List.FindIndex(p => p == CurrentPlayer))
            {
                LastRule = null;
            }

            GroupSender.Send($"{CurrentPlayer.ToAtCode()} 该你出牌了{(LastRule != null ? $", 当前规则为{LastRule.GetType().GetCustomAttribute<RuleNameAttribute>().Name}" : "")}");

        }

        public void PlayerSendCard(Player player, ICollection<CardGroup> cardGroups)
        {
            if (!Ruler.IsValidForPlayer(player.Cards.ToCardGroups(), cardGroups))
                goto invalid;

            Rule rule;
            if (cardGroups.Any(cg => cg.Value == AnyCardValue))
            {
                var processor = new AnyCardProcessor(this, AnyCardValue);
                processor.Process(cardGroups);
                var list = processor.ValidCardGroupsList;
                if (list.Count == 0) goto invalid;
                if (list.Count == 1)
                {
                    rule = list.First().rule;
                    goto run;
                }
                // 多选
                State = GameState.WaitAnyCard;
                StateData = new SelectCardData() {ValidCardGroupsList = list};
                var sb = new StringBuilder();
                var count = 0;
                sb.AppendLine("选择牌型:");
                foreach (var (r, cards) in list)
                {
                    var desc = r.GetType().GetCustomAttribute<RuleNameAttribute>().Name;
                    sb.AppendLine($"{count++}.{desc}: {cards.ToCards().ToCardString()}");
                }

                var cards1 = cardGroups.ToCards();
                player.RemoveCard(cards1);
                LastSendIndex = Cycle.List.FindIndex(p => p == player);
                GroupSender.Send($"{player.ToAtCode()} {sb.ToString()}");

                return;
            }
            else
            {
                rule = Ruler.GetRule(this, cardGroups);
            }

            run:
            if (rule is not null)
            {
                var cards = cardGroups.ToCards();
                player.RemoveCard(cards);
                LastSendIndex = Cycle.List.FindIndex(p => p == player);
                LastRule = rule;
                GroupSender.Send($"{player.ToAtCode()} 出牌: {cards.ToCardString()}");
                Cycle.MoveNext();
                Tick();
            }
            else
            {
                goto invalid;
            }
            return;

            invalid:
            GroupSender.Send("你出的牌无效");
        }

        public void EndAnyCard(Player player, string messageContent)
        {
            var state = (SelectCardData) StateData;
            if (!messageContent.IsInt()) return;
            
            var index = messageContent.ToInt();
            if (index >= state.ValidCardGroupsList.Count || index < 0)
            { 
                GroupSender.Send("索引越界.");
                return;
            }
            var (rule, cards) = state.ValidCardGroupsList[index];

            LastRule = rule;
            State = GameState.Gaming;
            GroupSender.Send($"{player.ToAtCode()} 出牌: {cards.ToCards().ToCardString()}");
            Cycle.MoveNext();
            Tick();
        }
    }

    public class AnyCardProcessor
    {
        private Game game;
        Value anyCardValue;
        int gcount = -1;
        public List<(Rule rule, CardGroup[] cards)> ValidCardGroupsList { get; } = new();

        public AnyCardProcessor(Game game, Value anyCardValue)
        {
            this.game = game;
            this.anyCardValue = anyCardValue;
        }

        public void Process(ICollection<CardGroup> originalCards)
        {
            var originalList = originalCards.ToArray();
            var cardsBase = originalCards.ToArray();
            
            foreach (ref var cg in cardsBase.AsSpan())
            {
                if (cg.Value == anyCardValue)
                {
                    gcount = cg.Count;
                    cg.Count = 0;
                }
            }

            Manipulate(cardsBase, gcount);
            Add(originalList, false);
        }

        private void Manipulate(CardGroup[] cardGroups, int count, int index = 0)
        {
            if (count == 0)
            {
                var mulp = cardGroups.Where(cg => cg.Count != 0).ToArray();
                if (cardGroups.Single(cg => cg.Value == anyCardValue).Count == gcount) return;
                
                Add(mulp);
                return;
            }

            for (var i = index; i < cardGroups.Length; i++)
            {
                if (cardGroups[i].Value is Value.ColoredJoker or Value.ColorlessJoker) 
                    continue;
                
                cardGroups[i].Count++;
                Manipulate(cardGroups, count - 1);
                cardGroups[i].Count--;
            }

        }

        void Add(CardGroup[] mulp, bool tr = true)
        {
            var rule = Ruler.GetRule(game, mulp, tr);
            if (rule != null)
            {
                if (ValidCardGroupsList.Any(cg => cg.cards.SequenceEqual(mulp))) return;

                ValidCardGroupsList.Add((rule, mulp));
            }
        }
    }

    public abstract class StateData
    {
    }

    public class SelectCardData : StateData
    {
        public List<(Rule rule, CardGroup[] cards)> ValidCardGroupsList { get; init; }
    }

    public class ChooseLandlordData : StateData
    {
        public ICollection<Card> HiddenCards { get; internal set; }
        public int LandlordIndex { get; internal set; } = 0;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StateOnlyAttribute : Attribute
    {
        public GameState GameState { get; }

        public StateOnlyAttribute(GameState state)
        {
            GameState = state;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PlayerOnlyAttribute : Attribute
    {
        public PlayerState PlayerState { get; }

        public PlayerOnlyAttribute(PlayerState state)
        {
            PlayerState = state;
        }
    }

    public enum PlayerState
    {
        InGame,
        CurrentPlayer,
        ChooseLandlordPlayer
    }

    public class GameCommands
    {
        [Strings("上桌", "加入游戏"), CommandDescription("加入当前群的游戏。")]
        [StateOnly(GameState.Prepairing)]
        string JoinGame([Inject] Game game,
            [Inject(Injects.PlayerID)] string id)
        {
            if (game.ContainsPlayer(id)) return "你已经在游戏中了.";
            if (game.State != GameState.Prepairing) return "当前的时间或空间错误.";
            game.AddPlayer(new Player(id));

            return $"{id.ToAtCode()} 加入了游戏"; //TODO
        }

        [Strings("下桌", "离开游戏", "退出游戏"), CommandDescription("离开当前群的游戏。")]
        [StateOnly(GameState.Prepairing)]
        string QuitGame([Inject] Game game,
            [Inject(Injects.PlayerID)] string id)
        {
            if (!game.ContainsPlayer(id)) return "你已经不在游戏中.";
            if (game.State != GameState.Prepairing) return "当前的时间或空间错误.";
            game.RemovePlayer(game.GetPlayer(id));

            return $"{id.ToAtCode()} 离开了游戏";
        }

        [Strings("开始游戏"), CommandDescription("加入当前群的游戏。")]
        string StartGame([Inject] Game game)
        {
            if (game.Cycle.Count < 2) return $"人数不够 当前玩家有{game.Cycle.Count}个";
            game.StartGame();
            return "游戏开始.";
        }

        [Strings("抢", "抢地主"), CommandDescription("抢地主。")]
        [StateOnly(GameState.ChooseLandlord), PlayerOnly(PlayerState.ChooseLandlordPlayer)]
        void WantLandlord([Inject] Game game,
            [Inject] Player player)
        {
            game.WantLandlord(player);
        }

        [Strings("不抢", "不"), CommandDescription("不抢地主。")]
        [StateOnly(GameState.ChooseLandlord), PlayerOnly(PlayerState.ChooseLandlordPlayer)]
        void DontWantLandlord([Inject] Game game,
            [Inject] Player player)
        {
            var data = (ChooseLandlordData)game.StateData;
            data.LandlordIndex++;
            game.SendChooseLandlordInfo();
        }

        [Strings("过", "pass"), CommandDescription("过牌。")]
        [StateOnly(GameState.Gaming), PlayerOnly(PlayerState.CurrentPlayer)]
        void Pass([Inject] Game game, [Inject] Player player)
        {
            if (game.Cycle.CurrentIndex == game.LastSendIndex)
                game.GroupSender.Send("你不能过牌！");
            else
            {
                game.Cycle.MoveNext();
                game.Tick();
            }
        }
        
    }

    public enum GameState
    {
        Prepairing,
        ChooseLandlord,
        Gaming,
        WaitAnyCard,
        Finished
    }
    public class GameInitSettings
    {
        [Description("多少副牌")] public Number Deck { get; set; } = 1;
        [Description("最大玩家数")] public Number MaxPlayers { get; set; } = 3;
        [Description("使用癞子")] public bool UseAnyCard { get; set; } = false;
        [Description("癞子数")] public Number AnyCardCount { get; set; } = 0;
    }

    public interface IGame
    {
        string GameID { get; }
        ICollection<Player> Players { get; }
        bool IsGaming { get; }
    }

    public static class GameExtensions
    {
        public static bool ContainsPlayer(this Game game, string id)
        {
            return game.Cycle.Any(p => p.PlayerID == id);
        }

        public static bool ContainsPlayer(this Game game, Player player)
        {
            return game.Cycle.Any(p => p.PlayerID == player.PlayerID);
        }

        public static void AddPlayer(this Game game, Player p)
        {
            game.Cycle.Add(p);
        }

        public static void RemovePlayer(this Game game, Player p)
        {
            game.Cycle.Remove(p);
        }

        public static Player GetPlayer(this Game game, string id)
        {
            return game.Cycle.FirstOrDefault(p => p.PlayerID == id);
        }
    }
}
