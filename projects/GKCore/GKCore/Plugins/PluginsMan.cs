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
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using GKCommon;
using GKCore.Interfaces;
using GKCore.Types;

namespace GKCore.Plugins
{
    /// <summary>
    /// 
    /// </summary>
    public class PluginsMan
    {
        private List<IPlugin> fPlugins;

        public int Count
        {
            get { return fPlugins.Count; }
        }

        public IPlugin this[int index]
        {
            get { return fPlugins[index]; }
        }

        public PluginsMan()
        {
            fPlugins = new List<IPlugin>();
        }

        public void Load(IHost host, string path)
        {
            if (!Directory.Exists(path)) return;

            try {
                AppDomain.CurrentDomain.SetupInformation.PrivateBinPath = path;

                Type pluginType = typeof(IPlugin);
                string[] pluginFiles = Directory.GetFiles(path, "*.dll");

                foreach (string pfn in pluginFiles) {
                    try {
                        Assembly asm;

                        try {
                            AssemblyName assemblyName = AssemblyName.GetAssemblyName(pfn);
                            asm = Assembly.Load(assemblyName);
                        } catch {
                            asm = null;
                            // block exceptions for bad or non-dotnet assemblies
                        }

                        if (asm == null) continue;

                        Type[] types = asm.GetTypes();
                        foreach (Type type in types) {
                            if (type.IsInterface || type.IsAbstract) continue;
                            if (type.GetInterface(pluginType.FullName) == null) continue;

                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                            plugin.Startup(host);
                            fPlugins.Add(plugin);
                        }
                    } catch (Exception ex) {
                        Logger.LogWrite("PluginsMan.Load.1(" + pfn + "): " + ex.Message);
                    }
                }
            } catch (Exception ex) {
                Logger.LogWrite("PluginsMan.Load(" + path + "): " + ex.Message);
            }
        }

        public void Unload()
        {
            try {
                foreach (IPlugin plugin in fPlugins) {
                    plugin.Shutdown();
                }
            } catch (Exception ex) {
                Logger.LogWrite("PluginsMan.Unload(): " + ex.Message);
            }
        }

        public void OnLanguageChange()
        {
            try {
                foreach (IPlugin plugin in fPlugins) {
                    plugin.OnLanguageChange();
                }
            } catch (Exception ex) {
                Logger.LogWrite("PluginsMan.OnLanguageChange(): " + ex.Message);
            }
        }

        public void NotifyRecord(IBaseWindow baseWin, object record, RecordAction action)
        {
            if (baseWin == null || record == null) return;

            foreach (IPlugin plugin in fPlugins) {
                ISubscriber subscriber = (plugin as ISubscriber);
                if (subscriber == null) continue;

                try {
                    subscriber.NotifyRecord(baseWin, record, action);
                } catch (Exception ex) {
                    Logger.LogWrite("PluginsMan.NotifyRecord(): " + ex.Message);
                }
            }
        }
    }
}
