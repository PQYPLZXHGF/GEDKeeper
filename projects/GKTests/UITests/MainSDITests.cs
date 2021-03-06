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

#if !__MonoCS__

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BSLib;
using GKCommon.GEDCOM;
using GKCore;
using GKCore.Charts;
using GKCore.Export;
using GKCore.Interfaces;
using GKCore.Lists;
using GKCore.Options;
using GKCore.Types;
using GKTests;
using GKTests.ControlTesters;
using GKUI.Components;
using GKUI.Forms;
using GKUI.Providers;
using NUnit.Extensions.Forms;
using NUnit.Framework;

namespace GKUI.Forms
{
    /// <summary>
    /// Tests for the main application window. Dependent calls of other windows
    /// and dialogs that are heavily dependent on the main window.
    /// </summary>
    [TestFixture]
    public class MainSDITests : CustomWindowTest
    {
        private Form fMainWin;
        private IBaseWindow fCurBase;
        private int fIndex;

        public override void Setup()
        {
            base.Setup();

            WFAppHost.ConfigureBootstrap(false);

            var appHost = new WFAppHost();
            appHost.Init(null, false);

            var indiCols = GlobalOptions.Instance.IndividualListColumns;
            for (int i = 0; i < indiCols.Count; i++) {
                var colProps = indiCols[i];
                colProps.CurActive = true;
            }
        }

        [STAThread, Test]
        public void Test_Common()
        {
            // required for testing, otherwise the engine will require saving
            // the database (requires path of files for the archive and storage)
            GlobalOptions.Instance.AllowMediaStoreReferences = true;

            var appHost = (WFAppHost)AppHost.Instance;
            Assert.IsNotNull(appHost.AppContext);

            appHost.BaseClosed(null);
            appHost.CloseWindow(null);
            appHost.SaveWinMRU(null);

            //

            // at complex tests, first form hasn't focus
            ((Form)AppHost.Instance.RunningForms[0]).Show(); // FIXME

            fMainWin = (Form)AppHost.Instance.GetActiveWindow();

            // Stage 1: call to AboutDlg, closing in AboutDlg_Handler
            ExpectModal("AboutDlg", "AboutDlg_Handler");
            ClickToolStripMenuItem("miAbout", fMainWin);


            // Stage 2.1: GetCurrentFile()
            IBaseWindow curBase = AppHost.Instance.GetCurrentFile();
            Assert.IsNotNull(curBase, "Stage 2.1");
            Assert.AreEqual(fMainWin, curBase);

            // Stage 2.2: create an empty base
            //ClickToolStripButton("tbFileNew", fBaseSDI);

            // Stage 2.3: GetCurrentFile()
            fCurBase = AppHost.Instance.GetCurrentFile();
            Assert.IsNotNull(fCurBase, "Stage 2.3");

            // Stage 2.4: fill context for sample data
            TestUtils.FillContext(fCurBase.Context);
            fCurBase.UpdateSettings();

            // Stage 2.5: select first individual record in base
            fCurBase.SelectRecordByXRef("I1");
            Assert.AreEqual("I1", fCurBase.GetSelectedPerson().XRef);

            // Stage 3: call to FilePropertiesDlg
            ModalFormHandler = Dialog_Cancel_Handler;
            ClickToolStripMenuItem("miFileProperties", fMainWin);
            ModalFormHandler = FilePropertiesDlg_btnAccept_Handler;
            ClickToolStripMenuItem("miFileProperties", fMainWin);


            // Stage 4: call to OptionsDlg
            ModalFormHandler = Dialog_Cancel_Handler;
            ClickToolStripMenuItem("miOptions", fMainWin);
            ModalFormHandler = OptionsDlg_btnAccept_Handler;
            ClickToolStripMenuItem("miOptions", fMainWin);


            // Stage 5: internals of BaseWin
            BaseWin_Tests(fCurBase, "Stage 5");


            // Stage 6
            MainWin_Test();


            // Stage 7: call to QuickFind
            ((BaseWinSDI)fCurBase).ShowRecordsTab(GEDCOMRecordType.rtIndividual);
            QuickSearch_Test();


            // Stage 21: call to TreeToolsWin
            ModalFormHandler = TreeCompareDlg_Handler;
            ClickToolStripMenuItem("miTreeCompare", fMainWin);

            ModalFormHandler = TreeMergeDlg_Handler;
            ClickToolStripMenuItem("miTreeMerge", fMainWin);

            ModalFormHandler = TreeSplitDlg_Handler;
            ClickToolStripMenuItem("miTreeSplit", fMainWin);

            ModalFormHandler = RecMergeDlg_Handler;
            ClickToolStripMenuItem("miRecMerge", fMainWin);

            ModalFormHandler = FamilyGroupsDlg_Handler;
            ClickToolStripMenuItem("miFamilyGroups", fMainWin);

            ModalFormHandler = TreeCheckDlg_Handler;
            ClickToolStripMenuItem("miTreeCheck", fMainWin);

            ModalFormHandler = PatSearchDlg_Handler;
            ClickToolStripMenuItem("miPatSearch", fMainWin);

            ModalFormHandler = PlacesManagerDlg_Handler;
            ClickToolStripMenuItem("miPlacesManager", fMainWin);


            // Stage 22-24: call to exports
            Exporter.TEST_MODE = true;
            GenerateFamilyBook_Tests("Stage 22");
            GenerateExcel_Tests("Stage 23");
            GeneratePedigree_Tests("Stage 24");
            //GenerateTreesAlbum_Tests("Stage 25"); // FIXME: fatal loop


            // Stage 25: call to CircleChartWin (required the base, selected person)
            fCurBase.SelectRecordByXRef("I3");
            Assert.AreEqual("I3", fCurBase.GetSelectedPerson().XRef, "Stage 25.0");
            ClickToolStripMenuItem("miAncestorsCircle", fMainWin);
            CircleChartWin_Tests(GetActiveForm("CircleChartWin"), "Stage 25");

            // Stage 26: call to CircleChartWin (required the base, selected person)
            fCurBase.SelectRecordByXRef("I1");
            Assert.AreEqual("I1", fCurBase.GetSelectedPerson().XRef, "Stage 26.0");
            ClickToolStripMenuItem("miDescendantsCircle", fMainWin);
            CircleChartWin_Tests(GetActiveForm("CircleChartWin"), "Stage 26");


            // Stage 27: call to TreeChartWin (required the base, selected person)
            fCurBase.SelectRecordByXRef("I3");
            Assert.AreEqual("I3", fCurBase.GetSelectedPerson().XRef, "Stage 27.0");
            ClickToolStripButton("tbTreeAncestors", fMainWin);
            TreeChartWin_Tests(GetActiveForm("TreeChartWin"), TreeChartKind.ckAncestors, "Stage 27", "I3");


            // Stage 28: call to TreeChartWin (required the base, selected person)
            fCurBase.SelectRecordByXRef("I1");
            Assert.AreEqual("I1", fCurBase.GetSelectedPerson().XRef, "Stage 28.0");
            ClickToolStripButton("tbTreeDescendants", fMainWin);
            TreeChartWin_Tests(GetActiveForm("TreeChartWin"), TreeChartKind.ckDescendants, "Stage 28", "I1");


            // Stage 29: call to TreeChartWin (required the base, selected person)
            ClickToolStripButton("tbTreeBoth", fMainWin);
            TreeChartWin_Tests(GetActiveForm("TreeChartWin"), TreeChartKind.ckBoth, "Stage 29", "I1");


            // Stage 30: call to StatsWin (required the base)
            ClickToolStripButton("tbStats", fMainWin);
            StatsWin_Tests(GetActiveForm("StatisticsWin"), "Stage 30");


            // Stage 31: call to SlideshowWin (required the base)
            ClickToolStripMenuItem("miSlideshow", fMainWin);
            SlideshowWin_Tests(GetActiveForm("SlideshowWin"), "Stage 31");


            // Stage 32: call to ScriptEditWin (required the base)
            ModalFormHandler = ScriptEditWin_Handler;
            ClickToolStripMenuItem("miScripts", fMainWin);
            //Assert.IsTrue((Form)AppHost.Instance.GetActiveWindow(), "Stage 32");


            // Stage 33: call to OrganizerWin
            ModalFormHandler = OrganizerWin_Handler;
            ClickToolStripMenuItem("miOrganizer", fMainWin);


            // Stage 34: call to RelationshipCalculatorDlg
            ModalFormHandler = RelationshipCalculatorDlg_Handler;
            ClickToolStripMenuItem("miRelationshipCalculator", fMainWin);


            // Stage 35: call to MapsViewerWin (required the base)
            ClickToolStripMenuItem("miMap", fMainWin);
            MapsViewerWin_Tests(GetActiveForm("MapsViewerWin"), "Stage 35");


            // Stage 36
            ModalFormHandler = MessageBox_OkHandler;
            fCurBase.DuplicateRecord();


            // Stage 47: close Base
            ModalFormHandler = MessageBox_CancelHandler;
            ClickToolStripMenuItem("miFileLoad", fMainWin);


            // Stage 48: close Base
            ModalFormHandler = MessageBox_CancelHandler;
            ClickToolStripMenuItem("miFileSaveAs", fMainWin);


            // Stage 49: close Base
            ModalFormHandler = MessageBox_CancelHandler;
            ClickToolStripMenuItem("miFileSave", fMainWin);


            // Stage 50: close Base
            Assert.IsTrue(fCurBase.Context.Modified);
            ModalFormHandler = MessageBox_CancelHandler;
            ClickToolStripMenuItem("miFileClose", fMainWin);


            // Stage 51: call to LanguageSelectDlg
            ModalFormHandler = LanguageSelectDlg_Accept_Handler;
            AppHost.Instance.LoadLanguage(0);


            // Stage 52: exit
            //ClickToolStripMenuItem("miExit", fBaseSDI);


            // Other
            ModalFormHandler = MessageBox_OkHandler;
            AppHost.StdDialogs.ShowMessage("test msg");

            ModalFormHandler = MessageBox_OkHandler;
            AppHost.StdDialogs.ShowError("test error msg");
        }

