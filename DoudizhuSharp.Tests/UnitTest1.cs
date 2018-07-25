
using System;
using System.Diagnostics;
using System.Linq;
using DoudizhuSharp.Messages;
using DoudizhuSharp.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DoudizhuSharp.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var sw = Stopwatch.StartNew();
            var cg = "2333333333KKKK31010101033333".ParseCards();
            Debug.WriteLine($"{sw.ElapsedMilliseconds}ms{cg.First().Value}");
        }

        [TestMethod]
        public void Test2()
        {
            foreach (var info in CommandHelper.AllCommands)
            {
                Debug.WriteLine(info.MethodInfo.Name);
            }
        }

        [TestMethod]
        public void Test3()
        {
            MessageCore.PrivateSender = (target, message) => Debug.WriteLine($"Private {target} {message}");
            MessageCore.GroupSender = (target, message) => Debug.WriteLine($"Group {target} {message}");
            MessageCore.AtCoder = s => s;
            var sw = Stopwatch.StartNew();
            MessageCore.ProcessMessage(new Message("123", "456", "新建游戏", "789"));
            Debug.WriteLine($"First: {sw.ElapsedMilliseconds}ms");
            sw = Stopwatch.StartNew();

            MessageCore.ProcessMessage(new Message("123", "456", "上桌", "789"));
            MessageCore.ProcessMessage(new Message("123", "789", "上桌", "789"));
            MessageCore.ProcessMessage(new Message("123", "789", "开始游戏", "789"));
            Debug.WriteLine($"Second: {sw.ElapsedMilliseconds}ms");

        }

        [TestMethod]
        public void Test4()
        {
            Assert.IsTrue(Ruler.GetRule("3".ParseCards()) is RuleSolo);
            Assert.IsTrue(Ruler.GetRule("33".ParseCards()) is RulePair);
            Assert.IsTrue(Ruler.GetRule("333".ParseCards()) is RuleTrio);
            Assert.IsTrue(Ruler.GetRule("3334".ParseCards()) is RuleTrioWithSolo);
            Assert.IsTrue(Ruler.GetRule("33344".ParseCards()) is RuleTrioWithPair);
            Assert.IsTrue(Ruler.GetRule("34567".ParseCards()) is RuleSoloChain);
            Assert.IsTrue(Ruler.GetRule("334455".ParseCards()) is RulePairChain);
            Assert.IsTrue(Ruler.GetRule("333444".ParseCards()) is RuleAirplain);
            Assert.IsTrue(Ruler.GetRule("33344456".ParseCards()) is RuleAirplainWithSolo);
            Assert.IsTrue(Ruler.GetRule("33344455".ParseCards()) is RuleAirplainWithSolo);
            Assert.IsTrue(Ruler.GetRule("3334445566".ParseCards()) is RuleAirplainWithPair);
            Assert.IsTrue(Ruler.GetRule("333345".ParseCards()) is RuleFourWithSolo);
            Assert.IsTrue(Ruler.GetRule("33334455".ParseCards()) is RuleFourWithPair);
            Assert.IsTrue(Ruler.GetRule("3333".ParseCards()) is RuleBomb);
            Assert.IsTrue(Ruler.GetRule("小王大王".ParseCards()) is RuleJokerBomb);
        }
    }
}
