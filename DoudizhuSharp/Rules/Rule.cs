using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoudizhuSharp.Rules
{

    public abstract class Rule //<T> where T : Rule<T> 傻逼C#泛型
    {
        public abstract SpecialRuleInfo[] Specials { get; }
        public ICollection<CardGroup> CardGroups { get; set; }

        public Rule(ICollection<CardGroup> cgs)
        {
            CardGroups = cgs;
        }

        public abstract bool IsValidate(Rule lastRule);
    }

    public class SpecialRuleInfo
    {
        //TODO 王炸
    }

    public class RuleSolo : Rule
    {
        public RuleSolo(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count != 1 || cgs.First().Count != 1) throw new DoudizhuRuleException();
            Amount = (int)cgs.First().Value;
        }

        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        private int Amount { get; }

        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleSolo) lastRule;
            return Amount > last.Amount;
        }
    }

    public class RulePair : Rule
    {
        public RulePair(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count != 1 || cgs.First().Count != 2) throw new DoudizhuRuleException();
            Amount = (int)cgs.First().Value;
        }

        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        private int Amount { get; }

        public override bool IsValidate(Rule lastRule)
        {
            var last = (RulePair)lastRule;
            return Amount > last.Amount;
        }
    }

    public class RuleSoloChain : Rule
    {
        public RuleSoloChain(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count < 5 || cgs.Any(cg => cg.Count != 1) || !cgs.IsSequential() || cgs.Any(cg => cg.Value > Value.Ace)) throw new DoudizhuRuleException();
            ChainLength = cgs.Count;
            SmallestAmount = (int)cgs.First().Value;
        }

        private readonly int SmallestAmount;
        private readonly int ChainLength;

        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleSoloChain) lastRule;
            return last.ChainLength == ChainLength && SmallestAmount > last.SmallestAmount;
        }
    }

    public class RulePairChain : Rule
    {
        public RulePairChain(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count < 3 || cgs.Any(cg => cg.Count != 2) || !cgs.IsSequential() || cgs.Any(cg => (int)cg.Value > (int)Value.Ace)) throw new DoudizhuRuleException();
            ChainLength = cgs.Count;
            SmallestAmount = (int)cgs.First().Value;
        }

        private readonly int SmallestAmount;
        private readonly int ChainLength;

        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RulePairChain)lastRule;
            return last.ChainLength == ChainLength && SmallestAmount > last.SmallestAmount;
        }
    }

    public class RuleTrio : Rule
    {
        public RuleTrio(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count != 1 || cgs.First().Count != 3) throw new DoudizhuRuleException();
            Amount = (int)cgs.First().Value;
        }

        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        private int Amount { get; }

        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleTrio)lastRule;
            return Amount > last.Amount;
        }
    }

    public class RuleAirplain : Rule
    {
        public RuleAirplain(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count < 2 || cgs.Any(cg => cg.Count != 3) || !cgs.IsSequential() || cgs.Any(cg => cg.Value > Value.Ace)) throw new DoudizhuRuleException();
            ChainLength = cgs.Count;
            SmallestAmount = (int)cgs.First().Value;
        }

        private readonly int SmallestAmount;
        private readonly int ChainLength;

        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleAirplain)lastRule;
            return last.ChainLength == ChainLength && SmallestAmount > last.SmallestAmount;
        }
    }

    public class RuleTrioWithSolo : Rule
    {
        public RuleTrioWithSolo(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count != 2 || !cgs.Any(cg => cg.Count == 3) || !cgs.Any(cg => cg.Count == 1)) throw new DoudizhuRuleException();
            TrioAmount = (int) cgs.First(cg => cg.Count == 3).Value;
        }

        private readonly int TrioAmount;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleTrioWithSolo) lastRule;
            return TrioAmount > last.TrioAmount;
        }
    }

    public class RuleTrioWithPair : Rule
    {
        public RuleTrioWithPair(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count != 2 || !cgs.Any(cg => cg.Count == 3) || !cgs.Any(cg => cg.Count == 2)) throw new DoudizhuRuleException();
            TrioAmount = (int)cgs.First(cg => cg.Count == 3).Value;
        }

        private readonly int TrioAmount;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleTrioWithPair)lastRule;
            return TrioAmount > last.TrioAmount;
        }
    }

    public class RuleAirplainWithSolo : Rule
    {
        public RuleAirplainWithSolo(ICollection<CardGroup> cgs) : base(cgs)
        {
            var trios = cgs.Where(cg => cg.Count == 3).ToList();
            var nonTrios = cgs.Where(cg => cg.Count != 3).ToCards().ToList();
            if (trios.Count != nonTrios.Count || !trios.IsSequential() || trios.Any(cg => cg.Value > Value.Ace)) throw new DoudizhuRuleException();
            SmallestTrio = (int) trios.First().Value;
            ChainLength = trios.Count;
        }

        private readonly int SmallestTrio;
        private readonly int ChainLength;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleAirplainWithSolo) lastRule;
            return ChainLength == last.ChainLength && SmallestTrio > last.SmallestTrio;
        }
    }

    public class RuleAirplainWithPair : Rule
    {
        public RuleAirplainWithPair(ICollection<CardGroup> cgs) : base(cgs)
        {
            var trios = cgs.Where(cg => cg.Count == 3).ToList();
            var nonTrios = cgs.Where(cg => cg.Count != 3).ToCards().ToList();
            if (trios.Count * 2 != nonTrios.Count || !trios.IsSequential() || trios.Any(cg => cg.Value > Value.Ace)) throw new DoudizhuRuleException();
            SmallestTrio = (int)trios.First().Value;
            ChainLength = trios.Count;
        }

        private readonly int SmallestTrio;
        private readonly int ChainLength;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleAirplainWithPair)lastRule;
            return ChainLength == last.ChainLength && SmallestTrio > last.SmallestTrio;
        }
    }

    public class RuleFourWithSolo : Rule
    {
        public RuleFourWithSolo(ICollection<CardGroup> cgs) : base(cgs)
        {
            var four = cgs.Where(cg => cg.Count == 4).ToList();
            var nonFour = cgs.Where(cg => cg.Count != 4).ToCards().ToList();
            if (four.Count != 1 || nonFour.Count != 2) throw new DoudizhuRuleException();
            
            FourAmount = (int)four.First().Value;
        }

        private readonly int FourAmount;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleFourWithSolo)lastRule;
            return FourAmount > last.FourAmount;
        }
    }

    public class RuleFourWithPair : Rule
    {
        public RuleFourWithPair(ICollection<CardGroup> cgs) : base(cgs)
        {
            var four = cgs.Where(cg => cg.Count == 4).ToList();
            var two = cgs.Where(cg => cg.Count == 2).ToList();
            if (four.Count != 1 || two.Count != 2 || cgs.Count(cg => cg.Count != 2 && cg.Count != 4) > 0) throw new DoudizhuRuleException();

            FourAmount = (int)four.First().Value;
        }

        private readonly int FourAmount;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            var last = (RuleFourWithPair)lastRule;
            return FourAmount > last.FourAmount;
        }
    }

    public class RuleBomb : Rule
    {
        public RuleBomb(ICollection<CardGroup> cgs) : base(cgs)
        {
            var cg = cgs.First();
            if (cgs.Count != 1 || cg.Count < 4) throw new DoudizhuRuleException();
            BombCardCount = cg.Count;
            BombAmount = (int) cg.Value;
        }

        private readonly int BombAmount;
        private readonly int BombCardCount;
        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            if (!(lastRule is RuleBomb) && !(lastRule is RuleJokerBomb)) return true;
            
            var last = (RuleBomb)lastRule;
            return BombCardCount > last.BombCardCount ||
                   (BombCardCount == last.BombCardCount && BombAmount > last.BombAmount);
        }
    }

    public class RuleJokerBomb : Rule
    {
        public RuleJokerBomb(ICollection<CardGroup> cgs) : base(cgs)
        {
            if (cgs.Count != 2 || !cgs.Any(cg => cg.Value == Value.ColoredJoker) || !cgs.Any(cg => cg.Value == Value.ColorlessJoker))
                throw new DoudizhuRuleException("");
        }


        public override SpecialRuleInfo[] Specials { get; } = new SpecialRuleInfo[0];
        public override bool IsValidate(Rule lastRule)
        {
            return !(lastRule is RuleJokerBomb);
        }
    }
}