        private void BaseWin_Tests(IBaseWindow baseWin, string stage)
        {
            // Stage 5: calls to the different Editors
            for (GEDCOMRecordType rt = GEDCOMRecordType.rtIndividual; rt <= GEDCOMRecordType.rtLocation; rt++) {
                Assert.IsNotNull(((BaseWinSDI)baseWin).GetHyperViewByType(rt), stage + ".1");

                baseWin.ShowRecordsTab(rt);

                ModalFormHandler = Dialog_Cancel_Handler;
                ClickToolStripButton("tbRecordAdd", fMainWin);

                ModalFormHandler = EditorDlg_btnAccept_Handler;
                ClickToolStripButton("tbRecordAdd", fMainWin);

                ModalFormHandler = Dialog_Cancel_Handler;
                ClickToolStripButton("tbRecordEdit", fMainWin);

                ModalFormHandler = EditorDlg_btnAccept_Handler;
                ClickToolStripButton("tbRecordEdit", fMainWin);

                IListManager listMan = baseWin.GetRecordsListManByType(rt);
                listMan.AddCondition((byte)PersonColumnType.ctPatriarch, ConditionKind.ck_Contains, "test"); // any first column

                ModalFormHandler = CommonFilterDlg_btnAccept_Handler;
                ClickToolStripButton("tbFilter", fMainWin);
                ModalFormHandler = CommonFilterDlg_btnReset_Handler;
                ClickToolStripButton("tbFilter", fMainWin);
            }

            Assert.IsTrue(baseWin.Context.IsUnknown(), stage + ".2");

            baseWin.ShowRecordsTab(GEDCOMRecordType.rtIndividual);
            baseWin.SelectRecordByXRef("I1");

            GEDCOMRecord record = ((BaseWinSDI)baseWin).GetSelectedRecordEx();
            Assert.IsNotNull(record, stage + ".4");

            StringList recordContent = baseWin.GetRecordContent(record);
            Assert.IsNotNull(recordContent, stage + ".4.1");

            Assert.IsTrue(baseWin.Context.IsAvailableRecord(record), stage + ".5");
            Assert.IsTrue(baseWin.RecordIsFiltered(record), stage + ".6");

            Assert.Throws(typeof(ArgumentNullException), () => { baseWin.ShowMedia(null, false); });
            Assert.Throws(typeof(ArgumentNullException), () => { baseWin.Context.SelectSpouseFor(null); });
            baseWin.NotifyRecord(null, RecordAction.raAdd);

            IList<ISearchResult> search = baseWin.FindAll("Maria");
            Assert.AreEqual(1, search.Count);

            Assert.AreEqual(null, baseWin.Context.GetChildFamily(null, false, null));
            Assert.AreEqual(null, baseWin.Context.AddChildForParent(null, GEDCOMSex.svNone));
            Assert.Throws(typeof(ArgumentNullException), () => { baseWin.Context.AddFamilyForSpouse(null); });

            Assert.Throws(typeof(ArgumentNullException), () => { baseWin.Context.CollectTips(null); });
            baseWin.Context.CollectTips(new StringList());

            Assert.Throws(typeof(ArgumentNullException), () => { baseWin.Context.CheckPersonSex(null); });

            baseWin.NotifyRecord(null, RecordAction.raEdit);

            baseWin.ApplyFilter();

            // default lang for tests is English
            string patr = baseWin.Context.DefinePatronymic("Ivan", GEDCOMSex.svMale, false);
            Assert.AreEqual("", patr);

            ModalFormHandler = SexCheckDlgTests.SexCheckDlgTests_Accept_Handler;
            GEDCOMSex sex = baseWin.Context.DefineSex("Ivan", "Ivanovich");
            Assert.AreEqual(GEDCOMSex.svMale, sex);
        }

        private void MainWin_Test()
        {
            ModalFormHandler = DayTipsDlgTests.CloseModalHandler;
            AppHost.Instance.ShowTips(); // don't show dialog because BirthDays is empty

            AppHost.Instance.AddMRU("test.ged");

            fMainWin.Activate();
            Assert.AreEqual("Unknown", AppHost.Instance.GetCurrentFileName(), "check AppHost.Instance.GetCurrentFileName()");

            //
            Assert.Throws(typeof(ArgumentNullException), () => { AppHost.Instance.RequestGeoCoords(null, null); });
            Assert.Throws(typeof(ArgumentNullException), () => { AppHost.Instance.RequestGeoCoords("Moscow", null); });

            // IHost tests
            //IHost host = fMainWin;
            // FIXME: !!!
            IHost host = AppHost.Instance;

            GlobalOptions.Instance.LastDir = "";
            string ufPath = host.GetUserFilesPath("");
            Assert.AreEqual(GKUtils.GetHomePath(), ufPath);
            Assert.IsFalse(string.IsNullOrEmpty(ufPath));

            IBaseWindow baseWin = host.FindBase("Unknown");
            Assert.IsNotNull(baseWin);

            ModalFormHandler = MessageBox_OkHandler;
            AppHost.StdDialogs.ShowWarning("test warn");


            ILangMan langMan = host.CreateLangMan(null);
            Assert.IsNull(langMan);

            host.WidgetShow(null);
            host.WidgetClose(null);
            Assert.IsFalse(host.IsWidgetActive(null));
            host.EnableWindow(null, false);
            host.BaseRenamed(null, "", "");


            ClickToolStripButton("tbNext", fMainWin);
            ClickToolStripButton("tbPrev", fMainWin);
        }

        private void QuickSearch_Test()
        {
            ClickToolStripMenuItem("miSearch", fMainWin);

            var searchPanel = new FormTester("QuickSearchDlg");
            Form frm = searchPanel.Properties;

            // handlers for empty text
            ClickButton("btnPrev", frm);
            ClickButton("btnNext", frm);
            // enter text
            var txtSearchPattern = new TextBoxTester("txtSearchPattern", frm);
            txtSearchPattern.Enter("John");
            // handlers for entered text? - msgbox processing

            KeyDownForm(frm.Name, Keys.Enter);
            KeyDownForm(frm.Name, Keys.Enter | Keys.Shift);
            KeyDownForm(frm.Name, Keys.Escape);
        }

        #region Exports tests

        private string fFileExt;

