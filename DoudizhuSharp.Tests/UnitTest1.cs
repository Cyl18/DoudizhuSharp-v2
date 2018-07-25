
using System;
using System.Diagnostics;
using System.Linq;
using DoudizhuSharp.Messages;
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
    }
}
