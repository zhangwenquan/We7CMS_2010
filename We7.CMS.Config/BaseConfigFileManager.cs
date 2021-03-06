﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using We7.Framework.Config;

namespace We7.CMS.Config
{
    class BaseConfigFileManager : DefaultConfigFileManager
    {
        private static IConfigInfo m_configinfo;

        public static string filename = null;

        private static object m_lockHelper = new object();

        public new static string ConfigFilePath
        {
            get
            {
                if (filename == null || filename == "")
                {
                    HttpContext context = HttpContext.Current;
                    if (context != null)
                    {
                        filename = context.Server.MapPath("~/Config/db.config");
                    }
                    else
                    {
                        string path = AppDomain.CurrentDomain.BaseDirectory;
                        if (path.ToLower().EndsWith("bin"))
                        {
                            path = path.Substring(0, path.Length - 4);
                        }
                        filename = Path.Combine(path, "Config/db.config");
                    }

                    if (!File.Exists(filename))
                    {
                        filename = "";
                    }
                }
                return filename;
            }
        }

        public new static IConfigInfo ConfigInfo
        {
            get { return m_configinfo; }
            set { m_configinfo = (BaseConfigInfo)value; }
        }

        public static BaseConfigInfo LoadRealConfig()
        {
            if (ConfigFilePath != "")
            {
                lock (m_lockHelper)
                {
                    ConfigInfo = DeserializeInfo(ConfigFilePath, typeof(BaseConfigInfo));
                }
            }

            return ConfigInfo as BaseConfigInfo;
        }
    }
}
