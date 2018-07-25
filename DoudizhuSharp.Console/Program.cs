using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoudizhuSharp.Messages;

namespace DoudizhuSharp.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageCore.PrivateSender = (target, message) => System.Console.WriteLine($"Private {target} {message}");
            MessageCore.GroupSender = (target, message) => System.Console.WriteLine($"Group {target} {message}");
            MessageCore.AtCoder = s => s;


            while (true)
            {
                var messages = System.Console.ReadLine().Split(' ');
                var msg = new Message("1", messages[0], messages[1]);
                MessageCore.ProcessMessage(msg);
            }
        }
    }
}
