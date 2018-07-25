using System;
using System.Collections.Generic;
using System.Text;

namespace DoudizhuSharp
{
    public class DoudizhuInternalException : Exception
    {
        public DoudizhuInternalException(string message) : base(message)
        {
        }
    }

    public class DoudizhuCommandParseException : DoudizhuInternalException
    {
        public DoudizhuCommandParseException(string message) : base($"你发送的命令在检验时发生了错误: {message} 请检查你的输入.")
        {
        }
    }

    public class DoudizhuCardParseException : DoudizhuInternalException
    {
        public DoudizhuCardParseException(string message) : base($"你发送的出牌命令在检验时发生了错误: {message} 请检查你的输入.")
        {
        }
    }

    public class DoudizhuRuleException : DoudizhuInternalException
    {
        public DoudizhuRuleException() : base($"规则校验出错.")
        {

        }

        public DoudizhuRuleException(string message) : base($"规则校验出错: {message}.")
        {
        }
    }
}
