﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2017 by Sergey V. Zhdanovskih.
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
using System.Collections.Generic;
using System.Windows.Forms;
using GKCommon.GEDCOM;
using GKCommon.TreeMap;
using GKCore.Interfaces;
using GKCore.Tools;

namespace GKDataQualityPlugin
{
    public partial class DataQualityWidget : Form, ILocalization
    {
        private DQViewer fDataMap;
        private readonly Plugin fPlugin;
        private IBaseWindow fBase;

        public DataQualityWidget(Plugin plugin)
        {
            InitializeComponent();

            fPlugin = plugin;
            fDataMap = new DQViewer();
            fDataMap.Dock = DockStyle.Fill;
            Controls.Add(fDataMap);

            SetLang();
        }

        private MapItem CreateItem(MapItem parent, string name, double size, float quality)
        {
            var item = fDataMap.Model.CreateItem(parent, name, size) as MapFile;

            double wavelength = Spectrum.ColdWavelength + (Spectrum.WavelengthMaximum - Spectrum.ColdWavelength) * (1.0f - quality);
            item.Color = Spectrum.WavelengthToRGB(wavelength);

            return item;
        }

        private void DataQualityWidget_Load(object sender, EventArgs e)
        {
            fPlugin.Host.WidgetShow(fPlugin);
            BaseChanged(fPlugin.Host.GetCurrentFile());
        }

        private void DataQualityWidget_Closed(object sender, EventArgs e)
        {
            BaseChanged(null);
            fPlugin.Host.WidgetClose(fPlugin);
        }

        public void BaseChanged(IBaseWindow baseWin)
        {
            if (fBase != baseWin) {
                fBase = baseWin;
                UpdateTreeMap();
            }
        }

        private void UpdateTreeMap()
        {
            fDataMap.Model.Clear();
            if (fBase == null) {
                fDataMap.Invalidate();
                return;
            }

            GEDCOMTree tree = fBase.Context.Tree;
            List<GEDCOMIndividualRecord> prepared = new List<GEDCOMIndividualRecord>();
            List<GEDCOMRecord> groupRecords = new List<GEDCOMRecord>();
            try {
                int groupNum = 0;
                int num = tree.RecordsCount;
                for (int i = 0; i < num; i++) {
                    GEDCOMRecord rec = tree[i];

                    if (rec.RecordType == GEDCOMRecordType.rtIndividual) {
                        GEDCOMIndividualRecord iRec = rec as GEDCOMIndividualRecord;
                        if (prepared.IndexOf(iRec) < 0) {
                            groupNum++;
                            groupRecords.Clear();

                            TreeTools.WalkTree(iRec, TreeTools.TreeWalkMode.twmAll, groupRecords);

                            int groupSize = groupRecords.Count;
                            float quality = 0.0f;
                            for (int j = 0; j < groupSize; j++) {
                                iRec = (GEDCOMIndividualRecord)groupRecords[j];
                                prepared.Add(iRec);

                                quality += iRec.GetCertaintyAssessment();
                            }
                            quality /= groupSize;

                            string name = groupNum.ToString() + " group (" + groupSize.ToString() + ") " + quality.ToString("0.00");

                            CreateItem(null, name, groupSize, quality);
                        }
                    }
                }
            } finally {
                groupRecords.Clear();
            }

            fDataMap.UpdateView();
        }

        #region ILocalization support

        public void SetLang()
        {
            Text = fPlugin.LangMan.LS(CLS.LSID_Title);
        }

        #endregion
    }
}