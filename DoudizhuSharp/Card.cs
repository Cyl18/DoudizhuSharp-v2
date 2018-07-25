using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using DoudizhuSharp.Extensions;
using EnumsNET;
using GammaLibrary.Extensions;

namespace DoudizhuSharp
{
    public struct Card : IEquatable<Card>, IComparable<Card>
    {
        public Value CardValue { get; }
        public bool IsAnyCard { get; }

        public Card(Value cardValue, bool isAnyCard = false)
        {
            CardValue = cardValue;
            IsAnyCard = isAnyCard;
        }

        public int CompareTo(Card other)
        {
            return CardValue.CompareTo(other.CardValue);
        }

        public static bool operator <(Card left, Card right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Card left, Card right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Card left, Card right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Card left, Card right)
        {
            return left.CompareTo(right) >= 0;
        }

        public bool Equals(Card other)
        {
            return CardValue == other.CardValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is Card card && Equals(card);
        }

        public override int GetHashCode()
        {
            return (int) CardValue;
        }

        public static bool operator ==(Card left, Card right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Card left, Card right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"[{(CardValue.GetAttributes()[0] as SymbolAttribute).Symbols[0]}]";
        }
    }

    //[Symbol("VOID*", "+", "癞")]



    public enum Value
    {
        [Symbol("喵喵喵?")] NoValue,
        [Symbol("3")] Three,
        [Symbol("4")] Four,
        [Symbol("5")] Five,
        [Symbol("6")] Six,
        [Symbol("7")] Seven,
        [Symbol("8")] Eight,
        [Symbol("9")] Nine,
        [Symbol("10")] Ten,
        [Symbol("J")] Jack,
        [Symbol("Q")] Queen,
        [Symbol("K")] King,
        [Symbol("A")] Ace,
        [Symbol("2")] Two,
        [Symbol("小王", "鬼")] ColorlessJoker,
        [Symbol("大王", "王")] ColoredJoker
    }

    public static class CardHelper
    {
        public static readonly Dictionary<string, Value> Values = GenerateValues();

        private static Dictionary<string, Value> GenerateValues()
        {
            return Enums.GetMembers<Value>()
                .Select(member => new {symbol = ((SymbolAttribute) member.Attributes[0]).Symbols, member})
                .SelectMany(obj => obj.symbol, (obj, str) => new {obj.member, str})
                .OrderByDescending(obj => obj.str.Length)
                .ToDictionary(obj => obj.str, obj => obj.member.Value);
        }

        public static ICollection<CardGroup> ParseCards(this string source)
        {
            var list = new List<CardGroup>();
            foreach (var pair in Values)
            {
                var (count, result) = source.CountAndRemove(pair.Key);
                if (count != 0) list.Add(new CardGroup(pair.Value, count));
                source = result;
                if (source.Length == 0) break;
            }

            if (source.Length != 0) throw new DoudizhuCardParseException("无法处理");
            list.Sort();
            return list;
        }

        public static ICollection<Card> ToCards(this IEnumerable<CardGroup> cardGroups)
        {
            return cardGroups.ToCardsInternal().ToList();
        }

        private static IEnumerable<Card> ToCardsInternal(this IEnumerable<CardGroup> cardGroups)
        {
            foreach (var cardGroup in cardGroups)
            {
                for (var i = 0; i < cardGroup.Count; i++)
                {
                     yield return new Card(cardGroup.Value);
                }
            }
        }

        public static ICollection<Card> GetDeck()
        {
            return GetDeckInternal().ToList();
        }

        public static IEnumerable<Card> GetDeckInternal()
        {
            foreach (var i in Enumerable.Range((int)Value.Three, Value.Two - Value.Three + 1))
            {
                for (var j = 0; j < 4; j++)
                {
                    yield return new Card((Value)i);
                }
            }
            yield return new Card(Value.ColorlessJoker);
            yield return new Card(Value.ColoredJoker);
        }

        public static string ToCardString(this IEnumerable<Card> cards)
        {
            return cards.Select(card => card.ToString()).StringJoin("");
        }

        public static ICollection<CardGroup> ToCardGroups(this ICollection<Card> cards)
        {
            return cards.CloneAndSort().ExtractCardGroupsInternal();
        }

        private static unsafe List<CardGroup> ExtractCardGroupsInternal(this ICollection<Card> cards, bool keepZero = false)
        {
            var enumerable = cards;
            var cardnums = enumerable.Select(card => (int)card.CardValue);
            var length = keepZero ? 15 : (int)enumerable.Last().CardValue + 1;
            var array = stackalloc int[length];
            ZeroMemory(array, length);
            foreach (var num in cardnums) array[num]++;
            var o = new List<CardGroup>(length);
            for (var i = 0; i < length; i++)
            {
                var num = array[i];
                if (num != 0 || keepZero) o.Add(new CardGroup((Value)i, num));
            }

            return o;

            void ZeroMemory(int* source, int len)
            {
                for (var i = 0; i < len; i++) source[i] = 0;
            }
        }
    }

    public struct CardGroup : IComparable<CardGroup>
    {
        public Value Value;
        public int Count;

        public CardGroup(Value value, int count)
        {
            Value = value;
            Count = count;
        }

        public int CompareTo(CardGroup other)
        {
            return Value.CompareTo(other.Value);
        }

        public static bool operator <(CardGroup left, CardGroup right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(CardGroup left, CardGroup right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(CardGroup left, CardGroup right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(CardGroup left, CardGroup right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
/*
    public enum Color
    {
        [Symbol("♥")] Heart,
        [Symbol("♦")] Tile,
        [Symbol("♣")] Clover,
        [Symbol("♠")] Pike
    }
*/

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    class SymbolAttribute : Attribute
    {
        public string[] Symbols { get; }

        public SymbolAttribute(params string[] symbols)
        {
            Symbols = symbols;
        }
    }
}
