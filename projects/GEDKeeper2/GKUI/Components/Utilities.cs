﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2017 by Sergey V. Zhdanovskih.
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

using GKCommon;
using GKCore.UIContracts;

namespace GKUI.Components
{
    /// <summary>
    /// 
    /// </summary>
    public class Utilities : IUtilities
    {
        public Utilities()
        {
        }

        #region Executing environment

        public Assembly GetExecutingAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        public Version GetAppVersion()
        {
            return GetExecutingAssembly().GetName().Version;
        }

        public string GetAppCopyright()
        {
            var attr = SysUtils.GetAssemblyAttribute<AssemblyCopyrightAttribute>(GetExecutingAssembly());
            return (attr == null) ? string.Empty : attr.Copyright;
        }

        #endregion

        #region KeyLayout functions

        public int GetKeyLayout()
        {
            #if __MonoCS__
            // There is a bug in Mono: does not work this CurrentInputLanguage
            return CultureInfo.CurrentUICulture.KeyboardLayoutId;
            #else
            InputLanguage currentLang = InputLanguage.CurrentInputLanguage;
            return currentLang.Culture.KeyboardLayoutId;
            #endif
        }

        public void SetKeyLayout(int layout)
        {
            try {
                CultureInfo cultureInfo = new CultureInfo(layout);
                InputLanguage currentLang = InputLanguage.FromCulture(cultureInfo);
                InputLanguage.CurrentInputLanguage = currentLang;
            } catch (Exception ex) {
                Logger.LogWrite("Utilities.SetKeyLayout(): " + ex.Message);
            }
        }

        #endregion
    }
}
