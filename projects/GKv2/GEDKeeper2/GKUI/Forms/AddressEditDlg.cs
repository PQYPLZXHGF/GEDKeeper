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

using System;
using System.Windows.Forms;

using GKCommon.GEDCOM;
using GKCore;
using GKCore.Controllers;
using GKCore.Interfaces;
using GKCore.Lists;
using GKCore.MVP.Controls;
using GKCore.MVP.Views;
using GKCore.Types;
using GKUI.Components;

namespace GKUI.Forms
{
    public sealed partial class AddressEditDlg : EditorDialog, IAddressEditDlg
    {
        private readonly AddressEditDlgController fController;

        private readonly GKSheetList fPhonesList;
        private readonly GKSheetList fMailsList;
        private readonly GKSheetList fWebsList;

        public GEDCOMAddress Address
        {
            get { return fController.Address; }
            set { fController.Address = value; }
        }

        #region View Interface

        ISheetList IAddressEditDlg.PhonesList
        {
            get { return fPhonesList; }
        }

        ISheetList IAddressEditDlg.MailsList
        {
            get { return fMailsList; }
        }

        ISheetList IAddressEditDlg.WebsList
        {
            get { return fWebsList; }
        }


        ITextBoxHandler IAddressEditDlg.Country
        {
            get { return GetControlHandler<ITextBoxHandler>(txtCountry); }
        }

        ITextBoxHandler IAddressEditDlg.State
        {
            get { return GetControlHandler<ITextBoxHandler>(txtState); }
        }

        ITextBoxHandler IAddressEditDlg.City
        {
            get { return GetControlHandler<ITextBoxHandler>(txtCity); }
        }

        ITextBoxHandler IAddressEditDlg.PostalCode
        {
            get { return GetControlHandler<ITextBoxHandler>(txtPostalCode); }
        }

        ITextBoxHandler IAddressEditDlg.AddressLine
        {
            get { return GetControlHandler<ITextBoxHandler>(txtAddress); }
        }

        #endregion

        private void ListModify(object sender, ModifyEventArgs eArgs)
        {
            GEDCOMTag itemTag = eArgs.ItemData as GEDCOMTag;
            if ((eArgs.Action == RecordAction.raEdit || eArgs.Action == RecordAction.raDelete) && (itemTag == null)) return;

            if (sender == fPhonesList) {
                fController.DoPhonesAction(eArgs.Action, itemTag);
            } else if (sender == fMailsList) {
                fController.DoMailsAction(eArgs.Action, itemTag);
            } else if (sender == fWebsList) {
                fController.DoWebsAction(eArgs.Action, itemTag);
            }
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            DialogResult = fController.Accept() ? DialogResult.OK : DialogResult.None;
        }

        public AddressEditDlg(IBaseWindow baseWin)
        {
            InitializeComponent();

            btnAccept.Image = UIHelper.LoadResourceImage("Resources.btn_accept.gif");
            btnCancel.Image = UIHelper.LoadResourceImage("Resources.btn_cancel.gif");

            fPhonesList = new GKSheetList(pagePhones);
            fPhonesList.SetControlName("fPhonesList"); // for purpose of tests
            fPhonesList.OnModify += ListModify;
            fPhonesList.AddColumn(LangMan.LS(LSID.LSID_Telephone), 350, false);

            fMailsList = new GKSheetList(pageEmails);
            fMailsList.SetControlName("fMailsList"); // for purpose of tests
            fMailsList.OnModify += ListModify;
            fMailsList.AddColumn(LangMan.LS(LSID.LSID_Mail), 350, false);

            fWebsList = new GKSheetList(pageWebPages);
            fWebsList.SetControlName("fWebsList"); // for purpose of tests
            fWebsList.OnModify += ListModify;
            fWebsList.AddColumn(LangMan.LS(LSID.LSID_WebSite), 350, false);

            // SetLang()
            btnAccept.Text = LangMan.LS(LSID.LSID_DlgAccept);
            btnCancel.Text = LangMan.LS(LSID.LSID_DlgCancel);
            Text = LangMan.LS(LSID.LSID_Address);
            pageCommon.Text = LangMan.LS(LSID.LSID_Address);
            lblCountry.Text = LangMan.LS(LSID.LSID_AdCountry);
            lblState.Text = LangMan.LS(LSID.LSID_AdState);
            lblCity.Text = LangMan.LS(LSID.LSID_AdCity);
            lblPostalCode.Text = LangMan.LS(LSID.LSID_AdPostalCode);
            lblAddress.Text = LangMan.LS(LSID.LSID_Address);
            pagePhones.Text = LangMan.LS(LSID.LSID_Telephones);
            pageEmails.Text = LangMan.LS(LSID.LSID_EMails);
            pageWebPages.Text = LangMan.LS(LSID.LSID_WebSites);

            fController = new AddressEditDlgController(this);
            fController.Init(baseWin);
        }
    }
}
