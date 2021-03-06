﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2018 by Sergey V. Zhdanovskih.
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

using GKCore.MVP;
using GKCore.MVP.Views;

namespace GKCore.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CircleChartWinController : FormController<ICircleChartWin>
    {

        public CircleChartWinController(ICircleChartWin view) : base(view)
        {
        }

        public override void UpdateView()
        {
        }

        // TODO: update localization
        public void SaveSnapshot()
        {
            string filters = LangMan.LS(LSID.LSID_TreeImagesFilter) + "|SVG files (*.svg)|*.svg";
            string fileName = AppHost.StdDialogs.GetSaveFile("", "", filters, 2, "jpg", "");
            if (!string.IsNullOrEmpty(fileName)) {
                fView.CircleChart.SaveSnapshot(fileName);
            }
        }
    }
}
