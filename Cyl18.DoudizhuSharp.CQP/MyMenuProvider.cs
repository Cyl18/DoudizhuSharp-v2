﻿using System.Collections.Generic;
using Newbe.Mahua;

namespace Cyl18.DoudizhuSharp.CQP
{
    public class MyMenuProvider : IMahuaMenuProvider
    {
        public IEnumerable<MahuaMenu> GetMenus()
        {
            return new[]
            {
                new MahuaMenu
                {
                    Id = "menu1",
                    Text = "测试菜单1"
                },
                new MahuaMenu
                {
                    Id = "menu2",
                    Text = "测试菜单2"
                },
            };
        }
    }
}
