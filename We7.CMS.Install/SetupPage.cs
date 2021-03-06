﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using We7.Framework.Config;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace We7.CMS.Install
{
    public class SetupPage : System.Web.UI.Page
    {
        private static FileVersionInfo AssemblyFileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        public static string footer = "";

        public static string header = "";

        public static string logo = "<img src=\"images/logo.jpg\" width=\"180\" height=\"300\">";

        public static string productName = "";

        public static string SelectDB = "";

        public static void CreateLockFile()
        {
            HttpContext context = HttpContext.Current;
            string physicalPath = string.Empty;
            if (context != null)
            {
                physicalPath = context.Server.MapPath("/Config");
            }
            else
            {
                physicalPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            using (FileStream fs = new FileStream(physicalPath+"\\db-is-creating.lock", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs.Close();
            }

        }

        public static bool LockFileExist()
        {
            HttpContext context = HttpContext.Current;
            string physicsPath = null;
            if (context != null)
            {
                physicsPath = context.Server.MapPath("/Config");
            }
            else
            {
                physicsPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            return File.Exists(physicsPath + "\\db-is-creating.lock");
        }

        private static void checkDataFilePath()
        {
            HttpContext context = HttpContext.Current;
            string physicsPath = null;
            if (context != null)
                physicsPath = context.Server.MapPath("/");
            else
                physicsPath = AppDomain.CurrentDomain.BaseDirectory;

            string mypath = Path.Combine(physicsPath, "_data");
            if (!Directory.Exists(mypath))
                Directory.CreateDirectory(mypath);
            mypath = Path.Combine(physicsPath, "_skins");
            if (!Directory.Exists(mypath))
                Directory.CreateDirectory(mypath);
        }

        public static void DeleteLockFile()
        {
            HttpContext context = HttpContext.Current;
            string physicPath = "";
            if (context != null)
            {
                physicPath = context.Server.MapPath("/Config");
            }
            else
            {
                physicPath = AppDomain.CurrentDomain.BaseDirectory; //为什么与上面的路径要不同，为了区别二者故意这样吗？
            }
            if (File.Exists(Path.Combine(physicPath, "db-is-creating.lock")))
            {
                File.Delete(Path.Combine(physicPath, "db-is-creating.lock"));
            }
        }

        public void DisableSubmitButton(Page page, Button submitBtn)
        {
            RegisterAdminPageClientScriptBlock();

            page.ClientScript.GetPostBackEventReference(submitBtn, "");

            StringBuilder sb = new StringBuilder();
            sb.Append("if(typeof(Page_ClientValidate)=='function') { if (Page_ClientValidate() == false) { return false; }}");
            sb.Append("disableOtherSubmit();");
            sb.Append("document.getElementById('success').style.display='block';");
            sb.Append(this.GetPostBackEventReference(submitBtn, ""));
            sb.Append(";");
            submitBtn.Attributes.Add("onclick", sb.ToString());
        }

        public static string IISSystemBINCheck(ref bool error)
        {
            string binfolderpath = HttpRuntime.BinDirectory;
            string result = "";

            try
            {
                string[] assemblyList = new string[]{ "We7.CMS.Config.dll", "We7.CMS.Utils.dll", "We7.CMS.Web.dll", "We7.Framework.dll"};
                bool isAssemblyInExistence = false;
                ArrayList inExistenceAssemblyList = new ArrayList();
                foreach (string assembly in assemblyList)
                {
                    if (!File.Exists(binfolderpath + assembly))
                    {
                        isAssemblyInExistence = true;
                        error = true;
                        inExistenceAssemblyList.Add(assembly);
                    }
                }
                if (isAssemblyInExistence)
                {
                    foreach (string assembly in inExistenceAssemblyList)
                    {
                        result += "<tr><td bgcolor='#ffffff' width='5%'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>"+assembly+" 文件放置不正确<br/>请将所有的dll文件复制到目录 "+binfolderpath+" 中.</td></tr>";
                    }
                }
            }
            catch
            {
                result += "<tr><td bgcolor='#ffffff' width='5%'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>请将所有的dll文件复制到目录 " + binfolderpath + " 中.</td></tr>";
                error = true;
            }

            return result;
        }

        public static void Init()
        {
            header = "<HEAD><title>安装"+GetAssemblyProductName()+"</title><meta http-equiv=\"Content-Type\" content=\"text/html\" charset=\"utf-8\">\r\n";
            header += "<LINK rev=\"stylesheet\" media=\"all\" href=\"css/general.css\" type=\"text/css\" rel=\"stylesheet\">\r\n";
            header += "<script type=\"text/javascript\" src=\"js/setup.js\"></script>\r\n";
            GeneralConfigInfo gi = GeneralConfigs.GetConfig();

            if (gi != null)
            {
                string copyright = gi.CopyrightOfWe7;
                if (gi.IsOEM)
                    copyright = gi.Copyright;
                footer = string.Format("<div class='pubfooter'><p>{0}</p></div>", copyright);
            }
            else
            {
                footer = "<div class='pubfooter'><p>Powered by <a href=\"http://we7.cn/\" target=\"_blank\">"+GetAssemblyProductName()+"</a>";
                footer += " &nbsp; &copy;"+GetAssemblyCopyright().Split(',')[0]+"<a href=\"http://www.westEngine.com/\" target=\"_blank\">WestEngine Inc.</a></p></div>";
            }

            productName = GetAssemblyProductName();
        }

        public static string InitialSystemValidCheck(ref bool error)
        {
            error = false;
            StringBuilder sb = new StringBuilder();
            sb.Append("<table width='90%' border='0' cellpadding='0' cellspacing='0' align='center' bgcolor='#666666' style='font-size:12px;'>");
            
            HttpContext context = HttpContext.Current;

            string filename = "";
            if (context != null)
            {
                filename = context.Server.MapPath("/Config/db.config");
            }
            else
            {
                filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Config/db.config");
            }

            sb.Append(IISSystemBINCheck(ref error));

            if (!SystemConfigCheck())
            {
                sb.Append("<tr style=\"height:15px;\"><td width='5%' bgcolor='#ffffff'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'> db.config 不可写或没有正确放置，相关问题详见安装文档.</td></tr>");
                error = true;
            }
            else
            {
                sb.Append("<tr style=\"height:15px;\"><td width='5%' bgcolor='#ffffff'><img src='images/ok.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>对 db.config 验证通过!</td></tr>");
            }

            if (!SystemConfigCheck())
            {
                sb.Append("您对系统配置文件db.config没有写入权限!<br/>");
            }

            checkDataFilePath();

            string folderstr = "config,_data,_skins";
            foreach (string folder in folderstr.Split(','))
            {
                if (!SystemFolderCheck(folder))
                {
                    sb.Append("<tr><td width='5%' bgcolor='#ffffff'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>对 "+folder+" 目录没有写入和删除权限!</td></tr>");
                    error = true;
                }
                else
                {
                    sb.Append("<tr><td width='5%' bgcolor='#ffffff'><img src='images/ok.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>对 "+folder+" 目录权限验证通过!</td></tr>");
                }
            }

            string filestr = "install\\systemfile.aspx";
            foreach (string file in filestr.Split(','))
            {
                if (!SystemFileCheck(file))
                {
                    sb.Append("<tr><td width='5%' bgcolor='#ffffff'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>对 " + file.Substring(0, file.LastIndexOf('\\')) + " 目录没有写入和删除权限!</td></tr>");
                    error = true;
                }
                else
                {
                    sb.Append("<tr><td width='5%' bgcolor='#ffffff'><img src='images/ok.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>对 " + file.Substring(0, file.LastIndexOf('\\')) + " 目录权限验证通过!</td></tr>");
                }
            }

            if (!tempTest())
            {
                sb.Append("<tr><td width='5%' bgcolor='#ffffff'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>您没有对 " + Path.GetTempPath() + " 文件夹访问权限，详情参见安装文档!</td></tr>");
                error = true;
            }
            else
            {
                if (!serializeTest())
                {
                    sb.Append("<tr><td width='5%' bgcolor='#ffffff'><img src='images/error.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'>对config文件反序列化失败，请确保config下的文件具有可写权限及格式正确.</td></tr>");
                    error = true;
                }
                else
                {
                    sb.Append("<tr><td bgcolor='#ffffff' width='5%'><img src='images/ok.gif' width='16' height='16'></td><td bgcolor='#ffffff' width='95%'  style='color:#080'>反序列化验证通过！</td></tr>");
                }
            }
            sb.Append("</table>");

            return sb.ToString();
        }

        public static string GetAssemblyCopyright()
        {
            return AssemblyFileVersion.LegalCopyright;
        }

        public static string GetAssemblyProductName()
        {
            GeneralConfigInfo gi = GeneralConfigs.GetConfig();
            if (gi != null)
            {
                return gi.ProductName;
            }
            else
            {
                return AssemblyFileVersion.ProductName;
            }
        }

        public void RegisterAdminPageClientScriptBlock()
        {
            string script = "<div id=\"success\" style=\"position:absolute;z-index;300;height:120px;width:284px;left:50%;top:50%;margin-left:-150px;margin-top:-80px;\">\r\n" +
                "<div id=\"Layer2\" style=\"position:absolute;z-index:300;width:270px;height:90px;background-color:#ffffff;border:solid #000000 1px;font-size:14px;\">\r\n" +
                "   <div id=\"Layer4\" style=\"height:26px;background:#333333;line-height:26px;padding:0px 3px 0px 3px;font-weight:bolder;color:#fff;\">操作提示</div>\r\n" +
                "   <div id=\"Layer5\" style=\"height:64px;line-height:150%;padding:0px 3px 0px 3px;\" align=\"center\"><br/>正在执行操作，请稍等...</div>\r\n" +
                "</div>\r\n" +
                "<div id=\"Layer3\" style=\"position:absolute;width:270px;height:90px;z-index:299;left:4px;top:5px;background-color:#cccccc;\"></div>\r\n" +
                "</div>\r\n" +
                "<script>\r\n" +
                "document.getElementById('success').style.display='none';\r\n" +
                "</script>\r\n";

            base.ClientScript.RegisterClientScriptBlock(this.GetType(), "", script);
        }

        private static bool serializeTest()
        {
            try
            {
                string configPath = HttpContext.Current.Server.MapPath("/config/general.config");
                GeneralConfigInfo __configinfo = new GeneralConfigInfo();
                if (!File.Exists(configPath))
                    GeneralConfigs.Serialize(__configinfo, configPath);
                __configinfo = GeneralConfigs.Deserialize(configPath);
                GeneralConfigs.Serialize(__configinfo, configPath);

                return true;
            }
            catch
            {
                return false;
            }
            
        }

        public static bool SystemConfigCheck()
        {
            HttpContext context = HttpContext.Current;
            string physicsPath = "";

            if (context != null)
            {
                physicsPath = context.Server.MapPath("/config");
            }
            else
            {
                physicsPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            try
            {
                using (FileStream fs = new FileStream(physicsPath + "\\a.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fs.Close();
                }
                File.Delete(physicsPath + "\\a.txt");

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SystemFileCheck(string filename)
        {
            filename = HttpContext.Current.Server.MapPath(@"../"+filename);
            try
            {
                if (filename.IndexOf("systemfile.aspx") == -1 && !File.Exists(filename))
                    return false;
                if (filename.IndexOf("systemfile.aspx") != -1)
                {
                    File.Delete(filename);
                    return true;
                }
                StreamReader sr = new StreamReader(filename);
                string content = sr.ReadToEnd();
                sr.Close();
                StreamWriter sw = new StreamWriter(filename, false);
                sw.Write(content);
                sw.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SystemFolderCheck(string folder)
        {
            HttpContext context = HttpContext.Current;
            string physicsPath = "";

            if (context != null)
            {
                physicsPath = context.Server.MapPath(@"../"+folder);
            }
            else
            {
                physicsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../"+folder);
            }

            try
            {
                using (FileStream fs = new FileStream(physicsPath + "\\a.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fs.Close();
                }
                if (File.Exists(physicsPath + "\\a.txt"))
                {
                    File.Delete(physicsPath + "\\a.txt");
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch
            {
                return false;
            }
        }

        private static bool tempTest()
        {
            string userGuid = Guid.NewGuid().ToString();
            string tempPath = Path.GetTempPath();
            string path = tempPath + userGuid;

            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine(DateTime.Now);
                }
                using (StreamReader sw = new StreamReader(path))
                {
                    sw.ReadLine();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}