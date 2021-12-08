using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using GammaLibrary.Extensions;

namespace DoudizhuSharp.Rules
{
    public static class Ruler
    {
        private static readonly Type[] _rules = Assembly.GetExecutingAssembly()
            .ExportedTypes.Where(type => type.IsSubclassOf(typeof(Rule))).Where(type => type != typeof(RuleBomb) && type != typeof(RuleSoftBomb)).ToArray();

        public static bool IsValid(Game game, ICollection<CardGroup> cardGroups, bool transformedAnyCard = false)
        {
            foreach (var ruleType in _rules.Concat((transformedAnyCard ? typeof(RuleSoftBomb) : typeof(RuleBomb)).AsArray()))
            {
                try
                {
                    var rule = CreateRule(ruleType, cardGroups);
                    if (game.LastRule != null && !rule.IsValid(game.LastRule))
                    {
                        throw new DoudizhuRuleException();
                    }

                    game.LastRule = rule;
                    return true;
                }
                catch (DoudizhuRuleException)
                {
                    // ignore
                }
                catch (TargetInvocationException)
                {
                    // ignore
                }
                catch (InvalidCastException)
                {
                    // ignore
                }
            }

            return false;
        }

        public static Rule GetRule(Game game, ICollection<CardGroup> cardGroups, bool transformedAnyCard = false)
        {
            foreach (var ruleType in _rules.Concat((transformedAnyCard ? typeof(RuleSoftBomb) : typeof(RuleBomb)).AsArray()))
            {
                try
                {
                    var rule = CreateRule(ruleType, cardGroups);
                    if (game.LastRule != null && !rule.IsValid(game.LastRule))
                    {
                        throw new DoudizhuRuleException();
                    }

                    //game.LastRule = rule;
                    return rule;
                }
                catch (DoudizhuRuleException)
                {
                    // ignore
                }
                catch (TargetInvocationException)
                {
                    // ignore
                }
                catch (InvalidCastException)
                {
                    // ignore
                }
            }

            return null;
        }

        private static Rule CreateRule(Type type, ICollection<CardGroup> cardGroups)
        {
            return (Rule)Activator.CreateInstance(type, cardGroups);
        }
        public static bool IsSequential(this ICollection<CardGroup> groups)
        {
            //Debug.Assert(groups.OrderBy(cg => cg.Value).SequenceEqual(groups)); // 应已排序
            //Debug.Assert(groups.Count != 0); // CardGroups 不应为 0
            Debug.Assert(groups.All(cg => cg.Count != 0)); // CardGroup 的 Card Count 不应为 0
            
            //if (groups.Any(cg => cg.Count != 1)) return false;
            return groups.IsSequential(g => (int)g.Value);
        }

        public static bool IsSequential<T>(this ICollection<T> groups, Func<T, int> valueSelector)
        {
            Debug.Assert(groups.OrderBy(valueSelector).SequenceEqual(groups)); // 应已排序
            Debug.Assert(groups.Count != 0); // CardGroups 不应为 0
            //Debug.Assert(groups.All(cg => cg.Count != 0)); // CardGroup 的 Card Count 不应为 0

            //if (groups.Any(cg => cg.Count != 1)) return false;
            return valueSelector(groups.Last()) - valueSelector(groups.First()) + 1 == groups.Count;
        }

        public static bool IsValidForPlayer(ICollection<CardGroup> playerCards, ICollection<CardGroup> cards)
        {
            Debug.Assert(playerCards.ToCards().OrderBy(c => c.CardValue).SequenceEqual(playerCards.ToCards()));
            Debug.Assert(cards.ToCards().OrderBy(c => c.CardValue).SequenceEqual(cards.ToCards()));
            var pcardGroup = playerCards.ToDictionary(g => g.Value);
            var cardGroup = cards;
            return cardGroup.All(g => pcardGroup.ContainsKey(g.Value) && pcardGroup[g.Value].Count >= g.Count);
        }
    }
}
