using BedrockLauncher.Core.CoreOption;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core
{
    public class BedrockCore
    {
        public CoreOptions options { get;set; }
        public BedrockCore()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new Exception("仅支持Windows平台");
            }
        }

        public void Init()
        {
            if (options == null)
            {
                options = new CoreOptions();
            }

            if (options.autoOpenWindowsDevelopment)
            {
                OpenWindowsDevelopment();
            }
            
        }
        /// <summary>
        /// 开启Windows开发者模式
        /// </summary>
        public bool OpenWindowsDevelopment()
        {
            try
            {
                var AppModelUnlock = Registry.LocalMachine.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
                AppModelUnlock.SetValue("AllowDevelopmentWithoutDevLicense", 1);
                return true;
            }
            catch
            {
                return false;
            }
          
        }
        /// <summary>
        /// 获取Windows开发者模式状态
        /// </summary>
        /// <returns>true为开启，false为关闭</returns>
        public bool GetWindowsDevelopmentState()
        {
            var AppModelUnlock = Registry.LocalMachine.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
            var value = AppModelUnlock.GetValue("AllowDevelopmentWithoutDevLicense", 1);
            if ((int)value == 0)
            {
                return false;
            }
            return true;
        }
    }
}