        private void GeneratePedigree_Tests(string stage)
        {
            fCurBase.SelectRecordByXRef("I3");
            Assert.AreEqual("I3", fCurBase.GetSelectedPerson().XRef, stage + ".1");

            GeneratePedigree(stage, "miPedigreeAscend");

            fCurBase.SelectRecordByXRef("I1");
            Assert.AreEqual("I1", fCurBase.GetSelectedPerson().XRef, stage + ".2");

            GeneratePedigree(stage, "miPedigree_dAboville");
            GeneratePedigree(stage, "miPedigree_Konovalov");
        }

        private void GeneratePedigree(string stage, string menuItem)
        {
            fFileExt = ".html";
            ModalFormHandler = GeneratePedigree_Handler;
            ClickToolStripMenuItem(menuItem, fMainWin);

            fFileExt = ".rtf";
            ModalFormHandler = GeneratePedigree_Handler;
            ClickToolStripMenuItem(menuItem, fMainWin);

            #if !__MonoCS__
            fFileExt = ".pdf";
            ModalFormHandler = GeneratePedigree_Handler;
            ClickToolStripMenuItem(menuItem, fMainWin);
            #endif
        }

        private void GeneratePedigree_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test" + fFileExt, hWnd);
        }

        private void GenerateExcel_Tests(string stage)
        {
            ModalFormHandler = GenerateExcel_Handler;
            ClickToolStripMenuItem("miExportToExcelFile", fMainWin);
        }

