﻿using System;
using Eto.Drawing;
using Eto.Forms;
using GKUI.Components;

namespace GKUI.Dialogs
{
    partial class FilePropertiesDlg
    {
        private Button btnAccept;
        private Button btnCancel;
        private TabPage pageAuthor;
        private Label lblName;
        private Label lblAddress;
        private Label lblTelephone;
        private TextBox txtName;
        private TextBox txtTel;
        private TextArea txtAddress;
        private TabPage pageOther;
        private TabControl PageControl1;
        private GKListViewStub lvRecordStats;
        private Button btnLangEdit;
        private TextBox txtLanguage;
        private Label lblLanguage;

        private void InitializeComponent()
        {
            SuspendLayout();

            btnAccept = new Button();
            btnAccept.ImagePosition = ButtonImagePosition.Left;
            btnAccept.Size = new Size(130, 26);
            btnAccept.Text = "btnAccept";
            btnAccept.Click += btnAccept_Click;

            btnCancel = new Button();
            btnCancel.ImagePosition = ButtonImagePosition.Left;
            btnCancel.Size = new Size(130, 26);
            btnCancel.Text = "btnCancel";
            btnCancel.Click += CancelClickHandler;

            btnLangEdit = new Button();
            btnLangEdit.Size = new Size(26, 26);
            btnLangEdit.Click += btnLangEdit_Click;

            lblName = new Label();
            lblName.Text = "lblName";

            lblAddress = new Label();
            lblAddress.Text = "lblAddress";

            lblLanguage = new Label();
            lblLanguage.Text = "lblLanguage";

            lblTelephone = new Label();
            lblTelephone.Text = "lblTelephone";

            txtName = new TextBox();

            txtLanguage = new TextBox();
            txtLanguage.ReadOnly = true;

            txtTel = new TextBox();

            txtAddress = new TextArea();

            pageAuthor = new TabPage();
            pageAuthor.Text = "pageAuthor";
            pageAuthor.Content = new TableLayout {
                Padding = new Padding(10),
                Spacing = new Size(10, 10),
                Rows = {
                    new TableRow {
                        Cells = { lblName, txtName }
                    },
                    new TableRow {
                        ScaleHeight = true,
                        Cells = { lblAddress, txtAddress }
                    },
                    new TableRow {
                        Cells = { lblTelephone, txtTel }
                    },
                    new TableRow {
                        Cells = { lblLanguage, TableLayout.Horizontal(10, new TableCell(txtLanguage, true), new TableCell(btnLangEdit, false)) }
                    }
                }
            };

            lvRecordStats = new GKListViewStub();
            lvRecordStats.FullRowSelect = true;
            lvRecordStats.AllowMultipleSelection = false;
            lvRecordStats.AddColumn("Records", 300);
            lvRecordStats.AddColumn("Count", 100 /*, HorizontalAlignment.Right*/);

            pageOther = new TabPage();
            pageOther.Text = "pageOther";
            pageOther.Content = lvRecordStats;

            PageControl1 = new TabControl();
            PageControl1.Pages.Add(pageAuthor);
            PageControl1.Pages.Add(pageOther);

            Content = new TableLayout {
                Padding = new Padding(10),
                Spacing = new Size(10, 10),
                Rows = {
                    new TableRow {
                        ScaleHeight = true,
                        Cells = { PageControl1 }
                    },
                    UIHelper.MakeDialogFooter(null, btnAccept, btnCancel)
                }
            };

            DefaultButton = btnAccept;
            AbortButton = btnCancel;
            ClientSize = new Size(620, 400);
            Title = "FilePropertiesDlg";

            UIHelper.SetControlFont(this, "Tahoma", 8.25f);
            ResumeLayout();
        }
    }
}
