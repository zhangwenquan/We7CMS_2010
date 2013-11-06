﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using We7.CMS.Common.Enum;
using We7.CMS.Utils.Helpers;
using We7.Framework;

namespace We7.CMS
{
    public class BasePage : Page
    {
        protected virtual MasterPageMode MasterPageIs
        {
            get { return MasterPageMode.FullMenu; }
        }

        protected virtual bool NeedAnAccount
        {
            get { return true; }
        }

        protected virtual bool NeedAnPermission
        {
            get { return true; }
        }

        protected SiteSettingHelper CDHelper
        {
            get { return HelperFactory.GetHelper<SiteSettingHelper>(); }
        }
    }
}