        private void GenerateExcel_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test.xls", hWnd);
        }

        private void GenerateFamilyBook_Tests(string stage)
        {
            ModalFormHandler = GenerateFamilyBook_Handler;
            ClickToolStripMenuItem("miExportToFamilyBook", fMainWin);
        }

        private void GenerateFamilyBook_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test2.pdf", hWnd);
        }

        private void GenerateTreesAlbum_Tests(string stage)
        {
            ModalFormHandler = GenerateTreesAlbum_Handler;
            ClickToolStripMenuItem("miExportToTreesAlbum", fMainWin);
        }

        private void GenerateTreesAlbum_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test3.pdf", hWnd);
        }

        #endregion

        #region AboutDlg handler

        public void AboutDlg_Handler()
        {
            ClickButton("btnClose", "AboutDlg");
        }

        #endregion

        #region FilePropertiesDlg handlers

        private void FilePropertiesDlg_btnAccept_Handler(string name, IntPtr ptr, Form form)
        {
            var txtName = new TextBoxTester("txtName");
            txtName.Enter("sample text");

            ModalFormHandler = LanguageEditDlg_Handler;
            ClickButton("btnLangEdit", form);

            ClickButton("btnAccept", form);

            GEDCOMSubmitterRecord submitter = fCurBase.Context.Tree.Header.Submitter.Value as GEDCOMSubmitterRecord;
            Assert.AreEqual("sample text", submitter.Name.StringValue);
        }

        #endregion

        #region OptionsDlg handlers

        private void OptionsDlg_btnAccept_Handler(string name, IntPtr ptr, Form form)
        {
            var optDlg = ((OptionsDlg)form);
            Assert.AreEqual(GlobalOptions.Instance, optDlg.Options);

            optDlg.SetPage(OptionsPage.opCommon);

            optDlg.SetPage(OptionsPage.opTreeChart);
            var chkPortraitsVisible = new CheckBoxTester("chkPortraitsVisible", form);
            chkPortraitsVisible.Properties.Checked = false;
            chkPortraitsVisible.Properties.Checked = true;

            optDlg.SetPage(OptionsPage.opCircleChart);

            optDlg.SetPage(OptionsPage.opInterface);
            var chkExtendWomanSurnames = new CheckBoxTester("chkExtendWomanSurnames", form);
            chkExtendWomanSurnames.Properties.Checked = true;
            chkExtendWomanSurnames.Properties.Checked = false;

            optDlg.SetPage(OptionsPage.opPedigree);

            ClickButton("btnColumnUp", form);
            ClickButton("btnColumnDown", form);
            ClickButton("btnDefList", form);

            ClickButton("btnAccept", form);
        }

        #endregion

        #region XFilterDlg handlers

        private void CommonFilterDlg_btnAccept_Handler(string name, IntPtr ptr, Form form)
        {
            CommonFilterDlg cfDlg = ((CommonFilterDlg)form);
            Assert.AreEqual(fCurBase, cfDlg.Base);

            IListManager listMan = cfDlg.ListMan;

            var tabsFilters = new TabControlTester("tabsFilters", form);
            tabsFilters.SelectTab(0);

            var dataGridView1 = new DataGridViewTester("dataGridView1", form);
            dataGridView1.SelectCell(0, 0);
            dataGridView1.Properties.BeginEdit(false);
            dataGridView1.Properties.EndEdit();
            dataGridView1.SelectCell(0, 1);
            dataGridView1.Properties.BeginEdit(false);
            dataGridView1.Properties.EndEdit();
            dataGridView1.SelectCell(0, 2);
            dataGridView1.Properties.BeginEdit(false);
            dataGridView1.Properties.EndEdit();

            // Fail: AmbiguousMatch?!
            //dataGridView1.FireEvent("Scroll", new ScrollEventArgs(ScrollEventType.SmallIncrement, 1));

            if (form is PersonsFilterDlg) {
                PersonsFilterDlg_Handler(form);
            }

            ClickButton("btnAccept", form);
        }

        private void CommonFilterDlg_btnReset_Handler(string name, IntPtr ptr, Form form)
        {
            ClickButton("btnReset", form);
            ClickButton("btnAccept", form);
        }

        private void PersonsFilterDlg_Handler(Form form)
        {
            PersonsFilterDlg pfDlg = (PersonsFilterDlg)form;

            var tabs = new TabControlTester("tabsFilters", form);
            tabs.SelectTab(1);

            var rbAliveBefore = new RadioButtonTester("rgLife.rbAliveBefore", form);
            rbAliveBefore.Click();
            var rbAll = new RadioButtonTester("rgLife.rbAll", form);
            rbAll.Click();

            var rbSexMale = new RadioButtonTester("rbSexMale", form);
            rbSexMale.Properties.Checked = true;

            var rbOnlyLive = new RadioButtonTester("rbOnlyLive", form);
            rbOnlyLive.Properties.Checked = true;

            var txtName = new ComboBoxTester("txtName", form);
            txtName.Enter("*Ivan*");

            var cmbResidence = new ComboBoxTester("cmbResidence", form);
            cmbResidence.Enter("*test place*");

            var cmbEventVal = new ComboBoxTester("cmbEventVal", form);
            cmbEventVal.Enter("*test event*");

            var cmbGroup = new ComboBoxTester("cmbGroup", form);
            cmbGroup.Enter("- any -");

            var cmbSource = new ComboBoxTester("cmbSource", form);
            cmbSource.Enter("- any -");
        }

        #endregion

        #region ScriptEditWin handlers

        private void ScriptEditWin_Handler(string name, IntPtr ptr, Form form)
        {
            ScriptEditWin scriptWin = form as ScriptEditWin;
            Assert.AreEqual("unknown.lua", scriptWin.FileName);

            var txtScriptText = new TextBoxTester("txtScriptText");

            txtScriptText.Enter("gk_print(\"Hello\")");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("R = gt_get_records_count()");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("R = gt_get_record(0); rt = gt_get_record_type(R); "+
                                "xref = gt_get_record_xref(R); uid = gt_get_record_uid(R);"+
                                "isf = gt_record_is_filtered(R); tn = gt_get_record_type_name(rt);"+
                                "num = gt_get_records_count();");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("gk_progress_init(1, \"Hello\"); gk_progress_step(); gk_progress_done(); gk_update_view()");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("x = gk_strpos(\"test\", \"alpha test\");");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("indi = gt_create_person(\"Ivan\", \"Ivanovich\", \"Ivanov\", \"M\");"+
                                "gt_set_person_sex(indi, \"M\"); name = gt_get_person_name(indi);" +
                                "gt_add_person_association(indi, \"rel\", indi);" +
                                "assoNum = gt_get_person_associations_count(indi);" +
                                "asso = gt_get_person_association(indi, 0);" +
                                "gt_delete_person_association(indi, 0);" +
                                "evtNum = gt_get_person_events_count(indi);" +
                                "evt = gt_get_person_event(indi, 0);" +
                                "gt_delete_person_event(indi, 0);" +
                                "parentsFam = gt_get_person_parents_family(indi);" +
                                "sx = gt_get_person_sex(indi);" +
                                "evt2 = gt_get_person_event_ex(indi, \"BIRT\");");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("indi = gt_create_person(\"John\", \"\", \"Smith\", \"M\");" +
                                "evt = gt_create_event(indi, \"FACT\");" +
                                "gt_set_event_date(evt, \"08 MAR 1990\");" +
                                "gt_set_event_place(evt, \"sample place\");" +
                                "gt_set_event_value(evt, \"sample value\");" +
                                "ed = gt_get_event_date(evt);" +
                                "en = gt_get_event_name(evt);" +
                                "ep = gt_get_event_place(evt);" +
                                "ev = gt_get_event_value(evt);" +
                                "ey = gt_get_event_year(evt);");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("fam = gt_create_family(); evt = gt_create_event(fam, \"MARR\");" +
                                "R = gt_get_record(0); gt_bind_family_spouse(fam, R); " +
                                "R2 = gt_get_record(1); gt_bind_family_child(fam, R2); " +
                                "chNum = gt_get_family_childs_count(fam); chl = gt_get_family_child(fam, 0);" +
                                "h = gt_get_family_husband(fam); w = gt_get_family_wife(fam);" +
                                "spNum = gt_get_person_spouses_count(R);" +
                                "fam2 = gt_get_person_spouse_family(R, 0);");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("note = gt_create_note(); gt_add_note_text(note, \"test\");" +
                                "R = gt_get_record(0); gt_bind_record_note(R, note); " +
                                "ntNum = gt_get_record_notes_count(R);");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("src = gt_create_source(\"source\");" +
                                "R = gt_get_record(0); gt_bind_record_source(R, src, \"p1\", 1); " +
                                "src = gt_find_source(\"source\");");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("grp = gt_create_group(\"group\");" +
                                "R = gt_get_record(0); gt_bind_group_member(grp, R); " +
                                "gname = gt_get_group_name(grp);" +
                                "gNum = gt_get_person_groups_count(R); grp1 = gt_get_person_group(R, 0);" +
                                "gt_delete_record(grp);");
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("x = gt_get_location_usages(loc);"); // -1
            ClickToolStripButton("tbRun", form);

            txtScriptText.Enter("con = ado_open(\"test\"); qr = ado_query_open(con, \"select * from X\"); "+
                                "ado_query_first(con); ado_query_prev(con);"+
                                "ado_query_next(con); ado_query_last(con);"+
                                "x = ado_get_query_field(con, \"field\");"+
                                "ado_query_close(qr); ado_dump(con);  ado_close(con);");
            ClickToolStripButton("tbRun", form);

            ModalFormHandler = OpenFile_Cancel_Handler;
            txtScriptText.Enter("file = gk_select_file();");
            ClickToolStripButton("tbRun", form);

            ModalFormHandler = Dialog_Cancel_Handler;
            txtScriptText.Enter("R = gt_select_record(rtIndividual);");
            ClickToolStripButton("tbRun", form);

            ModalFormHandler = OpenFile_Cancel_Handler;
            ClickToolStripButton("tbLoadScript", form);

            ModalFormHandler = SaveFile_Cancel_Handler;
            ClickToolStripButton("tbSaveScript", form);

            ModalFormHandler = MessageBox_NoHandler;
            KeyDownForm(form.Name, Keys.Escape);
            form.Dispose();
        }

        #endregion

        #region TreeToolsWin handlers

        private void TreeCompareDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            var radBtn = new RadioButtonTester("radMatchInternal", form);
            radBtn.Click();
            ClickButton("btnMatch", form);

            radBtn = new RadioButtonTester("radAnalysis", form);
            radBtn.Click();
            ClickButton("btnMatch", form);

            radBtn = new RadioButtonTester("radMathExternal", form);
            radBtn.Click();
            ModalFormHandler = OpenFile_Cancel_Handler;
            ClickButton("btnFileChoose", form);
            //ClickButton("btnMatch", form);

            form.Close();
        }

        private void TreeMergeDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            ModalFormHandler = OpenFile_Cancel_Handler;
            ClickButton("btnTreeMerge", form);

            form.Close();
        }

        private void TreeSplitDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            ClickButton("btnSelectFamily", form);

            ClickButton("btnSelectAncestors", form);

            ClickButton("btnSelectDescendants", form);

            ClickButton("btnSelectAll", form);

            ModalFormHandler = SaveFile_Cancel_Handler;
            ClickButton("btnSave", form);

            ModalFormHandler = SaveFile_GED_Handler;
            ClickButton("btnSave", form);

            form.Close();
        }

        private void RecMergeDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            var chkBookmarkMerged = new CheckBoxTester("chkBookmarkMerged", form);
            chkBookmarkMerged.Properties.Checked = true;
            chkBookmarkMerged.Properties.Checked = false;

            var radPersons = new RadioButtonTester("radPersons", form);
            radPersons.Properties.Checked = true;

            RSD_ItemIndex = 0;
            ModalFormHandler = RecordSelectDlg_SelectItem_Handler;
            var btnRec1Sel = new ButtonTester("MergeControl.btnRec1Select", form);
            btnRec1Sel.Click();

            RSD_ItemIndex = 1;
            ModalFormHandler = RecordSelectDlg_SelectItem_Handler;
            var btnRec2Sel = new ButtonTester("MergeControl.btnRec2Select", form);
            btnRec2Sel.Click();

            ClickButton("btnAutoSearch", form);

            ClickButton("btnSkip", form);

            form.Close();
        }

        private void FamilyGroupsDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            ClickButton("btnAnalyseGroups", form);

            form.Close();
        }

        private void TreeCheckDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            ClickButton("btnAnalyseBase", form);
            ClickButton("btnBaseRepair", form);

            form.Close();
        }

        private void PatSearchDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            var edMinGens = new NumericUpDownTester("edMinGens", form);
            edMinGens.EnterValue(1);

            ClickButton("btnPatSearch", form);

            ClickButton("btnSetPatriarch", form);

            ClickButton("btnPatriarchsDiagram", form);
            var pvWin = new FormTester("PatriarchsViewerWin");
            pvWin.Close();

            form.Close();
        }

        private void PlacesManagerDlg_Handler(string name, IntPtr ptr, Form form)
        {
            var tabs = new TabControlTester("tabsTools", form);

            ClickButton("btnAnalysePlaces", form);
            ClickButton("btnIntoList", form);

            form.Close();
        }

        private void SaveFile_GED_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test.ged", hWnd);
        }

        #endregion

        #region OrganizerWin handlers

        private void OrganizerWin_Handler(string name, IntPtr ptr, Form form)
        {
            KeyDownForm(form.Name, Keys.Escape);
            form.Dispose();
        }

        #endregion

        #region RelationshipCalculatorDlg handlers

        private void RelationshipCalculatorDlg_Handler(string name, IntPtr ptr, Form form)
        {
            Assert.IsTrue(fCurBase.Context.Tree.RecordsCount > 1);
            GEDCOMIndividualRecord iRec1 = fCurBase.Context.Tree.XRefIndex_Find("I1") as GEDCOMIndividualRecord;
            Assert.IsNotNull(iRec1);
            GEDCOMIndividualRecord iRec2 = fCurBase.Context.Tree.XRefIndex_Find("I2") as GEDCOMIndividualRecord;
            Assert.IsNotNull(iRec2);

            fIndex = 0;
            ModalFormHandler = RCD_RecordSelectDlg_Select_Handler; // required
            ClickButton("btnRec1Select", form);
            fIndex = 1;
            ModalFormHandler = RCD_RecordSelectDlg_Select_Handler; // required
            ClickButton("btnRec2Select", form);

            var txtResult = new TextBoxTester("txtResult", form);
            // default is not Russian culture
            Assert.AreEqual("Ivanova Maria Petrovna is wife of Ivanov Ivan Ivanovich", txtResult.Text); // :D

            ClickButton("btnClose", form);
        }

        private void RCD_RecordSelectDlg_Select_Handler(string name, IntPtr ptr, Form form)
        {
            var listRecords = new GKRecordsViewTester("fListRecords", form);

            Assert.IsNotNull(listRecords.Properties.ListMan.BaseContext);
            Assert.AreNotEqual(GEDCOMRecordType.rtNone, listRecords.Properties.ListMan.RecordType);

            listRecords.Properties.SelectItem(fIndex);
            ClickButton("btnSelect", form);
        }

        #endregion

        #region LanguageSelectDlg handlers

        private void LanguageSelectDlg_Accept_Handler(string name, IntPtr ptr, Form form)
        {
            ClickButton("btnAccept", form);
        }

        #endregion

        #region LanguageEditDlg handlers

        private void LanguageEditDlg_Handler(string name, IntPtr ptr, Form form)
        {
            ClickButton("btnAccept", form);
        }

        #endregion

        #region EditorDlg handlers

        private static bool NoteEditDlg_FirstCall = true;
        private static bool FamilyEditDlg_FirstCall = true;
        private static bool GroupEditDlg_FirstCall = true;
        private static bool PersonEditDlg_FirstCall = true;
        private static bool ResearchEditDlg_FirstCall = true;
        private static bool LocationEditDlg_FirstCall = true;
        private static bool SourceEditDlg_FirstCall = true;
        private static bool CommunicationEditDlg_FirstCall = true;
        private static bool TaskEditDlg_FirstCall = true;

        public void EditorDlg_btnAccept_Handler(string name, IntPtr ptr, Form form)
        {
            if (NoteEditDlg_FirstCall && form is NoteEditDlg) {
                NoteEditDlg_Handler((NoteEditDlg) form);
                NoteEditDlg_FirstCall = false;
            }

            if (FamilyEditDlg_FirstCall && form is FamilyEditDlg) {
                FamilyEditDlg_Handler((FamilyEditDlg) form);
                FamilyEditDlg_FirstCall = false;
            }

            if (GroupEditDlg_FirstCall && form is GroupEditDlg) {
                GroupEditDlg_Handler((GroupEditDlg) form);
                GroupEditDlg_FirstCall = false;
            }

            if (PersonEditDlg_FirstCall && form is PersonEditDlg) {
                PersonEditDlg_Handler((PersonEditDlg) form);
                PersonEditDlg_FirstCall = false;
            }

            if (ResearchEditDlg_FirstCall && form is ResearchEditDlg) {
                ResearchEditDlg_Handler((ResearchEditDlg) form);
                ResearchEditDlg_FirstCall = false;
            }

            if (LocationEditDlg_FirstCall && form is LocationEditDlg) {
                LocationEditDlg_Handler((LocationEditDlg) form);
                LocationEditDlg_FirstCall = false;
            }

            if (SourceEditDlg_FirstCall && form is SourceEditDlg) {
                SourceEditDlg_Handler((SourceEditDlg) form);
                SourceEditDlg_FirstCall = false;
            }

            if (CommunicationEditDlg_FirstCall && form is CommunicationEditDlg) {
                CommunicationEditDlg_Handler((CommunicationEditDlg) form);
                CommunicationEditDlg_FirstCall = false;
            }

            if (TaskEditDlg_FirstCall && form is TaskEditDlg) {
                TaskEditDlg_Handler((TaskEditDlg) form);
                TaskEditDlg_FirstCall = false;
            }

            ClickButton("btnAccept", form);
        }

        private void StructsDlg_Handler(GEDCOMRecordWithEvents record, Form dlg, TabControlTester tabs, int[] tabIndexes)
        {
            GKSheetListTester sheetTester;

            // notes
            Assert.AreEqual(0, record.Notes.Count);
            tabs.SelectTab(tabIndexes[0]);
            ModalFormHandler = RecordSelectDlg_Select_Handler;
            ClickToolStripButton("fNotesList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, record.Notes.Count);

            sheetTester = new GKSheetListTester("fNotesList");
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fNotesList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, record.Notes.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fNotesList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, record.Notes.Count);

            // media
            Assert.AreEqual(0, record.MultimediaLinks.Count);
            tabs.SelectTab(tabIndexes[1]);
            ModalFormHandler = RecordSelectDlg_Select_Handler;
            ClickToolStripButton("fMediaList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, record.MultimediaLinks.Count);

            sheetTester = new GKSheetListTester("fMediaList");
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fMediaList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, record.MultimediaLinks.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fMediaList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, record.MultimediaLinks.Count);

            // sources
            Assert.AreEqual(0, record.SourceCitations.Count);
            tabs.SelectTab(tabIndexes[2]);
            ModalFormHandler = SourceCitEditDlgTests.AcceptModalHandler;
            ClickToolStripButton("fSourcesList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, record.SourceCitations.Count);

            sheetTester = new GKSheetListTester("fSourcesList");
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fSourcesList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, record.SourceCitations.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fSourcesList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, record.SourceCitations.Count);
        }

        public void NoteEditDlg_Handler(NoteEditDlg dlg)
        {
            var txtNote = new TextBoxTester("txtNote", dlg);
            txtNote.Enter("sample text");
            Assert.AreEqual("sample text", txtNote.Text);
        }

        private void FamilyEditDlg_Handler(FamilyEditDlg dlg)
        {
            GEDCOMFamilyRecord familyRecord = dlg.Family;
            var tabs = new TabControlTester("tabsFamilyData", dlg);
            GKSheetListTester sheetTester;

            // father
            fNeedIndividualSex = GEDCOMSex.svMale;
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnHusbandAdd", dlg);
            ModalFormHandler = MessageBox_YesHandler;
            ClickButton("btnHusbandDelete", dlg);

            // mother
            fNeedIndividualSex = GEDCOMSex.svFemale;
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnWifeAdd", dlg);
            ModalFormHandler = MessageBox_YesHandler;
            ClickButton("btnWifeDelete", dlg);

            // childs
            Assert.AreEqual(0, familyRecord.Children.Count);
            tabs.SelectTab(0);
            fNeedIndividualSex = GEDCOMSex.svFemale;
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickToolStripButton("fChildsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, familyRecord.Children.Count);

            //sheetTester = new GKSheetListTester("fEventsList", dlg);
            //sheetTester.Properties.SelectItem(0);
            //ModalFormHandler = EventEditDlg_Select_Handler;
            //ClickToolStripButton("fChildsList_ToolBar_btnEdit", dlg);
            //Assert.AreEqual(1, familyRecord.Childrens.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester = new GKSheetListTester("fChildsList", dlg);
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fChildsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, familyRecord.Children.Count);

            // events
            Assert.AreEqual(0, familyRecord.Events.Count);
            tabs.SelectTab(1);
            ModalFormHandler = EventEditDlg_Select_Handler;
            ClickToolStripButton("fEventsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, familyRecord.Events.Count);

            sheetTester = new GKSheetListTester("fEventsList", dlg);
            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = EventEditDlg_Select_Handler;
            ClickToolStripButton("fEventsList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, familyRecord.Events.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fEventsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, familyRecord.Events.Count);

            StructsDlg_Handler(familyRecord, dlg, tabs, new int[] { 2, 3, 4 });
        }

        private void PersonEditDlg_Handler(PersonEditDlg dlg)
        {
            GEDCOMIndividualRecord indiRecord = dlg.Person;

            var cmbSex = new ComboBoxTester("cmbSex", dlg);
            cmbSex.Select(1); // male

            var tabs = new TabControlTester("tabsPersonData", dlg);
            GKSheetListTester sheetTester;

            var cmbRestriction = new ComboBoxTester("cmbRestriction", dlg);
            cmbRestriction.Select(3);
            cmbRestriction.Select(2);
            cmbRestriction.Select(1);
            cmbRestriction.Select(0);

            var txtSurname = new TextBoxTester("txtSurname", dlg);
            txtSurname.FireEvent("KeyDown", new KeyEventArgs(Keys.Down | Keys.Control));

            // parents
            RSD_SubHandler = FamilyAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnParentsAdd", dlg);
            ModalFormHandler = MessageBox_YesHandler;
            ClickButton("btnParentsDelete", dlg);

            // father
            fNeedIndividualSex = GEDCOMSex.svMale;
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnFatherAdd", dlg);
            ModalFormHandler = MessageBox_YesHandler;
            ClickButton("btnFatherDelete", dlg);

            // mother
            fNeedIndividualSex = GEDCOMSex.svFemale;
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnMotherAdd", dlg);
            ModalFormHandler = MessageBox_YesHandler;
            ClickButton("btnMotherDelete", dlg);

            ClickButton("btnNameCopy", dlg);

            // events
            tabs.SelectTab(0);
            Assert.AreEqual(1, indiRecord.Events.Count);
            ModalFormHandler = EventEditDlg_Select_Handler;
            ClickToolStripButton("fEventsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(2, indiRecord.Events.Count);

            sheetTester = new GKSheetListTester("fEventsList", dlg);
            sheetTester.Properties.SelectItem(1);
            ModalFormHandler = EventEditDlg_Select_Handler;
            ClickToolStripButton("fEventsList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(2, indiRecord.Events.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(1);
            ClickToolStripButton("fEventsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(1, indiRecord.Events.Count);

            // spouses
            tabs.SelectTab(1);
            sheetTester = new GKSheetListTester("fSpousesList", dlg);
            Assert.AreEqual(0, indiRecord.SpouseToFamilyLinks.Count);
            ModalFormHandler = SpouseEdit_Handler;
            ClickToolStripButton("fSpousesList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, indiRecord.SpouseToFamilyLinks.Count);

            sheetTester.Properties.SelectItem(1);
            ModalFormHandler = SpouseEdit_Handler;
            ClickToolStripButton("fSpousesList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, indiRecord.SpouseToFamilyLinks.Count);

            sheetTester.Properties.SelectItem(1);
            ModalFormHandler = MessageBox_YesHandler;
            ClickToolStripButton("fSpousesList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, indiRecord.SpouseToFamilyLinks.Count);

            // names
            tabs.SelectTab(2);
            sheetTester = new GKSheetListTester("fNamesList", dlg);
            Assert.AreEqual(1, indiRecord.PersonalNames.Count);
            ModalFormHandler = NameEditAdd_Handler;
            ClickToolStripButton("fNamesList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(2, indiRecord.PersonalNames.Count);
            Assert.AreEqual("sample surname", indiRecord.PersonalNames[1].Surname);

            sheetTester.Properties.SelectItem(1);
            ModalFormHandler = NameEditEdit_Handler;
            ClickToolStripButton("fNamesList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(2, indiRecord.PersonalNames.Count);
            Assert.AreEqual("sample surname2", indiRecord.PersonalNames[1].Surname);

            sheetTester.Properties.SelectItem(1);
            ModalFormHandler = MessageBox_YesHandler;
            ClickToolStripButton("fNamesList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(1, indiRecord.PersonalNames.Count);

            // associations
            tabs.SelectTab(3);
            sheetTester = new GKSheetListTester("fAssociationsList", dlg);
            Assert.AreEqual(0, indiRecord.Associations.Count);
            ModalFormHandler = AssociationEditDlgTests.AcceptModalHandler;
            ClickToolStripButton("fAssociationsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, indiRecord.Associations.Count);
            Assert.AreEqual("sample relation", indiRecord.Associations[0].Relation);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fAssociationsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, indiRecord.Associations.Count);

            // groups
            tabs.SelectTab(4);
            sheetTester = new GKSheetListTester("fGroupsList", dlg);
            Assert.AreEqual(0, indiRecord.Groups.Count);
            RSD_SubHandler = GroupAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickToolStripButton("fGroupsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, indiRecord.Groups.Count);
            Assert.AreEqual("sample group", ((GEDCOMGroupRecord)indiRecord.Groups[0].Value).GroupName);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fGroupsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, indiRecord.Groups.Count);


            StructsDlg_Handler(indiRecord, dlg, tabs, new int[] { 5, 6, 7 });


            // userrefs
            tabs.SelectTab(8);
            sheetTester = new GKSheetListTester("fUserRefList", dlg);
            Assert.AreEqual(0, indiRecord.UserReferences.Count);
            ModalFormHandler = UserRefEditDlgTests.AcceptModalHandler;
            ClickToolStripButton("fUserRefList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, indiRecord.UserReferences.Count);
            Assert.AreEqual("sample reference", indiRecord.UserReferences[0].StringValue);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fUserRefList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, indiRecord.UserReferences.Count);
        }

        private void SourceEditDlg_Handler(SourceEditDlg dlg)
        {
            GEDCOMSourceRecord srcRecord = dlg.Model;
            GKSheetListTester sheetTester;
            var tabs = new TabControlTester("tabsData", dlg);

            // repositories
            tabs.SelectTab(2);
            Assert.AreEqual(0, srcRecord.RepositoryCitations.Count);
            RSD_SubHandler = TaskAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickToolStripButton("fRepositoriesList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, srcRecord.RepositoryCitations.Count);

            sheetTester = new GKSheetListTester("fRepositoriesList", dlg);
            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = MessageBox_YesHandler;
            ClickToolStripButton("fRepositoriesList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, srcRecord.RepositoryCitations.Count);
        }

        private void ResearchEditDlg_Handler(ResearchEditDlg dlg)
        {
            GEDCOMResearchRecord resRecord = dlg.Research;
            GKSheetListTester sheetTester;
            var tabs = new TabControlTester("tabsData", dlg);

            // tasks
            tabs.SelectTab(0);
            Assert.AreEqual(0, resRecord.Tasks.Count);
            RSD_SubHandler = TaskAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickToolStripButton("fTasksList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, resRecord.Tasks.Count);

            sheetTester = new GKSheetListTester("fTasksList", dlg);
            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = TaskAdd_Mini_Handler;
            ClickToolStripButton("fTasksList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, resRecord.Tasks.Count);

            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = MessageBox_YesHandler;
            ClickToolStripButton("fTasksList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, resRecord.Tasks.Count);

            // communications
            tabs.SelectTab(1);
            Assert.AreEqual(0, resRecord.Communications.Count);
            RSD_SubHandler = CommunicationAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickToolStripButton("fCommunicationsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, resRecord.Communications.Count);

            sheetTester = new GKSheetListTester("fCommunicationsList", dlg);
            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = CommunicationAdd_Mini_Handler;
            ClickToolStripButton("fCommunicationsList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, resRecord.Communications.Count);

            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = MessageBox_YesHandler;
            ClickToolStripButton("fCommunicationsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, resRecord.Communications.Count);

            // groups
            tabs.SelectTab(2);
            Assert.AreEqual(0, resRecord.Groups.Count);
            RSD_SubHandler = GroupAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickToolStripButton("fGroupsList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, resRecord.Groups.Count);
            Assert.AreEqual("sample group", ((GEDCOMGroupRecord)resRecord.Groups[0].Value).GroupName);

            sheetTester = new GKSheetListTester("fGroupsList", dlg);
            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = GroupAdd_Mini_Handler;
            ClickToolStripButton("fGroupsList_ToolBar_btnEdit", dlg);
            Assert.AreEqual(1, resRecord.Groups.Count);

            sheetTester.Properties.SelectItem(0);
            ModalFormHandler = MessageBox_YesHandler;
            ClickToolStripButton("fGroupsList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, resRecord.Groups.Count);
        }

        private void LocationEditDlg_Handler(LocationEditDlg dlg)
        {
            var tabs = new TabControlTester("tabsData", dlg);
            tabs.SelectTab(0);

            var txtName = new TextBoxTester("txtName");
            txtName.Enter("Moscow");

            var listGeoCoords = new ListViewTester("ListGeoCoords", dlg);
            listGeoCoords.FireEvent("Click", new EventArgs());

            ClickButton("btnSearch", dlg);
            ClickButton("btnSelect", dlg);
            ClickButton("btnSelectName", dlg);
            ClickButton("btnShowOnMap", dlg);
        }

        private void CommunicationEditDlg_Handler(CommunicationEditDlg dlg)
        {
            fNeedIndividualSex = GEDCOMSex.svMale;
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnPersonAdd", dlg);
        }

        private void TaskEditDlg_Handler(TaskEditDlg dlg)
        {
            var cmbGoalType = new ComboBoxTester("cmbGoalType", dlg);
            cmbGoalType.Select(3);
            ClickButton("btnGoalSelect", dlg);

            cmbGoalType.Select(2);
            RSD_SubHandler = SourceAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnGoalSelect", dlg);

            cmbGoalType.Select(1);
            RSD_SubHandler = FamilyAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnGoalSelect", dlg);

            cmbGoalType.Select(0);
            RSD_SubHandler = IndividualAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnGoalSelect", dlg);
        }

        private void GroupEditDlg_Handler(GroupEditDlg dlg)
        {
            GEDCOMGroupRecord groupRecord = dlg.Group;
            GKSheetListTester sheetTester = new GKSheetListTester("fMembersList", dlg);

            // members
            Assert.AreEqual(0, groupRecord.Members.Count);
            ModalFormHandler = RecordSelectDlg_Select_Handler;
            ClickToolStripButton("fMembersList_ToolBar_btnAdd", dlg);
            Assert.AreEqual(1, groupRecord.Members.Count);

            ModalFormHandler = MessageBox_YesHandler;
            sheetTester.Properties.SelectItem(0);
            ClickToolStripButton("fMembersList_ToolBar_btnDelete", dlg);
            Assert.AreEqual(0, groupRecord.Members.Count);
        }

        private void EventEditDlg_Select_Handler(string name, IntPtr ptr, Form form)
        {
            EventEditDlg eventDlg = (EventEditDlg) form;
            Assert.IsNotNull(eventDlg.Event);

            var cmbEventType = new ComboBoxTester("cmbEventType", form);
            cmbEventType.Select(1); // Birth(indi) / ?(fam)

            var txtEventPlace = new TextBoxTester("txtEventPlace", form);
            txtEventPlace.Enter("test place");

            var cmbEventDateType = new ComboBoxTester("cmbEventDateType", form);
            cmbEventDateType.Select(3); // Between

            var txtEventDate1 = new MaskedTextBoxTester("txtEventDate1", form);
            txtEventDate1.Enter("01.01.1900");

            var txtEventDate2 = new MaskedTextBoxTester("txtEventDate2", form);
            txtEventDate2.Enter("10.01.1900");

            var cmbDate1Calendar = new ComboBoxTester("cmbDate1Calendar", form);
            cmbDate1Calendar.Select(1); // Julian

            var cmbDate2Calendar = new ComboBoxTester("cmbDate2Calendar", form);
            cmbDate2Calendar.Select(1); // Julian

            var txtEventCause = new TextBoxTester("txtEventCause", form);
            txtEventCause.Enter("test cause");

            var txtEventOrg = new TextBoxTester("txtEventOrg", form);
            txtEventOrg.Enter("test agency");

            ModalFormHandler = AddressEditDlg_btnAccept_Handler;
            ClickButton("btnAddress", form);


            RSD_SubHandler = LocationAdd_Mini_Handler;
            ModalFormHandler = RecordSelectDlg_Create_Handler;
            ClickButton("btnPlaceAdd", form);

            ClickButton("btnPlaceDelete", form);


            ClickButton("btnAccept", form);
        }

        public void AddressEditDlg_btnAccept_Handler(string name, IntPtr ptr, Form form)
        {
            var addrDlg = (AddressEditDlg)form;
            ClickButton("btnAccept", form);
        }

        #region RecordSelectDlg handlers

        private GEDCOMSex fNeedIndividualSex;

        public void IndividualAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            var txtName = new TextBoxTester("txtName", form);
            txtName.Enter("test");

            var cmbSex = new ComboBoxTester("cmbSex", form);
            cmbSex.Select((int)fNeedIndividualSex);

            ClickButton("btnAccept", form);
        }

        public void FamilyAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            ClickButton("btnAccept", form);
        }

        public void NoteAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            var txtNote = new TextBoxTester("txtNote", form);
            txtNote.Enter("sample text");
            Assert.AreEqual("sample text", txtNote.Text);

            ClickButton("btnAccept", form);
        }

        public void SourceAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            var txtShortTitle = new TextBoxTester("txtShortTitle");
            txtShortTitle.Enter("sample text");

            ClickButton("btnAccept", form);
        }

        public void MediaAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            //var txtName = new TextBoxTester("txtName");
            //txtName.Enter("sample text");

            ClickButton("btnAccept", form);
        }

        public void LocationAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            var txtName = new TextBoxTester("txtName", form);
            txtName.Enter("sample location");

            ClickButton("btnAccept", form);
        }

        public void GroupAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            var edName = new TextBoxTester("edName", form);
            edName.Enter("sample group");

            ClickButton("btnAccept", form);
        }

        public void TaskAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            //var edName = new TextBoxTester("edName", form);
            //edName.Enter("sample group");

            ClickButton("btnAccept", form);
        }

        public void CommunicationAdd_Mini_Handler(string name, IntPtr ptr, Form form)
        {
            //var edName = new TextBoxTester("edName", form);
            //edName.Enter("sample group");

            ClickButton("btnAccept", form);
        }

        public void SpouseEdit_Handler(string name, IntPtr ptr, Form form)
        {
            ClickButton("btnAccept", form);
        }

        public void NameEditAdd_Handler(string name, IntPtr ptr, Form form)
        {
            var txtSurname = new TextBoxTester("txtSurname", form);
            txtSurname.Enter("sample surname");

            ClickButton("btnAccept", form);
        }

        public void NameEditEdit_Handler(string name, IntPtr ptr, Form form)
        {
            var txtSurname = new TextBoxTester("txtSurname", form);
            txtSurname.Enter("sample surname2");

            ClickButton("btnAccept", form);
        }

        private ModalFormHandler RSD_SubHandler;
        private int RSD_ItemIndex;

        public void RecordSelectDlg_Create_Handler(string name, IntPtr ptr, Form form)
        {
            ModalFormHandler = RSD_SubHandler;
            ClickButton("btnCreate", form);
        }

        public void RecordSelectDlg_Select_Handler(string name, IntPtr ptr, Form form)
        {
            var txtFastFilter = new TextBoxTester("txtFastFilter", form);
            txtFastFilter.Enter("*");

            var listRecords = new GKRecordsViewTester("fListRecords", form);
            listRecords.Properties.SelectItem(0);

            ClickButton("btnSelect", form);
        }

        public void RecordSelectDlg_SelectItem_Handler(string name, IntPtr ptr, Form form)
        {
            var listRecords = new GKRecordsViewTester("fListRecords", form);
            listRecords.Properties.SelectItem(RSD_ItemIndex);

            ClickButton("btnSelect", form);
        }

        #endregion

        #endregion

        private void CircleChartWin_Tests(Form frm, string stage)
        {
            Assert.IsInstanceOf(typeof(CircleChartWin), frm, stage);

            CircleChartWin ccWin = frm as CircleChartWin;
            Assert.AreEqual(fCurBase, ccWin.Base);
            ccWin.UpdateSettings();

            Assert.IsFalse(ccWin.AllowFilter());
            Assert.IsFalse(ccWin.AllowQuickSearch());
            Assert.IsTrue(ccWin.AllowPrint());

            // forced update
            ccWin.Refresh();

            Assert.IsFalse(ccWin.NavCanBackward());
            ccWin.NavPrev();
            Assert.IsFalse(ccWin.NavCanForward());
            ccWin.NavNext();

            var ctl = new ControlTester("fCircleChart", frm);
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Add));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Subtract));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Left));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Back));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Right));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.D0 | Keys.Control));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Up));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Down));

            ctl.FireEvent("DoubleClick", new EventArgs());
            ctl.Properties.Refresh();
            ctl.FireEvent("DoubleClick", new EventArgs());

            // empty methods
            Assert.IsNotNull(ccWin.FindAll(""));
            ccWin.QuickSearch();
            ccWin.SelectByRec(null);
            ccWin.SetFilter();

            ModalFormHandler = SaveSnapshotJPG_Handler;
            ClickToolStripButton("tbImageSave", ccWin);

            // FIXME exception!
            //ModalFormHandler = SaveSnapshotEMF_Handler;
            //ClickToolStripButton("tbImageSave", ccWin);

            ModalFormHandler = SaveSnapshotSVG_Handler;
            ClickToolStripButton("tbImageSave", ccWin);

            KeyDownForm(frm.Name, Keys.Escape);
            frm.Dispose();
        }

        private void TreeChartWin_Tests(Form frm, TreeChartKind kind, string stage, string checkXRef)
        {
            Assert.IsInstanceOf(typeof(TreeChartWin), frm, stage);

            TreeChartWin tcWin = frm as TreeChartWin;
            Assert.AreEqual(fCurBase, tcWin.Base);
            Assert.AreEqual(kind, tcWin.ChartKind);
            tcWin.UpdateSettings();

            Assert.IsTrue(tcWin.AllowFilter());
            Assert.IsTrue(tcWin.AllowQuickSearch());
            Assert.IsTrue(tcWin.AllowPrint());

            // forced update
            tcWin.Refresh();

            Assert.Throws(typeof(ArgumentNullException), () => { tcWin.SelectByRec(null); });

            GEDCOMIndividualRecord iRec = fCurBase.GetSelectedPerson();
            Assert.AreEqual(checkXRef, iRec.XRef);
            tcWin.SelectByRec(iRec);

            var formTester = new FormTester(tcWin.Name);
            Assert.IsNotNull(formTester);
            formTester[0].FireEvent("KeyDown", new KeyEventArgs(Keys.F5));
            formTester[0].FireEvent("KeyDown", new KeyEventArgs(Keys.F6));
            formTester[0].FireEvent("KeyDown", new KeyEventArgs(Keys.F7));

            //formTester[0].FireEvent("KeyDown", new KeyEventArgs(Keys.F | Keys.Control));

            tcWin.NavPrev();
            tcWin.NavNext();

            ModalFormHandler = TreeFilterDlg_btnAccept_Handler;
            tcWin.SetFilter();

            IList<ISearchResult> search = tcWin.FindAll("Maria");
            Assert.AreEqual(1, search.Count);

            ClickToolStripMenuItem("miGens9", tcWin);
            ClickToolStripMenuItem("miGens8", tcWin);
            ClickToolStripMenuItem("miGens7", tcWin);
            ClickToolStripMenuItem("miGens6", tcWin);
            ClickToolStripMenuItem("miGens5", tcWin);
            ClickToolStripMenuItem("miGens4", tcWin);
            ClickToolStripMenuItem("miGens3", tcWin);
            ClickToolStripMenuItem("miGens2", tcWin);
            ClickToolStripMenuItem("miGens1", tcWin);
            ClickToolStripMenuItem("miGensInf", tcWin);

            ClickToolStripMenuItem("miModeBoth", tcWin);
            ClickToolStripMenuItem("miModeAncestors", tcWin);
            ClickToolStripMenuItem("miModeDescendants", tcWin);

            ClickToolStripMenuItem("miCertaintyIndex", tcWin);
            ClickToolStripMenuItem("miTraceKinships", tcWin);
            ClickToolStripMenuItem("miTraceSelected", tcWin);

            var ctl = new ControlTester("fTreeBox", frm);
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Add));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Subtract));
            ctl.FireEvent("KeyDown", new KeyEventArgs(Keys.Back));
            ctl.FireEvent("DoubleClick", new EventArgs());

            Mouse.UseOn(ctl);

            int sx = 10 + 20;
            int sy = ctl.Properties.Height / 2;
            Mouse.Hover(sx, sy);
            Mouse.Press(MouseButtons.Left);
            Mouse.Hover(sx, sy + 20); // generations control
            Mouse.Release(MouseButtons.Left);

            sx = ctl.Properties.Width - 10 - 30;
            Mouse.Hover(sx, sy);
            Mouse.Press(MouseButtons.Left);
            Mouse.Hover(sx, sy + 20); // scale control
            Mouse.Release(MouseButtons.Left);

            //

            var tbox = ctl.Properties as TreeChartBox;
            Assert.IsNotNull(tbox);

            // handlers tests
            //ClickToolStripMenuItem("miEdit", tcWin);
            //ClickToolStripMenuItem("miFatherAdd", tcWin);
            //ClickToolStripMenuItem("miMotherAdd", tcWin);
            //ClickToolStripMenuItem("miSpouseAdd", tcWin);
            //ClickToolStripMenuItem("miSonAdd", tcWin);
            //ClickToolStripMenuItem("miDaughterAdd", tcWin);
            //ClickToolStripMenuItem("miFamilyAdd", tcWin);
            //ClickToolStripMenuItem("miDelete", tcWin);
            //ClickToolStripMenuItem("miRebuildKinships", tcWin);
            //ClickToolStripMenuItem("miFillColor", tcWin);
            //ClickToolStripMenuItem("miFillImage", tcWin);
            //ClickToolStripMenuItem("miRebuildTree", tcWin);

            ModalFormHandler = SaveSnapshotJPG_Handler;
            ClickToolStripButton("tbImageSave", tcWin);

            // FIXME exception!
            //ModalFormHandler = SaveSnapshotEMF_Handler;
            //ClickToolStripButton("tbImageSave", tcWin);

            ModalFormHandler = SaveSnapshotSVG_Handler;
            ClickToolStripButton("tbImageSave", tcWin);

            //ModalFormHandler = PrintPreviewDialog_Handler;
            //ClickToolStripButton("tbDocPrint", fMainWin);

            try {
                ModalFormHandler = PrintDialog_Handler;
                ClickToolStripButton("tbDocPreview", fMainWin);
            } catch (Exception) {
                // AppVeyor tests crashed, because "No printers are installed"
                // No Fail, or Ignore, or etc - not yet divide this test into smaller correct parts
            }

            KeyDownForm(frm.Name, Keys.Escape);
            frm.Dispose();
        }

        private void SaveSnapshotJPG_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test.jpg", hWnd);
        }

        private void SaveSnapshotEMF_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test.emf", hWnd);
        }

        private void SaveSnapshotSVG_Handler(string name, IntPtr hWnd, Form form)
        {
            PrepareFileSave("test.svg", hWnd);
        }

        private static void PrintDialog_Handler(string name, IntPtr ptr, Form form)
        {
            form.Close();
        }

        private static void PrintPreviewDialog_Handler(string name, IntPtr ptr, Form form)
        {
            form.Refresh();
            form.Close();
        }

        private static void TreeFilterDlg_btnAccept_Handler(string name, IntPtr ptr, Form form)
        {
            ClickButton("btnAccept", form);
        }

        private void StatsWin_Tests(Form frm, string stage)
        {
            Assert.IsInstanceOf(typeof(StatisticsWin), frm, stage);

            ModalFormHandler = SaveFile_Cancel_Handler;
            ClickToolStripButton("tbExcelExport", frm);

            KeyDownForm(frm.Name, Keys.Escape);
            frm.Dispose();
        }

        private void SlideshowWin_Tests(Form frm, string stage)
        {
            Assert.IsInstanceOf(typeof(SlideshowWin), frm, stage);

            SlideshowWin slidesWin = (SlideshowWin)frm;

            /*
            Assert.IsNotNull(slidesWin.FindAll(""));

            Assert.AreEqual(false, slidesWin.AllowFilter());
            slidesWin.SetFilter();

            slidesWin.SelectByRec(null);
            slidesWin.UpdateSettings();

            Assert.AreEqual(false, slidesWin.AllowQuickSearch());
            slidesWin.QuickSearch();

            Assert.AreEqual(false, slidesWin.NavCanBackward());
            slidesWin.NavPrev();

            Assert.AreEqual(false, slidesWin.NavCanForward());
            slidesWin.NavNext();
            */

            ClickToolStripButton("tbStart", frm); // start
            ClickToolStripButton("tbStart", frm); // stop

            ClickToolStripButton("tbNext", frm);
            ClickToolStripButton("tbPrev", frm);

            KeyDownForm(frm.Name, Keys.Escape);
            frm.Dispose();
        }

        private void MapsViewerWin_Tests(Form frm, string stage)
        {
            Assert.IsInstanceOf(typeof(MapsViewerWin), frm, stage);

            ClickRadioButton("radTotal", frm);

            ModalFormHandler = SaveFile_Cancel_Handler;
            ClickButton("btnSaveImage", frm);

            KeyDownForm(frm.Name, Keys.Escape);
            frm.Dispose();
        }
    }
}

#endif
