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
using Eto.Forms;
using GKCommon;
using GKCommon.GEDCOM;
using GKCore;
using GKCore.Interfaces;
using GKCore.Options;
using GKCore.Types;
using GKCore.UIContracts;
using GKUI.Components;

namespace GKUI.Forms
{
    public partial class PersonalNameEditDlg: EditorDialog, IPersonalNameEditDlg
    {
        private GEDCOMPersonalName fPersonalName;

        public GEDCOMPersonalName PersonalName
        {
            get { return fPersonalName; }
            set { SetPersonalName(value); }
        }


        private void SetPersonalName(GEDCOMPersonalName value)
        {
            fPersonalName = value;
            try {
                UpdateControls();
            } catch (Exception ex) {
                Logger.LogWrite("PersonalNameEditDlg.SetPersonalName(): " + ex.Message);
            }
        }

        private void edName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyChar == '/') {
                e.Handled = true;
            }
        }

        private bool IsExtendedWomanSurname()
        {
            GEDCOMIndividualRecord iRec = fPersonalName.Parent as GEDCOMIndividualRecord;

            bool result = (GlobalOptions.Instance.WomanSurnameFormat != WomanSurnameFormat.wsfNotExtend) &&
                (iRec.Sex == GEDCOMSex.svFemale);
            return result;
        }

        private void UpdateControls()
        {
            GEDCOMIndividualRecord iRec = fPersonalName.Parent as GEDCOMIndividualRecord;

            var parts = GKUtils.GetNameParts(iRec, fPersonalName);

            txtSurname.Text = parts.Surname;
            txtName.Text = parts.Name;
            txtPatronymic.Text = parts.Patronymic;
            cmbNameType.SelectedIndex = (int)fPersonalName.NameType;

            txtNamePrefix.Text = fPersonalName.Pieces.Prefix;
            txtNickname.Text = fPersonalName.Pieces.Nickname;
            txtSurnamePrefix.Text = fPersonalName.Pieces.SurnamePrefix;
            txtNameSuffix.Text = fPersonalName.Pieces.Suffix;

            txtMarriedSurname.Text = fPersonalName.Pieces.MarriedName;

            if (!IsExtendedWomanSurname()) {
                lblSurname.Text = LangMan.LS(LSID.LSID_Surname);
                txtMarriedSurname.Enabled = false;
            } else {
                lblSurname.Text = LangMan.LS(LSID.LSID_MaidenSurname);
                txtMarriedSurname.Enabled = true;
            }

            ICulture culture = fBase.Context.Culture;
            txtSurname.Enabled = txtSurname.Enabled && culture.HasSurname();
            txtPatronymic.Enabled = txtPatronymic.Enabled && culture.HasPatronymic();

            GEDCOMLanguageID langID = fPersonalName.Language.Value;
            cmbLanguage.Text = GEDCOMLanguageEnum.Instance.GetStrValue(langID);
        }

        private void AcceptChanges()
        {
            GKUtils.SetNameParts(fPersonalName, txtSurname.Text, txtName.Text, txtPatronymic.Text);

            GEDCOMPersonalNamePieces pieces = fPersonalName.Pieces;
            pieces.Nickname = txtNickname.Text;
            pieces.Prefix = txtNamePrefix.Text;
            pieces.SurnamePrefix = txtSurnamePrefix.Text;
            pieces.Suffix = txtNameSuffix.Text;

            fPersonalName.NameType = (GEDCOMNameType)cmbNameType.SelectedIndex;

            var item = (GKComboItem)cmbLanguage.Items[cmbLanguage.SelectedIndex];
            var langID = (GEDCOMLanguageID)item.Tag;
            fPersonalName.Language.Value = langID;

            fBase.Context.CollectNameLangs(fPersonalName);
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            try {
                AcceptChanges();
                DialogResult = DialogResult.Ok;
            } catch (Exception ex) {
                Logger.LogWrite("PersonalNameEditDlg.btnAccept_Click(): " + ex.Message);
                DialogResult = DialogResult.None;
            }
        }

        public PersonalNameEditDlg()
        {
            InitializeComponent();

            for (GEDCOMNameType nt = GEDCOMNameType.ntNone; nt <= GEDCOMNameType.ntMarried; nt++) {
                cmbNameType.Items.Add(LangMan.LS(GKData.NameTypes[(int)nt]));
            }

            for (var lid = GEDCOMLanguageID.Unknown; lid < GEDCOMLanguageEnum.LastVal; lid++) {
                cmbLanguage.Items.Add(new GKComboItem(GEDCOMLanguageEnum.Instance.GetStrValue(lid), lid));
            }
            cmbLanguage.SortItems();

            SetLang();
        }

        public void SetLang()
        {
            Title = LangMan.LS(LSID.LSID_Name);
            btnAccept.Text = LangMan.LS(LSID.LSID_DlgAccept);
            btnCancel.Text = LangMan.LS(LSID.LSID_DlgCancel);
            lblSurname.Text = LangMan.LS(LSID.LSID_Surname);
            lblMarriedSurname.Text = LangMan.LS(LSID.LSID_MarriedSurname);
            lblName.Text = LangMan.LS(LSID.LSID_Name);
            lblPatronymic.Text = LangMan.LS(LSID.LSID_Patronymic);
            lblNickname.Text = LangMan.LS(LSID.LSID_Nickname);
            lblSurnamePrefix.Text = LangMan.LS(LSID.LSID_SurnamePrefix);
            lblNamePrefix.Text = LangMan.LS(LSID.LSID_NamePrefix);
            lblNameSuffix.Text = LangMan.LS(LSID.LSID_NameSuffix);
            lblType.Text = LangMan.LS(LSID.LSID_Type);
            lblLanguage.Text = LangMan.LS(LSID.LSID_Language);
        }
    }
}
