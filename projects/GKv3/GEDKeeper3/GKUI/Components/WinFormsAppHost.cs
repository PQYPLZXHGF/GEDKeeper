/*
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
using Eto.Forms;

using GKCommon;
using GKCommon.IoC;
using GKCore;
using GKCore.Charts;
using GKCore.Interfaces;
using GKCore.Options;
using GKCore.UIContracts;
using GKUI.Components;
using GKUI.Dialogs;

namespace GKUI.Components
{
    public sealed class WinFormsAppHost : AppHost
    {
        /*private readonly ApplicationContext fAppContext; // FIXME: GKv3 DevRestriction

        public ApplicationContext AppContext
        {
            get { return fAppContext; }
        }*/

        public WinFormsAppHost() : base()
        {
            //fAppContext = new ApplicationContext();
            //Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
        }

        public override void Init(string[] args, bool isMDI)
        {
            base.Init(args, isMDI);

            if (fIsMDI) {
                //fAppContext.MainForm = (Form)fMainWindow;
            }
        }

        public override IWindow GetActiveWindow()
        {
            if (fIsMDI) {
                //return (IWindow)((Form)fMainWindow).ActiveMdiChild;
                return null;
            } else {
                //Application.Instance.
                //Window.
                Form activeForm = null;//Form.ActiveForm;

                // only for tests!
                if (activeForm == null && fRunningForms.Count > 0) {
                    activeForm = (Form)fRunningForms[0];
                }

                return (activeForm is IWindow) ? (IWindow)activeForm : null;

                /*foreach (IWindow win in fRunningForms) {
                    if ((win as Form).Focused) {
                        return win;
                    }
                }
                return null;*/
            }
        }

        public override IntPtr GetTopWindowHandle()
        {
            IntPtr mainHandle = IntPtr.Zero;
            if (fIsMDI && fMainWindow != null) {
                //mainHandle = ((Form)fMainWindow).Handle;
            } else {
                // FIXME
            }
            return mainHandle;
        }

        public override void CloseWindow(IWindow window)
        {
            base.CloseWindow(window);

            if (!fIsMDI && fRunningForms.Count == 0) {
                //fAppContext.ExitThread();
                Application.Instance.Quit();
            }
        }

        public override void ShowWindow(IWindow window)
        {
            Form frm = window as Form;

            if (frm != null) {
                if (fIsMDI) {
                    //frm.MdiParent = (Form)fMainWindow;
                } else {
                    frm.ShowInTaskbar = true;
                }
                frm.Show();
            }
        }

        public override bool ShowModalX(ICommonDialog form, bool keepModeless)
        {
            IntPtr mainHandle = GetTopWindowHandle();

            if (keepModeless) {
                #if !__MonoCS__
                //NativeMethods.PostMessage(mainHandle, NativeMethods.WM_KEEPMODELESS, IntPtr.Zero, IntPtr.Zero);
                #endif
            }

            //UIHelper.CenterFormByParent((Form)form, mainHandle);

            return base.ShowModalX(form, keepModeless);
        }

        public override void EnableWindow(IWidgetForm form, bool value)
        {
            Form frm = form as Form;

            if (frm != null) {
                #if !__MonoCS__
                //NativeMethods.EnableWindow(frm.Handle, value);
                #endif
            }
        }

        protected override void UpdateLang()
        {
            if (fIsMDI && fMainWindow != null) {
                /*fMainWindow.SetLang();

                var mdiForm = fMainWindow as Form;
                foreach (Form child in mdiForm.MdiChildren) {
                    ILocalization localChild = (child as ILocalization);

                    if (localChild != null) {
                        localChild.SetLang();
                    }
                }*/
            } else {
                foreach (IWindow win in fRunningForms) {
                    win.SetLang();
                }
            }
        }

        public override void ApplyOptions()
        {
            base.ApplyOptions();

            if (fIsMDI && fMainWindow != null) {
                /*var mdiForm = fMainWindow as Form;
                foreach (Form child in mdiForm.MdiChildren) {
                    if (child is IWorkWindow) {
                        (child as IWorkWindow).UpdateView();
                    }
                }*/
            } else {
                foreach (IWindow win in fRunningForms) {
                    if (win is IWorkWindow) {
                        (win as IWorkWindow).UpdateView();
                    }
                }
            }
        }

        public override void BaseClosed(IBaseWindow baseWin)
        {
            base.BaseClosed(baseWin);

            SaveWinMRU(baseWin);
        }

        protected override void UpdateMRU()
        {
            if (fIsMDI && fMainWindow != null) {
                //((MainWin)fMainWindow).UpdateMRU();
            } else {
                foreach (IWindow win in fRunningForms) {
                    if (win is IBaseWindow) {
                        (win as BaseWinSDI).UpdateMRU();
                    }
                }
            }
        }

        public override void SaveWinMRU(IBaseWindow baseWin)
        {
            int idx = AppHost.Options.MRUFiles_IndexOf(baseWin.Context.FileName);
            if (idx >= 0) {
                var frm = baseWin as Form;
                MRUFile mf = AppHost.Options.MRUFiles[idx];
                mf.WinRect = UIHelper.GetFormRect(frm);
                mf.WinState = gkWindowStates[(int)frm.WindowState];
            }
        }

        private static Eto.Forms.WindowState[] efWindowStates = new Eto.Forms.WindowState[] {
            Eto.Forms.WindowState.Normal,
            Eto.Forms.WindowState.Minimized,
            Eto.Forms.WindowState.Maximized
        };

        private static GKCore.Options.WindowState[] gkWindowStates = new GKCore.Options.WindowState[] {
            GKCore.Options.WindowState.Normal,
            GKCore.Options.WindowState.Maximized,
            GKCore.Options.WindowState.Minimized
        };

        public override void RestoreWinMRU(IBaseWindow baseWin)
        {
            int idx = AppHost.Options.MRUFiles_IndexOf(baseWin.Context.FileName);
            if (idx >= 0) {
                var frm = baseWin as Form;
                MRUFile mf = AppHost.Options.MRUFiles[idx];
                UIHelper.RestoreFormRect(frm, mf.WinRect, efWindowStates[(int)mf.WinState]);
            }
        }

        public override void SaveLastBases()
        {
            AppHost.Options.ClearLastBases();

            if (fIsMDI && fMainWindow != null) {
                /*var mdiForm = fMainWindow as Form;
                for (int i = mdiForm.MdiChildren.Length - 1; i >= 0; i--) {
                    var baseWin = mdiForm.MdiChildren[i] as IBaseWindow;
                    if (baseWin != null) {
                        AppHost.Options.AddLastBase(baseWin.Context.FileName);
                    }
                }*/
            } else {
                foreach (IWindow win in fRunningForms) {
                    if (win is IBaseWindow) {
                        AppHost.Options.AddLastBase((win as IBaseWindow).Context.FileName);
                    }
                }
            }
        }

        #region KeyLayout functions

        public override int GetKeyLayout()
        {
            return CultureInfo.DefaultThreadCurrentUICulture.KeyboardLayoutId;

            /*#if __MonoCS__
            // There is a bug in Mono: does not work this CurrentInputLanguage
            return CultureInfo.CurrentUICulture.KeyboardLayoutId;
            #else
            InputLanguage currentLang = InputLanguage.CurrentInputLanguage;
            return currentLang.Culture.KeyboardLayoutId;
            #endif*/
        }

        public override void SetKeyLayout(int layout)
        {
            try {
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(layout);

                /*CultureInfo cultureInfo = new CultureInfo(layout);
                InputLanguage currentLang = InputLanguage.FromCulture(cultureInfo);
                InputLanguage.CurrentInputLanguage = currentLang;*/
            } catch (Exception ex) {
                Logger.LogWrite("Utilities.SetKeyLayout(): " + ex.Message);
            }
        }

        #endregion

        #region Executing environment

        public override Assembly GetExecutingAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        #endregion

        #region Bootstrapper

        /// <summary>
        /// This function implements initialization of IoC-container for WinForms presentation.
        /// </summary>
        public static void ConfigureBootstrap(bool mdi)
        {
            var appHost = new WinFormsAppHost();
            IContainer container = AppHost.Container;

            if (container == null)
                throw new ArgumentNullException("container");

            container.Reset();

            container.Register<IStdDialogs, WinFormsStdDialogs>(LifeCycle.Singleton);
            container.Register<IGraphicsProvider, WinFormsGfxProvider>(LifeCycle.Singleton);
            //container.Register<ILogger, LoggerStub>(LifeCycle.Singleton);
            container.Register<IProgressController, ProgressController>(LifeCycle.Singleton);

            // controls and other
            container.Register<ITreeChartBox, TreeChartBox>(LifeCycle.Transient);
            //container.Register<IWizardPages, WizardPages>(LifeCycle.Transient);

            // dialogs
            container.Register<IRecordSelectDialog, RecordSelectDlg>(LifeCycle.Transient);
            container.Register<IAddressEditDlg, AddressEditDlg>(LifeCycle.Transient);
            container.Register<IAssociationEditDlg, AssociationEditDlg>(LifeCycle.Transient);
            container.Register<ICommunicationEditDlg, CommunicationEditDlg>(LifeCycle.Transient);
            container.Register<IEventEditDlg, EventEditDlg>(LifeCycle.Transient);
            container.Register<IFamilyEditDlg, FamilyEditDlg>(LifeCycle.Transient);
            container.Register<IGroupEditDlg, GroupEditDlg>(LifeCycle.Transient);
            container.Register<ILanguageEditDlg, LanguageEditDlg>(LifeCycle.Transient);
            container.Register<ILanguageSelectDlg, LanguageSelectDlg>(LifeCycle.Transient);
            container.Register<ILocationEditDlg, LocationEditDlg>(LifeCycle.Transient);
            container.Register<IMediaEditDlg, MediaEditDlg>(LifeCycle.Transient);
            container.Register<INameEditDlg, NameEditDlg>(LifeCycle.Transient);
            container.Register<INoteEditDlg, NoteEditDlg>(LifeCycle.Transient);
            container.Register<INoteEditDlgEx, NoteEditDlgEx>(LifeCycle.Transient);
            container.Register<IPersonalNameEditDlg, PersonalNameEditDlg>(LifeCycle.Transient);
            container.Register<IPersonEditDlg, PersonEditDlg>(LifeCycle.Transient);
            container.Register<IRepositoryEditDlg, RepositoryEditDlg>(LifeCycle.Transient);
            container.Register<IResearchEditDlg, ResearchEditDlg>(LifeCycle.Transient);
            container.Register<ISexCheckDlg, SexCheckDlg>(LifeCycle.Transient);
            container.Register<ISourceCitEditDlg, SourceCitEditDlg>(LifeCycle.Transient);
            container.Register<ISourceEditDlg, SourceEditDlg>(LifeCycle.Transient);
            container.Register<ITaskEditDlg, TaskEditDlg>(LifeCycle.Transient);
            container.Register<IUserRefEditDlg, UserRefEditDlg>(LifeCycle.Transient);
            container.Register<IFilePropertiesDlg, FilePropertiesDlg>(LifeCycle.Transient);
            container.Register<IPortraitSelectDlg, PortraitSelectDlg>(LifeCycle.Transient);
            container.Register<IDayTipsDlg, DayTipsDlg>(LifeCycle.Transient);

            if (!mdi) {
                container.Register<IBaseWindow, BaseWinSDI>(LifeCycle.Transient);
            } else {
                //container.Register<IBaseWindow, BaseWin>(LifeCycle.Transient);
                //container.Register<IMainWindow, MainWin>(LifeCycle.Singleton);
            }
        }

        #endregion
    }
}
