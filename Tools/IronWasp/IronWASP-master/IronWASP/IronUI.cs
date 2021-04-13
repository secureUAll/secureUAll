﻿//
// Copyright 2011-2013 Lavakumar Kuppan
//
// This file is part of IronWASP
//
// IronWASP is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 of the License.
//
// IronWASP is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with IronWASP.  If not, see http://www.gnu.org/licenses/.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using IronPython;
using IronPython.Hosting;
using IronPython.Modules;
using IronPython.Runtime;
using IronPython.Runtime.Exceptions;
using IronRuby;
using IronRuby.Hosting;
using IronRuby.Runtime;
using IronRuby.StandardLibrary;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace IronWASP
{
    public class IronUI
    {
        internal static Main UI;
        internal static AboutForm AF;
        internal static WaitForm WF;
        internal static AskUserWindow AUW;
        internal static ScanBranchWizard SBF;
        internal static StartScanWizard SSW;
        internal static LoadForm LF;
        internal static LoadInitialConfigurationForm LICF;
        internal static PluginEditor PE;
        internal static ReportGenerationWizard RGW;
        internal static ModUiDesigner UD;
        internal static DiffWindow DW;
        internal static EncodeDecodeWindow EDW;
        internal static ImportForm IF;
        internal static CloseForm CF;

        internal static int MainViewSelectedScanTraceId = 0;

        static DateTime TopMostTime;

        //internal static List<StartScanJobWizard> StartScanJobWizards = new List<StartScanJobWizard>();

        internal static bool BlockShell = false;

        internal static void SetUI(Main M)
        {
            UI = M;
        }

        delegate void BuildIronTree_d();
        internal static void BuildIronTree()
        {
            if (UI.IronTree.InvokeRequired)
            {
                BuildIronTree_d BIT_d = new BuildIronTree_d(BuildPluginTree);
                UI.Invoke(BIT_d, new object[] { });
            }
            else
            {
                UI.IronTree.BeginUpdate();
                TreeNode Node = UI.IronTree.Nodes.Add("Project", "Project");
                TreeNode VulnNode = new TreeNode("Vulnerabilities");
                VulnNode.Name = "Vulnerabilities";
                VulnNode.Nodes.Add("High", "High");
                VulnNode.Nodes[0].ForeColor = Color.Red;
                VulnNode.Nodes.Add("Medium", "Medium");
                VulnNode.Nodes[1].ForeColor = Color.Orange;
                VulnNode.Nodes.Add("Low", "Low");
                VulnNode.Nodes[2].ForeColor = Color.SteelBlue;
                Node.Nodes.Add(VulnNode);
                Node.Nodes.Add("TestLeads", "Test Leads");
                Node.Nodes.Add("Information", "Information");
                TreeNode ExceptionNode = new TreeNode("Exceptions");
                ExceptionNode.ForeColor = Color.OrangeRed;
                ExceptionNode.Name = "Exceptions";
                Node.Nodes.Add(ExceptionNode);
                Node.Nodes.Add("SiteMap", "SiteMap");
                UI.IronTree.EndUpdate();
                UI.IronTree.Nodes[0].ExpandAll();
            }
        }

        delegate void BuildPluginTree_d();
        internal static void BuildPluginTree()
        {
            if (UI.PluginAndModuleTree.InvokeRequired)
            {
                BuildPluginTree_d BPT_d = new BuildPluginTree_d(BuildPluginTree);
                UI.Invoke(BPT_d, new object[] { });
            }
            else
            {
                UI.PluginAndModuleTree.BeginUpdate();
                UI.PluginAndModuleTree.Nodes.Clear();
                TreeNode RootNode = UI.PluginAndModuleTree.Nodes.Add("PluginsAndModules", "Plugins & Modules");
                TreeNode PluginsNode = RootNode.Nodes.Add("Plugins", "Plugins");
                TreeNode ModulesNode = RootNode.Nodes.Add("Modules", "Modules");

                TreeNode Node = PluginsNode.Nodes.Add("ActivePlugins", "Active Plugins");
                Node.Checked = true;

                foreach (string Name in ActivePlugin.List())
                {
                    TreeNode SubNode = Node.Nodes.Add(Name, Name);
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Green;
                }
                
                Node = PluginsNode.Nodes.Add("PassivePlugins", "Passive Plugins");
                Node.Checked = true;
                foreach (string Name in PassivePlugin.List())
                {
                    TreeNode SubNode = Node.Nodes.Add(Name, Name);
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Green;
                }
                foreach (string Name in PassivePlugin.GetDeactivated())
                {
                    TreeNode SubNode = Node.Nodes.Add(Name, Name + " (Deactivated)");
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Gray;
                }

                

                Node = PluginsNode.Nodes.Add("FormatPlugins", "Format Plugins");
                Node.Checked = true;

                foreach (string Name in FormatPlugin.List())
                {
                    TreeNode SubNode = Node.Nodes.Add(Name, Name);
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Green;
                }

                Node = PluginsNode.Nodes.Add("SessionPlugins", "Session Plugins");
                Node.Checked = true;

                foreach (string Name in SessionPlugin.List())
                {
                    TreeNode SubNode = Node.Nodes.Add(Name, Name);
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Green;
                }

                Node = ModulesNode.Nodes.Add("LoadedModules", "Loaded Modules");
                Node.Checked = true;
                foreach (string Name in Module.List())
                {
                    TreeNode SubNode = Node.Nodes.Add(Name, Name);
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Green;
                }

                Node = ModulesNode.Nodes.Add("AllModules", "All Modules");
                Node.Checked = true;

                foreach (Module M in Module.ModuleListFromXml)
                {
                    TreeNode SubNode = Node.Nodes.Add(M.Name, M.Name);
                    SubNode.Checked = true;
                    SubNode.ForeColor = Color.Gray;
                }

                UI.PluginAndModuleTree.EndUpdate();
                UI.PluginAndModuleTree.ExpandAll();
            }
        }

        internal static void InitialiseAllScriptEditors()
        {
            //Multi-line shell
            UI.MultiLineShellInTE.ShowTabs = false;
            UI.MultiLineShellInTE.ShowEOLMarkers = false;
            UI.MultiLineShellInTE.ShowSpaces = false;
            UI.MultiLineShellInTE.ShowInvalidLines = false;
            UI.MultiLineShellInTE.TabIndent = 2;
            
            //ScriptedSend
            UI.CustomSendTE.ShowTabs = false;
            UI.CustomSendTE.ShowEOLMarkers = false;
            UI.CustomSendTE.ShowSpaces = false;
            UI.CustomSendTE.ShowInvalidLines = false;
            UI.CustomSendTE.ShowLineNumbers = false;
            UI.CustomSendTE.TabIndent = 2;
            UI.CustomSendTopRtb.Rtf = @"{\rtf1{\colortbl ;\red0\green0\blue255;\red25\green25\blue112;} \cf1 def \cf0 \cf2 \b1 ScriptedSend \b0 \cf0 (req):";
            UI.CustomSendBottomRtb.Rtf = @"{\rtf1{\colortbl ;\red0\green0\blue128;} \cf1     return \cf0 res";
            UI.CustomSendTE.ActiveTextAreaControl.TextArea.KeyUp += new System.Windows.Forms.KeyEventHandler(UI.CustomSendTE_KeyUp);

            //ScriptedInterception
            UI.ScriptedInterceptionScriptTopRTB.Rtf = @"{\rtf1{\colortbl ;\red0\green0\blue255;\red25\green25\blue112;} \cf1 def \cf0 \cf2 \b1 ShouldIntercept \b0 \cf0 (sess):";
            UI.ScriptedInterceptionScriptBottomRTB.Rtf = @"{\rtf1{\colortbl ;\red0\green0\blue128;} \cf1     return \cf0 False";

            //Plugin Viewer
            UI.PluginEditorInTE.ShowTabs = false;
            UI.PluginEditorInTE.ShowEOLMarkers = false;
            UI.PluginEditorInTE.ShowSpaces = false;
            UI.PluginEditorInTE.ShowInvalidLines = false;
            UI.PluginEditorInTE.TabIndent = 2;

            HighlightingManager.Manager.AddSyntaxModeFileProvider(new EditorSyntaxModesProvider());
            Directory.SetCurrentDirectory(Config.RootDir);

            UI.MultiLineShellInTE.SetHighlighting("Python");

            UI.CustomSendTE.SetHighlighting("Python");

            UI.PluginEditorInTE.SetHighlighting("Python");
        }

        delegate void UpdateAllFormatPluginRows_d();
        internal static void UpdateAllFormatPluginRows()
        {
            if (UI.InvokeRequired)
            {
                UpdateAllFormatPluginRows_d UAFPR_d = new UpdateAllFormatPluginRows_d(UpdateAllFormatPluginRows);
                UI.Invoke(UAFPR_d, new object[] { });
            }
            else
            {
                //UI.MTRequestFormatPluginsGrid.Rows.Clear();
                //UI.ProxyRequestFormatPluginsGrid.Rows.Clear();
                //UI.ProxyResponseFormatPluginsGrid.Rows.Clear();
                UI.ConfigureScanRequestFormatPluginsGrid.Rows.Clear();
                //UI.ConfigureScanRequestFormatPluginsGrid.Rows.Add(new object[] { false, "None" });
                foreach (string Name in FormatPlugin.List())
                {
                    //UI.MTRequestFormatPluginsGrid.Rows.Add(new object[] { Name });
                    //UI.ProxyRequestFormatPluginsGrid.Rows.Add(new object[] { Name });
                    //UI.ProxyResponseFormatPluginsGrid.Rows.Add(new object[] { Name });
                    UI.ConfigureScanRequestFormatPluginsGrid.Rows.Add(new object[] { false, Name });
                }
            }
        }

        delegate void UpdateAllActivePluginRows_d();
        internal static void UpdateAllActivePluginRows()
        {
            if (UI.ASScanPluginsGrid.InvokeRequired)
            {
                UpdateAllActivePluginRows_d UAAPR_d = new UpdateAllActivePluginRows_d(UpdateAllActivePluginRows);
                UI.Invoke(UAAPR_d, new object[] { });
            }
            else
            {
                UI.ASScanPluginsGrid.Rows.Clear();
                UI.ASScanPluginsGrid.Rows.Add(new object[] { false, "All" });
                foreach (string Name in ActivePlugin.List())
                {
                    UI.ASScanPluginsGrid.Rows.Add(new object[] { false, Name });
                }
            }
        }

        delegate void UpdateMTLogGridWithRequest_d(Request Req);
        internal static void UpdateMTLogGridWithRequest(Request Req)
        {
            if (UI.TestLogGrid.InvokeRequired)
            {
                UpdateMTLogGridWithRequest_d UMTLGWR_d = new UpdateMTLogGridWithRequest_d(UpdateMTLogGridWithRequest);
                UI.Invoke(UMTLGWR_d, new object[] { Req });
            }
            else
            {
                if (UI.TestLogGrid.Rows.Count > IronLog.MaxRowCount) return;
                try
                {
                    int GridID = UI.TestLogGrid.Rows.Add(new object[] { Req.ID, Req.Host, Req.Method, Req.URL, Req.File, Req.SSL, Req.GetParametersString() });
                    IronUpdater.MTGridMap.Add(Req.ID, GridID);
                    if (Req.ID > IronLog.TestMax) IronLog.TestMax = Req.ID;
                    if (Req.ID < IronLog.TestMin || IronLog.TestMin < 1) IronLog.TestMin = Req.ID;
                }
                catch(Exception Exp)
                {
                    IronException.Report("Error Updating MT Grid with Request", Exp.Message, Exp.StackTrace);
                }
                ShowCurrentLogStat();
            }
        }

        delegate void UpdateMTLogGridWithResponse_d(Response Res);
        internal static void UpdateMTLogGridWithResponse(Response Res)
        {
            if (UI.TestLogGrid.InvokeRequired)
            {
                UpdateMTLogGridWithResponse_d UMTLGWR_d = new UpdateMTLogGridWithResponse_d(UpdateMTLogGridWithResponse);
                UI.Invoke(UMTLGWR_d, new object[] { Res });
            }
            else
            {
                if (IronUpdater.MTGridMap.ContainsKey(Res.ID))
                {
                    try
                    {
                        bool MatchFound = true;
                        int GridID = IronUpdater.MTGridMap[Res.ID];
                        if (!((int)UI.TestLogGrid.Rows[GridID].Cells["MTLogGridColumnForID"].Value == Res.ID))
                        {
                            MatchFound = false;
                            foreach (DataGridViewRow Row in UI.TestLogGrid.Rows)
                            {
                                if ((int)Row.Cells["MTLogGridColumnForID"].Value == Res.ID)
                                {
                                    GridID = Row.Index;
                                    MatchFound = true;
                                    break;
                                }
                            }
                        }
                        if (MatchFound)
                        {
                            UI.TestLogGrid.Rows[GridID].Cells["MTLogGridColumnForCode"].Value = Res.Code;
                            UI.TestLogGrid.Rows[GridID].Cells["MTLogGridColumnForLength"].Value = Res.BodyArray.Length;
                            UI.TestLogGrid.Rows[GridID].Cells["MTLogGridColumnForMIME"].Value = Res.ContentType;
                            UI.TestLogGrid.Rows[GridID].Cells["MTLogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                        }
                        IronUpdater.MTGridMap.Remove(Res.ID);
                    }
                    catch (Exception Exp)
                    {
                        IronException.Report("Error updating MT Response in Grid", Exp.Message, Exp.StackTrace);
                    }
                }
            }
        }

        delegate void UpdateTestGroupLogGridWithRequest_d(Session IrSe);
        internal static void UpdateTestGroupLogGridWithRequest(Session IrSe)
        {
            if (UI.TestGroupLogGrid.InvokeRequired)
            {
                UpdateTestGroupLogGridWithRequest_d UTGLGWR_d = new UpdateTestGroupLogGridWithRequest_d(UpdateTestGroupLogGridWithRequest);
                UI.Invoke(UTGLGWR_d, new object[] { IrSe });
            }
            else
            {
                if (!ManualTesting.CurrentGroup.Equals(IrSe.Flags["Group"].ToString())) return;
                try
                {
                    UI.TestGroupLogGrid.Rows.Add(new object[] {false, IrSe.Request.ID, IrSe.Request.Host, IrSe.Request.Method, IrSe.Request.URL, IrSe.Request.SSL });
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error Updating Test Grid with Request", Exp.Message, Exp.StackTrace);
                }
            }
        }

        delegate void UpdateTestGroupLogGridWithResponse_d(Session IrSe);
        internal static void UpdateTestGroupLogGridWithResponse(Session IrSe)
        {
            if (UI.TestGroupLogGrid.InvokeRequired)
            {
                UpdateTestGroupLogGridWithResponse_d UTGLGWR_d = new UpdateTestGroupLogGridWithResponse_d(UpdateTestGroupLogGridWithResponse);
                UI.Invoke(UTGLGWR_d, new object[] { IrSe });
            }
            else
            {
                try
                {
                    if (!ManualTesting.CurrentGroup.Equals(IrSe.Flags["Group"].ToString())) return;

                    int GridID = 0;
                    foreach (DataGridViewRow Row in UI.TestGroupLogGrid.Rows)
                    {
                        if ((int)Row.Cells["TestGroupLogGridForID"].Value == IrSe.Request.ID)
                        {
                            GridID = Row.Index;
                            break;
                        }
                    }
                    UI.TestGroupLogGrid.Rows[GridID].Cells["TestGroupLogGridForCode"].Value = IrSe.Response.Code;
                    UI.TestGroupLogGrid.Rows[GridID].Cells["TestGroupLogGridForLength"].Value = IrSe.Response.BodyArray.Length;
                    UI.TestGroupLogGrid.Rows[GridID].Cells["TestGroupLogGridForMIME"].Value = IrSe.Response.ContentType;
                    UI.TestGroupLogGrid.Rows[GridID].Cells["TestGroupLogGridForSetCookie"].Value = (IrSe.Response.SetCookies.Count > 0);
                    if(IrSe.Flags.ContainsKey("Reflecton"))
                        UpdateManualTestingResponse(IrSe.Response, IrSe.Request, IrSe.Flags["Reflecton"].ToString());
                    else
                        UpdateManualTestingResponse(IrSe.Response, IrSe.Request, "");

                }
                catch (Exception Exp)
                {
                    IronException.Report("Error updating MT Response in Grid", Exp.Message, Exp.StackTrace);
                }
            }
        }


        delegate void UpdateTestGroupLogGrid_d(Dictionary<int, Session> GroupList);
        internal static void UpdateTestGroupLogGrid(Dictionary<int, Session> GroupList)
        {
            if (UI.TestGroupLogGrid.InvokeRequired)
            {
                UpdateTestGroupLogGrid_d UTGLG_d = new UpdateTestGroupLogGrid_d(UpdateTestGroupLogGrid);
                UI.Invoke(UTGLG_d, new object[] { GroupList });
            }
            else
            {
                UI.TestGroupLogGrid.Rows.Clear();
                foreach (int ID in GroupList.Keys)
                {
                    Session Irse = GroupList[ID];
                    if (Irse.Request == null) continue;
                    if(Irse.Response == null)
                        UI.TestGroupLogGrid.Rows.Add(new object[] {false, Irse.Request.ID, Irse.Request.Host, Irse.Request.Method, Irse.Request.Url, Irse.Request.SSL});
                    else
                        UI.TestGroupLogGrid.Rows.Add(new object[] {false, Irse.Request.ID, Irse.Request.Host, Irse.Request.Method, Irse.Request.Url, Irse.Request.SSL, Irse.Response.Code, Irse.Response.BodyLength, Irse.Response.ContentType, (Irse.Response.SetCookies.Count > 0) });
                }
            }
        }

        delegate void ClearTestGroupLogGrid_d();
        internal static void ClearTestGroupLogGrid()
        {
            if (UI.TestGroupLogGrid.InvokeRequired)
            {
                ClearTestGroupLogGrid_d CTGLG_d = new ClearTestGroupLogGrid_d(ClearTestGroupLogGrid);
                UI.Invoke(CTGLG_d, new object[] { });
            }
            else
            {
                UI.TestGroupLogGrid.Rows.Clear();
            }
        }

        delegate void SetNewTestRequest_d(Request Req, string Group, bool SwitchToMTSection);
        internal static void SetNewTestRequest(Request Req, string Group, bool SwitchToMTSection)
        {
            if (UI.TestGroupLogGrid.InvokeRequired)
            {
                SetNewTestRequest_d SNTR_d = new SetNewTestRequest_d(SetNewTestRequest);
                UI.Invoke(SNTR_d, new object[] { Req, Group, SwitchToMTSection });
            }
            else
            {
                ResetMTDisplayFields();
                //UI.TestResponseSplit.SplitterDistance = 30;
                //switch (Group)
                //{
                //    case ("Red"):
                //        UI.TestIDLbl.BackColor = Color.Red;
                //        break;
                //    case ("Blue"):
                //        UI.TestIDLbl.BackColor = Color.RoyalBlue;
                //        break;
                //    case ("Green"):
                //        UI.TestIDLbl.BackColor = Color.Green;
                //        break;
                //    case ("Gray"):
                //        UI.TestIDLbl.BackColor = Color.Gray;
                //        break;
                //    case ("Brown"):
                //        UI.TestIDLbl.BackColor = Color.Brown;
                //        break;
                //}
                UI.TestGroupsLV.Items.Add(Group, Group, 0);
                UI.TestGroupsLV.Items[Group].Selected = true;
                UI.TestIDLbl.Text = "ID: " + Req.ID.ToString();
                UI.MTCurrentGroupNameTB.Text = Group;
                FillMTFields(Req);
                try
                {
                    UI.TestGroupLogGrid.Rows.Clear();
                    UI.TestGroupLogGrid.Rows.Add(new object[] { false, Req.ID, Req.Host, Req.Method, Req.URL, Req.SSL });
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error Updating Test Grid with Request", Exp.Message, Exp.StackTrace);
                }
                if(SwitchToMTSection)
                    UI.main_tab.SelectTab("mt_manual");
                //UI.MTReqResTabs.SelectTab("MTRequestTab");
                UI.MTResponseSideTabs.SelectTab("MTResponseTab");
                if (!UI.TestGroupsTitleTB.Visible) UI.TestGroupsTitleTB.Visible = true;
                if (!UI.TestGroupsLV.Visible) UI.TestGroupsLV.Visible = true;
            }
        }

        delegate void UpdateShellLogGrid_d(List<Request> Requests, List<Response> Responses);
        internal static void UpdateShellLogGrid(List<Request> Requests, List<Response> Responses)
        {
            if (UI.ShellLogGrid.InvokeRequired)
            {
                UpdateShellLogGrid_d USLG_d = new UpdateShellLogGrid_d(UpdateShellLogGrid);
                UI.Invoke(USLG_d, new object[] { Requests, Responses});
            }
            else
            {
                foreach (Request Req in Requests)
                {
                    if (UI.ShellLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        int GridID = UI.ShellLogGrid.Rows.Add(new object[] { Req.ID, Req.Host, Req.Method, Req.URL, Req.StoredFile, Req.SSL, Req.StoredParameters });
                        IronUpdater.ShellGridMap.Add(Req.ID, GridID);
                        if (Req.ID > IronLog.ShellMax) IronLog.ShellMax = Req.ID;
                        if (Req.ID < IronLog.ShellMin || IronLog.ShellMin < 1) IronLog.ShellMin = Req.ID;
                    }
                    catch(Exception Exp)
                    {
                        IronException.Report("Error Updating Request in Shell LogGrid", Exp.Message, Exp.StackTrace);
                    }
                }
                foreach(Response Res in Responses)
                {
                    bool MatchFound = true;
                    if (IronUpdater.ShellGridMap.ContainsKey(Res.ID))
                    {
                        try
                        {
                            int GridID = IronUpdater.ShellGridMap[Res.ID];
                            if (!((int)UI.ShellLogGrid.Rows[GridID].Cells["ScriptingLogGridColumnForID"].Value == Res.ID))
                            {
                                MatchFound = false;
                                foreach (DataGridViewRow Row in UI.ShellLogGrid.Rows)
                                {
                                    if ((int)Row.Cells["ScriptingLogGridColumnForID"].Value == Res.ID)
                                    {
                                        GridID = Row.Index;
                                        MatchFound = true;
                                        break;
                                    }
                                }
                            }
                            if (MatchFound)
                            {
                                UI.ShellLogGrid.Rows[GridID].Cells["ScriptingLogGridColumnForCode"].Value = Res.Code;
                                UI.ShellLogGrid.Rows[GridID].Cells["ScriptingLogGridColumnForLength"].Value = Res.BodyArray.Length;
                                UI.ShellLogGrid.Rows[GridID].Cells["ScriptingLogGridColumnForMIME"].Value = Res.ContentType;
                                UI.ShellLogGrid.Rows[GridID].Cells["ScriptingLogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                            }
                            IronUpdater.ShellGridMap.Remove(Res.ID);
                        }
                        catch(Exception Exp)
                        {
                            IronException.Report("Error Updating Response in Shell LogGrid", Exp.Message, Exp.StackTrace);
                        }
                    }
                    else
                    {
                        //IronException.Report("Matching Request missing in Shell LogGrid", string.Format("Request ID - {0} is missing from the Shell LogGrid", new object[] { Res.ID.ToString() }), Res.ToString());
                    }
                }
                ShowCurrentLogStat();
            }
        }

        delegate void UpdateOtherSourceLogGrid_d(List<Request> Requests, List<Response> Responses, string CurrentSource, List<string> NewSources);
        internal static void UpdateOtherSourceLogGrid(List<Request> Requests, List<Response> Responses, string CurrentSource, List<string> NewSources)
        {
            if (UI.OtherLogGrid.InvokeRequired)
            {
                UpdateOtherSourceLogGrid_d UOSLG_d = new UpdateOtherSourceLogGrid_d(UpdateOtherSourceLogGrid);
                UI.Invoke(UOSLG_d, new object[] { Requests, Responses, CurrentSource, NewSources });
            }
            else
            {
                List<string> SourceRowsAvailable = new List<string>();
                int RowCount = UI.OtherLogSourceGrid.Rows.Count;
                foreach (DataGridViewRow Row in UI.OtherLogSourceGrid.Rows)
                {
                    SourceRowsAvailable.Add(Row.Cells[0].Value.ToString());                    
                }
                foreach (string NewSource in NewSources)
                {
                    if (!SourceRowsAvailable.Contains(NewSource))
                        UI.OtherLogSourceGrid.Rows.Add(new object[]{NewSource});
                }
                if (RowCount == 0 && UI.OtherLogSourceGrid.Rows.Count > 0) UI.OtherLogSourceGrid.ClearSelection();
                if (!SourceRowsAvailable.Contains(CurrentSource)) return;
                foreach (Request Req in Requests)
                {
                    if (UI.OtherLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        int GridID = UI.OtherLogGrid.Rows.Add(new object[] { Req.ID, Req.Host, Req.Method, Req.URL, Req.StoredFile, Req.SSL, Req.StoredParameters });
                        IronUpdater.OtherSourceGridMap.Add(Req.ID, GridID);
                        if (Req.ID > IronLog.OtherSourceMax) IronLog.OtherSourceMax = Req.ID;
                        if (Req.ID < IronLog.OtherSourceMin || IronLog.OtherSourceMin < 1) IronLog.OtherSourceMin = Req.ID;
                    }
                    catch (Exception Exp)
                    {
                        IronException.Report("Error Updating Request in Other Source LogGrid", Exp.Message, Exp.StackTrace);
                    }
                }
                foreach (Response Res in Responses)
                {
                    bool MatchFound = true;
                    if (IronUpdater.OtherSourceGridMap.ContainsKey(Res.ID))
                    {
                        try
                        {
                            int GridID = IronUpdater.OtherSourceGridMap[Res.ID];
                            if (!((int)UI.OtherLogGrid.Rows[GridID].Cells["LogGridColumnForID"].Value == Res.ID))
                            {
                                MatchFound = false;
                                foreach (DataGridViewRow Row in UI.OtherLogGrid.Rows)
                                {
                                    if ((int)Row.Cells["LogGridColumnForID"].Value == Res.ID)
                                    {
                                        GridID = Row.Index;
                                        MatchFound = true;
                                        break;
                                    }
                                }
                            }
                            if (MatchFound)
                            {
                                UI.OtherLogGrid.Rows[GridID].Cells["LogGridColumnForCode"].Value = Res.Code;
                                UI.OtherLogGrid.Rows[GridID].Cells["LogGridColumnForLength"].Value = Res.BodyArray.Length;
                                UI.OtherLogGrid.Rows[GridID].Cells["LogGridColumnForMIME"].Value = Res.ContentType;
                                UI.OtherLogGrid.Rows[GridID].Cells["LogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                            }
                            IronUpdater.OtherSourceGridMap.Remove(Res.ID);
                        }
                        catch (Exception Exp)
                        {
                            IronException.Report("Error Updating Response in Other Source LogGrid", Exp.Message, Exp.StackTrace);
                        }
                    }
                    else
                    {
                        //IronException.Report("Matching Request missing in Shell LogGrid", string.Format("Request ID - {0} is missing from the Shell LogGrid", new object[] { Res.ID.ToString() }), Res.ToString());
                    }
                }
                ShowCurrentLogStat();
            }
        }

        delegate void UpdateProbeLogGrid_d(List<Request> Requests, List<Response> Responses);
        internal static void UpdateProbeLogGrid(List<Request> Requests, List<Response> Responses)
        {
            if (UI.ProbeLogGrid.InvokeRequired)
            {
                UpdateProbeLogGrid_d UPLG_d = new UpdateProbeLogGrid_d(UpdateProbeLogGrid);
                UI.Invoke(UPLG_d, new object[] { Requests, Responses });
            }
            else
            {
                foreach (Request Req in Requests)
                {
                    if (UI.ProbeLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        int GridID = UI.ProbeLogGrid.Rows.Add(new object[] { Req.ID, Req.Host, Req.Method, Req.URL, Req.StoredFile, Req.SSL, Req.StoredParameters });
                        IronUpdater.ProbeGridMap.Add(Req.ID, GridID);
                        if (Req.ID > IronLog.ProbeMax) IronLog.ProbeMax = Req.ID;
                        if (Req.ID < IronLog.ProbeMin || IronLog.ProbeMin < 1) IronLog.ProbeMin = Req.ID;
                    }
                    catch (Exception Exp)
                    {
                        IronException.Report("Error Updating Request in Probe LogGrid", Exp.Message, Exp.StackTrace);
                    }
                }
                foreach (Response Res in Responses)
                {
                    if (IronUpdater.ProbeGridMap.ContainsKey(Res.ID))
                    {
                        bool MatchFound = true;
                        try
                        {
                            int GridID = IronUpdater.ProbeGridMap[Res.ID];
                            if (!((int)UI.ProbeLogGrid.Rows[GridID].Cells["ProbeLogGridColumnForID"].Value == Res.ID))
                            {
                                MatchFound = false;
                                foreach (DataGridViewRow Row in UI.ProbeLogGrid.Rows)
                                {
                                    if ((int)Row.Cells["ProbeLogGridColumnForID"].Value == Res.ID)
                                    {
                                        GridID = Row.Index;
                                        MatchFound = true;
                                        break;
                                    }
                                }
                            }
                            if (MatchFound)
                            {
                                UI.ProbeLogGrid.Rows[GridID].Cells["ProbeLogGridColumnForCode"].Value = Res.Code;
                                UI.ProbeLogGrid.Rows[GridID].Cells["ProbeLogGridColumnForLength"].Value = Res.BodyArray.Length;
                                UI.ProbeLogGrid.Rows[GridID].Cells["ProbeLogGridColumnForMIME"].Value = Res.ContentType;
                                UI.ProbeLogGrid.Rows[GridID].Cells["ProbeLogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                            }
                            IronUpdater.ProbeGridMap.Remove(Res.ID);
                        }
                        catch (Exception Exp)
                        {
                            IronException.Report("Error Updating Response in Probe LogGrid", Exp.Message, Exp.StackTrace);
                        }
                    }
                    else
                    {
                        //IronException.Report("Matching Request missing in Probe LogGrid", string.Format("Request ID - {0} is missing from the Probe LogGrid", new object[] { Res.ID.ToString() }), Res.ToString());
                    }
                }
                ShowCurrentLogStat();
            }
        }

        delegate void UpdateScanLogGrid_d(List<Request> Requests, List<Response> Responses);
        internal static void UpdateScanLogGrid(List<Request> Requests, List<Response> Responses)
        {
            if (UI.ScanLogGrid.InvokeRequired)
            {
                UpdateScanLogGrid_d USLG_d = new UpdateScanLogGrid_d(UpdateScanLogGrid);
                UI.Invoke(USLG_d, new object[] { Requests, Responses });
            }
            else
            {
                foreach (Request Req in Requests)
                {
                    if (UI.ScanLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        int GridID = UI.ScanLogGrid.Rows.Add(new object[] { Req.ID, Req.ScanID, Req.Host, Req.Method, Req.URL, Req.StoredFile, Req.SSL, Req.StoredParameters });
                        IronUpdater.ScanGridMap.Add(Req.ID, GridID);
                        if (Req.ID > IronLog.ScanMax) IronLog.ScanMax = Req.ID;
                        if (Req.ID < IronLog.ScanMin || IronLog.ScanMin < 1) IronLog.ScanMin = Req.ID;
                    }
                    catch(Exception Exp)
                    {
                        IronException.Report("Error Updating Request in Scan LogGrid", Exp.Message, Exp.StackTrace);
                    }
                }

                foreach (Response Res in Responses)
                {
                    if (IronUpdater.ScanGridMap.ContainsKey(Res.ID))
                    {
                        bool MatchFound = true;
                        try
                        {
                            int GridID = IronUpdater.ScanGridMap[Res.ID];
                            if (!((int)UI.ScanLogGrid.Rows[GridID].Cells["ScanLogGridColumnForID"].Value == Res.ID))
                            {
                                MatchFound = false;
                                foreach (DataGridViewRow Row in UI.ScanLogGrid.Rows)
                                {
                                    if ((int)Row.Cells["ScanLogGridColumnForID"].Value == Res.ID)
                                    {
                                        GridID = Row.Index;
                                        MatchFound = true;
                                        break;
                                    }
                                }
                            }
                            if (MatchFound)
                            {
                                UI.ScanLogGrid.Rows[GridID].Cells["ScanLogGridColumnForCode"].Value = Res.Code;
                                UI.ScanLogGrid.Rows[GridID].Cells["ScanLogGridColumnForLength"].Value = Res.BodyArray.Length;
                                UI.ScanLogGrid.Rows[GridID].Cells["ScanLogGridColumnForMIME"].Value = Res.ContentType;
                                UI.ScanLogGrid.Rows[GridID].Cells["ScanLogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                            } 
                            IronUpdater.ScanGridMap.Remove(Res.ID);
                        }
                        catch (Exception Exp)
                        {
                            IronException.Report("Error Updating Response in Scan LogGrid", Exp.Message, Exp.StackTrace);
                        }
                    }
                    else
                    {
                        //IronException.Report("Matching Request missing in Scan LogGrid", string.Format("Request ID - {0} is missing from the Scan LogGrid", new object[] { Res.ID.ToString() }), Res.ToString());
                    }
                }
                ShowCurrentLogStat();
            }
        }

        delegate void UpdateTraceGrid_d(List<IronTrace> Traces);
        internal static void UpdateTraceGrid(List<IronTrace> Traces)
        {
            if (UI.TraceGrid.InvokeRequired)
            {
                UpdateTraceGrid_d UTG_d = new UpdateTraceGrid_d(UpdateTraceGrid);
                UI.Invoke(UTG_d, new object[] { Traces });
            }
            else
            {
                foreach (IronTrace Trace in Traces)
                {
                    try
                    {
                        UI.TraceGrid.Rows.Add(new object[] { Trace.ID, Trace.Time, Trace.Date, Trace.ThreadID, Trace.Source, Trace.Message });
                    }
                    catch (Exception Exp)
                    {
                        IronException.Report("Error Updating Trace in TraceGrid", Exp.Message, Exp.StackTrace);
                    }
                }
            }
        }

        delegate void UpdateScanTraceGrid_d(List<IronTrace> Traces);
        internal static void UpdateScanTraceGrid(List<IronTrace> Traces)
        {
            if (UI.ScanTraceGrid.InvokeRequired)
            {
                UpdateScanTraceGrid_d USTG_d = new UpdateScanTraceGrid_d(UpdateScanTraceGrid);
                UI.Invoke(USTG_d, new object[] { Traces });
            }
            else
            {
                foreach (IronTrace Trace in Traces)
                {
                    if (UI.ScanTraceGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        UI.ScanTraceGrid.Rows.Add(new object[] { Trace.ID, Trace.ScanID, Trace.PluginName, Trace.Section, Trace.Parameter, Trace.Title });
                        if (Trace.ID > IronTrace.ScanTraceMax) IronTrace.ScanTraceMax = Trace.ID;
                        if (Trace.ID < IronTrace.ScanTraceMin || IronTrace.ScanTraceMin < 1) IronTrace.ScanTraceMin = Trace.ID;
                    }
                    catch (Exception Exp)
                    {
                        IronException.Report("Error Updating Trace in ScanTraceGrid", Exp.Message, Exp.StackTrace);
                    }
                }
                ShowCurrentScanTraceStat();
                IronUI.ShowScanTraceStatus("", false);
            }
        }



        delegate void SetScanTraceGrid_d(List<IronTrace> Traces);
        internal static void SetScanTraceGrid(List<IronTrace> Traces)
        {
            if (UI.ScanTraceGrid.InvokeRequired)
            {
                SetScanTraceGrid_d SSTG_d = new SetScanTraceGrid_d(SetScanTraceGrid);
                UI.Invoke(SSTG_d, new object[] { Traces });
            }
            else
            {
                UI.ScanTraceGrid.Rows.Clear();
                IronTrace.ScanTraceMin = 0;
                IronTrace.ScanTraceMax = 0;
                UpdateScanTraceGrid(Traces);
            }
        }



        delegate void ShowScanTraceStatus_d(string Message, bool Error);
        internal static void ShowScanTraceStatus(string Message, bool Error)
        {
            if (UI.ScanTraceStatusLbl.InvokeRequired)
            {
                ShowScanTraceStatus_d SSTS_d = new ShowScanTraceStatus_d(ShowScanTraceStatus);
                UI.Invoke(SSTS_d, new object[] { Message, Error });
            }
            else
            {
                if (Error)
                {
                    UI.ScanTraceStatusLbl.ForeColor = Color.Red;
                }
                else
                {
                    UI.ScanTraceStatusLbl.ForeColor = Color.Black;
                }
                UI.ScanTraceStatusLbl.Text = Message;
                UI.ScanTraceStatusLbl.Visible = true;
            }
        }

        delegate void ShowCurrentScanTraceStat_d();
        internal static void ShowCurrentScanTraceStat()
        {
            if (UI.ScanTraceStatLbl.InvokeRequired)
            {
                ShowCurrentScanTraceStat_d SCSTS_d = new ShowCurrentScanTraceStat_d(ShowCurrentScanTraceStat);
                UI.Invoke(SCSTS_d, new object[] { });
            }
            else
            {
                UI.ScanTraceStatLbl.Text = string.Format("Showing {0} - {1} of Scan Traces", IronTrace.ScanTraceMin, IronTrace.ScanTraceMax);
            }
        }

        delegate void UpdateSessionPluginTraceGrid_d(List<IronTrace> Traces);
        internal static void UpdateSessionPluginTraceGrid(List<IronTrace> Traces)
        {
            if (UI.SessionPluginTraceGrid.InvokeRequired)
            {
                UpdateSessionPluginTraceGrid_d USTG_d = new UpdateSessionPluginTraceGrid_d(UpdateSessionPluginTraceGrid);
                UI.Invoke(USTG_d, new object[] { Traces });
            }
            else
            {
                foreach (IronTrace Trace in Traces)
                {
                    try
                    {
                        UI.SessionPluginTraceGrid.Rows.Add(new object[] { Trace.ID, Trace.LogId, Trace.LogSource, Trace.SessionPluginName, Trace.Action, Trace.Message });
                    }
                    catch (Exception Exp)
                    {
                        IronException.Report("Error Updating Trace in SessionPluginTraceGrid", Exp.Message, Exp.StackTrace);
                    }
                }
            }
        }

        delegate void UpdateManualTestingResponse_d(Response Res, Request Req, string Reflection);
        internal static void UpdateManualTestingResponse(Response Res, Request Req, string Reflection)
        {
            if (UI.TestResponseView.InvokeRequired)
            {
                UpdateManualTestingResponse_d UMTR_d = new UpdateManualTestingResponse_d(UpdateManualTestingResponse);
                UI.Invoke(UMTR_d, new object[] { Res, Req, Reflection });
            }
            else
            {
                try
                {
                    if (ManualTesting.CurrentRequestID == Res.ID)
                    {
                        FillMTFields(Res, Req);
                        //FillTestReflection(Reflection);
                        UI.TestIDLbl.Text = "ID: " + Res.ID.ToString();
                        EndMTSend(true);
                    }
                }
                catch(Exception Exp)
                {
                    IronException.Report("Error updating MT Response", Exp.Message, Exp.StackTrace);
                }
            }
        }

        delegate void UpdateManualTestingRequest_d(Request Req);
        internal static void UpdateManualTestingRequest(Request Req)
        {
            if (UI.TestRequestView.InvokeRequired)
            {
                UpdateManualTestingRequest_d UMTR_d = new UpdateManualTestingRequest_d(UpdateManualTestingRequest);
                UI.Invoke(UMTR_d, new object[] { Req });
            }
            else
            {
                try
                {
                    FillMTFields(Req);
                    //UI.MTIsSSLCB.Checked = Req.SSL;
                    UI.TestIDLbl.Text = "ID: 0";
                    EndMTSend(false);
                }
                catch(Exception Exp)
                {
                    IronException.Report("Error updating MT Request", Exp.Message, Exp.StackTrace);
                }
            }
        }
        delegate void UpdateProxyLogGrid_d(List<Request> Requests, List<Response> Responses);
        internal static void UpdateProxyLogGrid(List<Request> Requests, List<Response> Responses)
        {
            if (UI.ProxyLogGrid.InvokeRequired)
            {
                UpdateProxyLogGrid_d UPLG_d = new UpdateProxyLogGrid_d(UpdateProxyLogGrid);
                UI.Invoke(UPLG_d, new object[] { Requests, Responses });
            }
            else
            {
                foreach (Request Req in Requests)
                {
                    if (UI.ProxyLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        int GridID = UI.ProxyLogGrid.Rows.Add(new object[] { Req.ID, Req.Host, Req.Method, Req.URL, Req.StoredFile, Req.SSL, Req.StoredParameters });
                        IronUpdater.ProxyGridMap.Add(Req.ID, GridID);
                        if (Req.ID > IronLog.ProxyMax) IronLog.ProxyMax = Req.ID;
                        if (Req.ID < IronLog.ProxyMin || IronLog.ProxyMin < 1) IronLog.ProxyMin = Req.ID;
                        UI.ProxyLogGrid.Rows[GridID].Visible = IronProxy.CanDisplayRowInLogDisplay(Req.Method, Req.Host, Req.StoredFile, 0, null, false);
                    }
                    catch(Exception exp)
                    {
                        IronException.Report("Error Updating Proxy LogGrid", exp.Message, exp.StackTrace);
                    }
                }

                foreach (Response Res in Responses)
                {
                    if (IronUpdater.ProxyGridMap.ContainsKey(Res.ID))
                    {
                        bool MatchFound = true;
                        try
                        {
                            int GridID = IronUpdater.ProxyGridMap[Res.ID];
                            if (!((int)UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForID"].Value == Res.ID))
                            {
                                MatchFound = false;
                                foreach (DataGridViewRow Row in UI.ProxyLogGrid.Rows)
                                {
                                    if ((int)Row.Cells["ProxyLogGridColumnForID"].Value == Res.ID)
                                    {
                                        GridID = Row.Index;
                                        MatchFound = true;
                                        break;
                                    }
                                }
                            }
                            if (MatchFound)
                            {
                                UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForCode"].Value = Res.Code;
                                if (Res.BodyArray != null)
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForLength"].Value = Res.BodyArray.Length;
                                }
                                else
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForLength"].Value = 0;
                                }
                                UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForMIME"].Value = Res.ContentType;
                                UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                                if (UI.ProxyLogGrid.Rows[GridID].Visible)
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Visible = IronProxy.CanDisplayRowInLogDisplay(null, null, null, Res.Code, Res.ContentType, Res.BodyLength == 0);
                                }
                            }
                            IronUpdater.ProxyGridMap.Remove(Res.ID);
                        }
                        catch(Exception exp)
                        {
                            IronException.Report("Error Updating Proxy LogGrid", exp.Message, exp.StackTrace);
                        }
                    }
                    else
                    {
                        //IronException.Report("Matching Request missing in Proxy LogGrid", string.Format("Request ID - {0} is missing from the Proxy LogGrid", new object[]{ Res.ID.ToString()}), Res.ToString());
                    }
                }
                ShowCurrentLogStat();
            }
        }

        delegate void UpdateProxyLogGridWithArrs_d(List<Request[]> RequestArrs, List<Response[]> ResponseArrs);
        internal static void UpdateProxyLogGridWithArrs(List<Request[]> RequestArrs, List<Response[]> ResponseArrs)
        {
            if (UI.ProxyLogGrid.InvokeRequired)
            {
                UpdateProxyLogGridWithArrs_d UPLGWA_d = new UpdateProxyLogGridWithArrs_d(UpdateProxyLogGridWithArrs);
                UI.Invoke(UPLGWA_d, new object[] { RequestArrs, ResponseArrs });
            }
            else
            {
                foreach (Request[] ReqArr in RequestArrs)
                {
                    if (UI.ProxyLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        int GridID = UI.ProxyLogGrid.Rows.Add(new object[] { ReqArr[1].ID, ReqArr[1].Host, ReqArr[1].Method, ReqArr[1].URL, ReqArr[1].StoredFile, ReqArr[1].SSL, ReqArr[1].StoredParameters });
                        UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForEdited"].Value = ReqArr[0] != null;
                        IronUpdater.ProxyGridMap.Add(ReqArr[1].ID, GridID);
                        if (ReqArr[1].ID > IronLog.ProxyMax) IronLog.ProxyMax = ReqArr[1].ID;
                        if (ReqArr[1].ID < IronLog.ProxyMin || IronLog.ProxyMin < 1) IronLog.ProxyMin = ReqArr[1].ID;
                        UI.ProxyLogGrid.Rows[GridID].Visible = IronProxy.CanDisplayRowInLogDisplay(ReqArr[1].Method, ReqArr[1].Host, ReqArr[1].StoredFile, 0, null, false);
                    }
                    catch (Exception exp)
                    {
                        IronException.Report("Error Updating Proxy LogGrid", exp.Message, exp.StackTrace);
                    }
                }

                foreach (Response[] ResArr in ResponseArrs)
                {
                    if (IronUpdater.ProxyGridMap.ContainsKey(ResArr[1].ID))
                    {
                        bool MatchFound = true;
                        try
                        {
                            int GridID = IronUpdater.ProxyGridMap[ResArr[1].ID];
                            if (!((int)UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForID"].Value == ResArr[1].ID))
                            {
                                MatchFound = false;
                                foreach (DataGridViewRow Row in UI.ProxyLogGrid.Rows)
                                {
                                    if ((int)Row.Cells["ProxyLogGridColumnForID"].Value == ResArr[1].ID)
                                    {
                                        GridID = Row.Index;
                                        MatchFound = true;
                                        break;
                                    }
                                }
                            }
                            if (MatchFound)
                            {
                                UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForCode"].Value = ResArr[1].Code;
                                if (ResArr[1].BodyArray != null)
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForLength"].Value = ResArr[1].BodyArray.Length;
                                }
                                else
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForLength"].Value = 0;
                                }
                                UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForMIME"].Value = ResArr[1].ContentType;
                                UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForSetCookie"].Value = (ResArr[1].SetCookies.Count > 0);
                                if (ResArr[0] != null)
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForEdited"].Value = true;
                                }
                                if (UI.ProxyLogGrid.Rows[GridID].Visible)
                                {
                                    UI.ProxyLogGrid.Rows[GridID].Visible = IronProxy.CanDisplayRowInLogDisplay(null, null, null, ResArr[1].Code, ResArr[1].ContentType, ResArr[1].BodyLength == 0);
                                }
                            }
                            IronUpdater.ProxyGridMap.Remove(ResArr[1].ID);
                        }
                        catch (Exception exp)
                        {
                            IronException.Report("Error Updating Proxy LogGrid", exp.Message, exp.StackTrace);
                        }
                    }
                    else
                    {
                        //IronException.Report("Matching Request missing in Proxy LogGrid", string.Format("Request ID - {0} is missing from the Proxy LogGrid", new object[]{ Res.ID.ToString()}), Res.ToString());
                    }
                }
                ShowCurrentLogStat();
            }
        }


        delegate void UpdateEditedProxyLogRequestEntry_d(Request Req);
        internal static void UpdateEditedProxyLogRequestEntry(Request Req)
        {
            if (UI.ProxyLogGrid.InvokeRequired)
            {
                UpdateEditedProxyLogRequestEntry_d UEPLRE_d = new UpdateEditedProxyLogRequestEntry_d(UpdateEditedProxyLogRequestEntry);
                UI.Invoke(UEPLRE_d, new object[] { Req });
            }
            else
            {
                try
                {
                    int GridID = 0;
                    if (IronUpdater.ProxyGridMap.ContainsKey(Req.ID))
                    {
                        GridID = IronUpdater.ProxyGridMap[Req.ID];
                    }
                    if (!((int)UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForID"].Value == Req.ID))
                    {
                        foreach (DataGridViewRow Row in UI.ProxyLogGrid.Rows)
                        {
                            if ((int)Row.Cells["ProxyLogGridColumnForID"].Value == Req.ID)
                            {
                                GridID = Row.Index;
                                break;
                            }
                        }
                    }

                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForMethod"].Value = Req.Method;
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForURL"].Value = Req.URL;
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForFile"].Value = Req.File;
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForParameters"].Value = Req.GetParametersString();
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForEdited"].Value = true;
                    UI.ProxyLogGrid.Rows[GridID].Visible = IronProxy.CanDisplayRowInLogDisplay(Req.Method, Req.Host, Req.StoredFile, 0, null, false);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error updating Edited Proxy Request in UI", Exp.Message, Exp.StackTrace);
                }
            }
        }

        delegate void UpdateEditedProxyLogResponseEntry_d(Response Res);
        internal static void UpdateEditedProxyLogResponseEntry(Response Res)
        {
            if (UI.ProxyLogGrid.InvokeRequired)
            {
                UpdateEditedProxyLogResponseEntry_d UEPLRE_d = new UpdateEditedProxyLogResponseEntry_d(UpdateEditedProxyLogResponseEntry);
                UI.Invoke(UEPLRE_d, new object[] { Res });
            }
            else
            {
                try
                {
                    int GridID = 0;
                    if (IronUpdater.ProxyGridMap.ContainsKey(Res.ID))
                    {
                        GridID = IronUpdater.ProxyGridMap[Res.ID];
                    }
                    if (!((int)UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForID"].Value == Res.ID))
                    {
                        foreach (DataGridViewRow Row in UI.ProxyLogGrid.Rows)
                        {
                            if ((int)Row.Cells["ProxyLogGridColumnForID"].Value == Res.ID)
                            {
                                GridID = Row.Index;
                                break;
                            }
                        }
                    }

                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForCode"].Value = Res.Code;
                    if (Res.BodyArray != null)
                    {
                        UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForLength"].Value = Res.BodyArray.Length;
                    }
                    else
                    {
                        UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForLength"].Value = 0;
                    }
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForMIME"].Value = Res.ContentType;
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForSetCookie"].Value = (Res.SetCookies.Count > 0);
                    UI.ProxyLogGrid.Rows[GridID].Cells["ProxyLogGridColumnForEdited"].Value = true;
                    if (UI.ProxyLogGrid.Rows[GridID].Visible)
                    {
                        UI.ProxyLogGrid.Rows[GridID].Visible = IronProxy.CanDisplayRowInLogDisplay(null, null, null, Res.Code, Res.ContentType, Res.BodyLength == 0);
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error updating Edited Proxy Response in UI", Exp.Message, Exp.StackTrace);
                }
            }
        }

        delegate void UpdateScanQueueStatuses_d(List<int> ScanIDs, string Status);
        internal static void UpdateScanQueueStatuses(List<int> ScanIDs, string Status)
        {
            if (UI.CanShutdown) return;
            if (UI.ASQueueGrid.InvokeRequired)
            {
                UpdateScanQueueStatuses_d USQS_d = new UpdateScanQueueStatuses_d(UpdateScanQueueStatuses);
                UI.Invoke(USQS_d, new object[] { ScanIDs, Status });
            }
            else
            {
                foreach (int ScanID in ScanIDs)
                {
                    UpdateScanQueueStatus(ScanID, Status);
                }
            }
        }

        delegate void UpdateScanQueueStatus_d(int ScanID, string Status);
        internal static void UpdateScanQueueStatus(int ScanID, string Status)
        {
            if (UI.CanShutdown) return;
            if (UI.ASQueueGrid.InvokeRequired)
            {
                UpdateScanQueueStatus_d USQS_d = new UpdateScanQueueStatus_d(UpdateScanQueueStatus);
                UI.Invoke(USQS_d, new object[] { ScanID, Status });
            }
            else
            {
                if (UI.ASQueueGrid.Rows.Count >= ScanID)
                {
                    try
                    {
                        if ((int)UI.ASQueueGrid.Rows[ScanID - 1].Cells[0].Value == ScanID)
                        {
                            SetScanRowStatus(UI.ASQueueGrid.Rows[ScanID -1], Status);
                            return;
                        }
                    }
                    catch { }
                }
                
                foreach (DataGridViewRow Row in UI.ASQueueGrid.Rows)
                {
                    int RowScanID = 0;
                    try
                    {
                        RowScanID = (int)Row.Cells[0].Value;
                    }
                    catch
                    {
                        continue;
                    }
                    if (RowScanID == ScanID)
                    {
                        SetScanRowStatus(Row, Status);
                        return;
                    }
                }
            }
        }

        static void SetScanRowStatus(DataGridViewRow Row, string Status)
        {
            Row.Cells[1].Value = Status;
            switch (Status)
            {
                case "Running":
                    Row.DefaultCellStyle.BackColor = Color.Green;
                    break;
                case "Aborted":
                    Row.DefaultCellStyle.BackColor = Color.Red;
                    break;
                case "Completed":
                    Row.DefaultCellStyle.BackColor = Color.Gray;
                    break;
                case "Incomplete":
                case "Stopped":
                    Row.DefaultCellStyle.BackColor = Color.IndianRed;
                    break;
                default:
                    Row.DefaultCellStyle.BackColor = Color.White;
                    break;
            }
        }

        delegate void CreateScan_d(int ScanID, string Status, string Method, string Url);
        internal static void CreateScan(int ScanID, string Status, string Method, string Url)
        {
            if (UI.ASQueueGrid.InvokeRequired)
            {
                CreateScan_d CS_d = new CreateScan_d(CreateScan);
                UI.Invoke(CS_d, new object[] { ScanID, Status, Method, Url });
            }
            else
            {
                int GridID = UI.ASQueueGrid.Rows.Add(new object[] { ScanID, Status, Method, Url });
                DataGridViewRow Row = null;
                try
                {
                    Row = UI.ASQueueGrid.Rows[GridID];
                }
                catch
                {
                    return;
                }
                switch (Status)
                {
                    case "Running":
                        Row.DefaultCellStyle.BackColor = Color.Green;
                        break;
                    case "Aborted":
                        Row.DefaultCellStyle.BackColor = Color.Red;
                        break;
                    case "Completed":
                        Row.DefaultCellStyle.BackColor = Color.Gray;
                        break;
                    case "Incomplete":
                    case "Stopped":
                        Row.DefaultCellStyle.BackColor = Color.IndianRed;
                        break;
                    default:
                        Row.DefaultCellStyle.BackColor = Color.White;
                        break;
                }
                if (!UI.main_tab.SelectedTab.Name.Equals("mt_auto"))
                {
                    try
                    {
                        UI.ASQueueGrid.FirstDisplayedScrollingRowIndex = GridID;
                    }
                    catch { }
                }
            }
        }

        delegate void ShowScanJobsQueue_d();
        internal static void ShowScanJobsQueue()
        {
            if (UI.ASQueueGrid.InvokeRequired)
            {
                ShowScanJobsQueue_d SSJQ_d = new ShowScanJobsQueue_d(ShowScanJobsQueue);
                UI.Invoke(SSJQ_d, new object[] { });
            }
            else
            {
                if(!UI.main_tab.SelectedTab.Name.Equals("mt_auto")) UI.main_tab.SelectTab("mt_auto");
                if (!UI.ASMainTabs.SelectedTab.Name.Equals("ASConfigureTab")) UI.ASMainTabs.SelectTab("ASConfigureTab");
            }
        }

        //delegate void FillAndShowJavaScriptTester_d(string Input);
        //internal static void FillAndShowJavaScriptTester(string Input)
        //{
        //    if (UI.JSTaintTraceInRTB.InvokeRequired)
        //    {
        //        FillAndShowJavaScriptTester_d FASJT_d = new FillAndShowJavaScriptTester_d(FillAndShowJavaScriptTester);
        //        UI.Invoke(FASJT_d, new object[] { Input});
        //    }
        //    else
        //    {
        //        UI.JSTaintTraceInRTB.Text = Input;
        //        if (!UI.main_tab.SelectedTab.Name.Equals("mt_js")) UI.main_tab.SelectTab("mt_js");
        //        if (!UI.JSTaintTabs.SelectedTab.Name.Equals("JSTaintInputTab")) UI.JSTaintTabs.SelectTab("JSTaintInputTab");
        //    }
        //}

        public delegate void UpdatePluginResultTree_d(List<Finding> PRs);
        public static void UpdatePluginResultTree(List<Finding> PRs)
        {
            if (UI.IronTree.InvokeRequired)
            {
                UpdatePluginResultTree_d UPRT_d = new UpdatePluginResultTree_d(UpdatePluginResultTree);
                UI.Invoke(UPRT_d, new object[] { PRs });
            }
            else
            {
                if (UI.IronTree == null) return;
                
                UI.IronTree.BeginUpdate();
                UI.IronTree.Enabled = false;
                foreach (Finding PR in PRs)
                {
                    if (PR.Type == FindingType.Vulnerability)
                    {
                        string Title = "";
                        if (PR.Confidence == FindingConfidence.High)
                        {
                            //Title = "+++ " + PR.Title;
                            Title = "+++ " + PR.Id.ToString();
                        }
                        else if (PR.Confidence == FindingConfidence.Medium)
                        {
                            //Title = "++- " + PR.Title;
                            Title = "++- " + PR.Id.ToString();
                        }
                        else if (PR.Confidence == FindingConfidence.Low)
                        {
                            //Title = "+-- " + PR.Title;
                            Title = "+-- " + PR.Id.ToString();
                        }
                        if (PR.Severity == FindingSeverity.High)
                        {
                            if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes.ContainsKey(PR.AffectedHost))
                            {
                                UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes.Add(PR.AffectedHost, PR.AffectedHost);
                            }
                            //if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Plugin))
                            if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Title))
                            {
                                //UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes[PR.AffectedHost].Nodes.Add(PR.Plugin, PR.Plugin);
                                UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes[PR.AffectedHost].Nodes.Add(PR.Title, PR.Title);
                            }
                            //UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes[PR.AffectedHost].Nodes[PR.Plugin].Nodes.Add(PR.Id.ToString(), Title);
                            UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes[PR.AffectedHost].Nodes[PR.Title].Nodes.Add(PR.Id.ToString(), Title);
                        }
                        else if (PR.Severity == FindingSeverity.Medium)
                        {
                            if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes.ContainsKey(PR.AffectedHost))
                            {
                                UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes.Add(PR.AffectedHost, PR.AffectedHost);
                            }
                            //if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Plugin))
                            if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Title))
                            {
                                //UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.Add(PR.Plugin, PR.Plugin);
                                UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.Add(PR.Title, PR.Title);
                            }
                            //UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes[PR.Plugin].Nodes.Add(PR.Id.ToString(), Title);
                            UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes[PR.Title].Nodes.Add(PR.Id.ToString(), Title);
                        }
                        else if (PR.Severity == FindingSeverity.Low)
                        {
                            if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes.ContainsKey(PR.AffectedHost))
                            {
                                UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes.Add(PR.AffectedHost, PR.AffectedHost);
                            }
                            //if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Plugin))
                            if (!UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Title))
                            {
                                //UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.Add(PR.Plugin, PR.Plugin);
                                UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.Add(PR.Title, PR.Title);
                            }
                            //UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes[PR.Plugin].Nodes.Add(PR.Id.ToString(), Title);
                            UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes[PR.Title].Nodes.Add(PR.Id.ToString(), Title);
                        }
                    }
                    else if (PR.Type == FindingType.TestLead)
                    {
                        if (!UI.IronTree.Nodes[0].Nodes[1].Nodes.ContainsKey(PR.AffectedHost))
                        {
                            UI.IronTree.Nodes[0].Nodes[1].Nodes.Add(PR.AffectedHost, PR.AffectedHost);
                        }
                        //if (!UI.IronTree.Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Plugin))
                        if (!UI.IronTree.Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Title))
                        {
                            //UI.IronTree.Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.Add(PR.Plugin, PR.Plugin);
                            UI.IronTree.Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes.Add(PR.Title, PR.Title);
                        }
                        //UI.IronTree.Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes[PR.Plugin].Nodes.Add(PR.Id.ToString(), PR.Title);
                        UI.IronTree.Nodes[0].Nodes[1].Nodes[PR.AffectedHost].Nodes[PR.Title].Nodes.Add(PR.Id.ToString(), PR.Id.ToString());
                    }
                    else if (PR.Type == FindingType.Information)
                    {
                        if (!UI.IronTree.Nodes[0].Nodes[2].Nodes.ContainsKey(PR.AffectedHost))
                        {
                            UI.IronTree.Nodes[0].Nodes[2].Nodes.Add(PR.AffectedHost, PR.AffectedHost);
                        }
                        //if (!UI.IronTree.Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Plugin))
                        if (!UI.IronTree.Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.ContainsKey(PR.Title))
                        {
                            //UI.IronTree.Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.Add(PR.Plugin, PR.Plugin);
                            UI.IronTree.Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes.Add(PR.Title, PR.Title);
                        }
                        //UI.IronTree.Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes[PR.Plugin].Nodes.Add(PR.Id.ToString(), PR.Title);
                        UI.IronTree.Nodes[0].Nodes[2].Nodes[PR.AffectedHost].Nodes[PR.Title].Nodes.Add(PR.Id.ToString(), PR.Id.ToString());
                    }
                }
                int HighVulnerabilityCount = 0;
                foreach (TreeNode HighNode in UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Nodes)
                {
                    int HostHighVulnerabilityCount = 0;
                    foreach (TreeNode PluginNode in HighNode.Nodes)
                    {
                        HostHighVulnerabilityCount = HostHighVulnerabilityCount + PluginNode.Nodes.Count;
                        if (PluginNode.Nodes.Count > 0) PluginNode.Text = PluginNode.Name + " (" + PluginNode.Nodes.Count.ToString() + ")";
                    }
                    HighVulnerabilityCount = HighVulnerabilityCount + HostHighVulnerabilityCount;
                    if (HostHighVulnerabilityCount > 0) HighNode.Text = HighNode.Name + " (" + HostHighVulnerabilityCount.ToString() + ")";
                }
                int MediumVulnerabilityCount = 0;
                foreach (TreeNode MediumNode in UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Nodes)
                {
                    int HostMediumVulnerabilityCount = 0;
                    foreach (TreeNode PluginNode in MediumNode.Nodes)
                    {
                        HostMediumVulnerabilityCount = HostMediumVulnerabilityCount + PluginNode.Nodes.Count;
                        if (PluginNode.Nodes.Count > 0) PluginNode.Text = PluginNode.Name + " (" + PluginNode.Nodes.Count.ToString() + ")";
                    }
                    MediumVulnerabilityCount = MediumVulnerabilityCount + HostMediumVulnerabilityCount;
                    if (HostMediumVulnerabilityCount > 0) MediumNode.Text = MediumNode.Name + " (" + HostMediumVulnerabilityCount.ToString() + ")";
                }
                int LowVulnerabilityCount = 0;
                foreach (TreeNode LowNode in UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Nodes)
                {
                    int HostLowVulnerabilityCount = 0;
                    foreach (TreeNode PluginNode in LowNode.Nodes)
                    {
                        HostLowVulnerabilityCount = HostLowVulnerabilityCount + PluginNode.Nodes.Count;
                        if (PluginNode.Nodes.Count > 0) PluginNode.Text = PluginNode.Name + " (" + PluginNode.Nodes.Count.ToString() + ")";
                    }
                    LowVulnerabilityCount = LowVulnerabilityCount + HostLowVulnerabilityCount;
                    if (HostLowVulnerabilityCount > 0) LowNode.Text = LowNode.Name + " (" + HostLowVulnerabilityCount.ToString() + ")";
                }
                int VulnerabilityCount = HighVulnerabilityCount + MediumVulnerabilityCount + LowVulnerabilityCount ;

                int TestLeadCount = 0;
                foreach (TreeNode TestLeadNode in UI.IronTree.Nodes[0].Nodes[1].Nodes)
                {
                    int HostTestLeadCount = 0;
                    foreach (TreeNode PluginNode in TestLeadNode.Nodes)
                    {
                        HostTestLeadCount = HostTestLeadCount + PluginNode.Nodes.Count;
                        if (PluginNode.Nodes.Count > 0) PluginNode.Text = PluginNode.Name + " (" + PluginNode.Nodes.Count.ToString() + ")";
                    }
                    TestLeadCount = TestLeadCount + HostTestLeadCount;
                    if (HostTestLeadCount > 0) TestLeadNode.Text = TestLeadNode.Name + " (" + HostTestLeadCount.ToString() + ")";
                }
                int InformationCount = 0;
                foreach (TreeNode InformationNode in UI.IronTree.Nodes[0].Nodes[2].Nodes)
                {
                    int HostInformationCount = 0;
                    foreach (TreeNode PluginNode in InformationNode.Nodes)
                    {
                        HostInformationCount = HostInformationCount + PluginNode.Nodes.Count;
                        if (PluginNode.Nodes.Count > 0) PluginNode.Text = PluginNode.Name + " (" + PluginNode.Nodes.Count.ToString() + ")";
                    }
                    InformationCount = InformationCount + HostInformationCount;
                    if (HostInformationCount > 0) InformationNode.Text = InformationNode.Name + " (" + HostInformationCount.ToString() + ")";
                }

                if(HighVulnerabilityCount > 0) UI.IronTree.Nodes[0].Nodes[0].Nodes[0].Text = "High (" +  HighVulnerabilityCount.ToString() + ")";
                if(MediumVulnerabilityCount > 0) UI.IronTree.Nodes[0].Nodes[0].Nodes[1].Text = "Medium (" + MediumVulnerabilityCount.ToString() + ")";
                if(LowVulnerabilityCount > 0 ) UI.IronTree.Nodes[0].Nodes[0].Nodes[2].Text = "Low (" + LowVulnerabilityCount.ToString() + ")";
                if (VulnerabilityCount > 0) UI.IronTree.Nodes[0].Nodes[0].Text = "Vulnerabilities (" + VulnerabilityCount.ToString() + ")";
                if(TestLeadCount > 0 ) UI.IronTree.Nodes[0].Nodes[1].Text = "Test Leads (" + TestLeadCount.ToString() + ")";
                if(InformationCount > 0) UI.IronTree.Nodes[0].Nodes[2].Text = "Information (" + InformationCount.ToString() + ")";
                
                UI.IronTree.Enabled = true;
                UI.IronTree.EndUpdate();
            }
        }


        public delegate void UpdateSitemapTree_d(List<List<string>> Urls);
        public static void UpdateSitemapTree(List<List<string>> Urls)
        {
            if (UI.IronTree.InvokeRequired)
            {
                UpdateSitemapTree_d UST_d = new UpdateSitemapTree_d(UpdateSitemapTree);
                UI.Invoke(UST_d, new object[] { Urls });
            }
            else
            {
                if (UI.IronTree == null) return;
                
                UI.IronTree.BeginUpdate();
                UI.IronTree.Enabled = false;
                TreeNode SiteMapNode = UI.IronTree.Nodes[0].Nodes["SiteMap"];
                foreach (List<string> Url in Urls)
                {
                    int KeyLoc = SiteMapNode.Nodes.IndexOfKey(Url[0]);
                    if (KeyLoc < 0)
                    {
                        TreeNode Node = SiteMapNode.Nodes.Add(Url[0], Url[0]);
                        KeyLoc = Node.Index;
                    }
                    UpdateUrlNodesRecursively(SiteMapNode.Nodes[KeyLoc], Url, 1);
                }
                UI.IronTree.Enabled = true;
                UI.IronTree.EndUpdate();
            }
        }

        static void UpdateUrlNodesRecursively(TreeNode Node, List<string> Url, int CurrentLevel)
        {
            if (Url[CurrentLevel] == "")
            {
                TreeNode EndNode;
                if (CurrentLevel == 1)
                {
                    if (!Node.Nodes.ContainsKey("/"))
                    {
                        EndNode = Node.Nodes.Add("/", "/");
                    }
                    else
                    {
                        EndNode = Node.Nodes["/"];
                    }
                }
                else
                {
                    EndNode = Node;
                }
                if (Url.Count > CurrentLevel + 1)
                {
                    if (Url[CurrentLevel + 1].Length > 0)
                    {
                        UpdateQueryStringToNode(EndNode, Url[CurrentLevel + 1]);
                    }
                }
            }
            else
            {
                if (!Node.Nodes.ContainsKey(Url[CurrentLevel]))
                {
                    TreeNode ChildNode = Node.Nodes.Add(Url[CurrentLevel], Url[CurrentLevel]);
                    CurrentLevel++;
                    UpdateUrlNodesRecursively(ChildNode, Url, CurrentLevel);
                }
                else
                {
                    TreeNode ChildNode = Node.Nodes[Url[CurrentLevel]];
                    CurrentLevel++;
                    UpdateUrlNodesRecursively(ChildNode, Url, CurrentLevel);
                }
            }
        }

        static void UpdateQueryStringToNode(TreeNode Node, string QueryString)
        {
            if (!Node.Nodes.ContainsKey(QueryString))
            {
                Node.Nodes.Add(QueryString, QueryString);
            }
        }


        internal static bool IsExceptionsNodeSelected()
        {
            TreeNode Node = UI.IronTree.SelectedNode;
            if (Node == null) return false;
            if (Node.Level == 2 && Node.Parent.Index == 3)
            {
                return true;
            }
            return false;
        }
        internal static bool IsFindingsNodeSelected()
        {
            TreeNode Node = UI.IronTree.SelectedNode;
            if (Node == null) return false;
            if (Node.Level == 4 && (Node.Parent.Parent.Parent.Index == 1 || Node.Parent.Parent.Parent.Index == 2))
            {
                return true;
            }
            else if (Node.Level == 5 && Node.Parent.Parent.Parent.Parent.Index == 0)
            {
                return true;
            }
            return false;
        }
        internal static bool IsSiteMapNodeSelected()
        {
            TreeNode Node = UI.IronTree.SelectedNode;
            if (Node == null) return false;
            if ((Node.Level > 5) || (Node.Level == 5 && (Node.Parent.Parent.Parent.Parent.Index == 4)) || (Node.Level == 4 && (Node.Parent.Parent.Parent.Index == 4)) || (Node.Level == 3 && (Node.Parent.Parent.Index == 4)) || (Node.Level == 2 && (Node.Parent.Index == 4)))
            {
                return true;
            }
            return false;
        }

        internal static int GetFindingIdFromIronTree()
        {
            TreeNode Node = UI.IronTree.SelectedNode;
            if (Node == null) return -1;
            if (IsFindingsNodeSelected())
            {
                try
                {
                    return Int32.Parse(Node.Name);
                }
                catch { }
            }
            return -1;
        }

        internal static Request GetSelectedUrlFromSiteMap()
        {
            TreeNode Node = UI.IronTree.SelectedNode;
            if (Node == null) return null;
            if (IsSiteMapNodeSelected())
            {
                List<string> UrlPaths = new List<string>();
                string Query = "";
                TreeNode SiteMapNode = Node;
                if (SiteMapNode.Text.StartsWith("?"))
                {
                    Query = SiteMapNode.Text;
                    SiteMapNode = SiteMapNode.Parent;
                }
                while (SiteMapNode.Level > 2)
                {
                    UrlPaths.Add(SiteMapNode.Text);
                    SiteMapNode = SiteMapNode.Parent;
                }
                UrlPaths.Reverse();
                StringBuilder UrlBuilder = new StringBuilder();
                foreach (string Path in UrlPaths)
                {
                    UrlBuilder.Append("/"); UrlBuilder.Append(Path);
                }
                UrlBuilder.Append(Query);
                string BaseUrl = SiteMapNode.Text;
                //string Url = UrlBuilder.ToString() + Query;
                string Url = UrlBuilder.ToString();
                if (Url == "//") Url = "/";
                if (Url.StartsWith("/"))
                    Url = Url.TrimStart(new char[]{'/'});
                try
                {
                    return new Request(string.Format("{0}{1}", BaseUrl, Url));
                }
                catch { }
                //IronUI.UpdateResultsTab(Host, Url);
                //return null;
            }
            
            //if (e.Node == null) return;
            //if ((e.Node.Level > 5) || (e.Node.Level == 5 && (e.Node.Parent.Parent.Parent.Parent.Index == 4)) || (e.Node.Level == 4 && (e.Node.Parent.Parent.Parent.Index == 4)) || (e.Node.Level == 3 && (e.Node.Parent.Parent.Index == 4)) || (e.Node.Level == 2 && (e.Node.Parent.Index == 4)))
            //{
            //    List<string> UrlPaths = new List<string>();
            //    string Query = "";
            //    TreeNode SiteMapNode = e.Node;
            //    if (SiteMapNode.Text.StartsWith("?"))
            //    {
            //        Query = SiteMapNode.Text;
            //        SiteMapNode = SiteMapNode.Parent;
            //    }
            //    while (SiteMapNode.Level > 2)
            //    {
            //        UrlPaths.Add(SiteMapNode.Text);
            //        SiteMapNode = SiteMapNode.Parent;
            //    }
            //    UrlPaths.Reverse();
            //    StringBuilder UrlPathBuilder = new StringBuilder();
            //    foreach (string Path in UrlPaths)
            //    {
            //        UrlPathBuilder.Append("/"); UrlPathBuilder.Append(Path);
            //    }
            //    string Host = SiteMapNode.Text;
            //    string Url = UrlPathBuilder.ToString() + Query;
            //    if (Url == "//") Url = "/";
            //    IronUI.UpdateResultsTab(Host, Url);
            //    return;
            //}
            
            //if (UI.IronTree.SelectedNode == null) return "";
            //TreeNode Node = UI.IronTree.SelectedNode;
            //if ((Node.Level > 4) || (Node.Level == 4 && (Node.Parent.Parent.Parent.Index == 4)) || (Node.Level == 3 && (Node.Parent.Parent.Index == 4)) || (Node.Level == 2 && (Node.Parent.Index == 4)))
            //{
            //    List<string> UrlPaths = new List<string>();
            //    string Query = "";
            //    TreeNode SiteMapNode = Node;
            //    if (SiteMapNode.Text.StartsWith("?"))
            //    {
            //        Query = SiteMapNode.Text;
            //        SiteMapNode = SiteMapNode.Parent;
            //    }
            //    while (SiteMapNode.Level > 2)
            //    {
            //        UrlPaths.Add(SiteMapNode.Text);
            //        SiteMapNode = SiteMapNode.Parent;
            //    }
            //    UrlPaths.Reverse();
            //    StringBuilder UrlPathBuilder = new StringBuilder();
            //    foreach (string Path in UrlPaths)
            //    {
            //        UrlPathBuilder.Append("/"); UrlPathBuilder.Append(Path);
            //    }
            //    string BaseUrl = SiteMapNode.Text;
            //    string Url = UrlPathBuilder.ToString();// +Query;
            //    if (Url.StartsWith("/"))
            //    {
            //        Url.TrimStart(new char[]{'/'});
            //    }
            //    return string.Format("{0}{1}", BaseUrl, Url);
            //}
            //return "";
            return null;
        }

        delegate void SendSessionToProxy_d(Session IrSe);
        internal static void SendSessionToProxy(Session IrSe)
        {
            if (UI.ProxyInterceptTabs.InvokeRequired)
            {
                SendSessionToProxy_d sstp_d = new SendSessionToProxy_d(SendSessionToProxy);
                UI.Invoke(sstp_d, new object[] { IrSe });
            }
            else
            {
                if (IronProxy.ManualTamperingFree)
                {
                    FillInterceptorTab(IrSe);
                    if (!UI.main_tab.SelectedTab.Name.Equals("mt_proxy")) UI.main_tab.SelectTab("mt_proxy");
                    MakeUiTopMost(true);
                }
                else
                {
                    string ID = IrSe.ID.ToString();
                    if (IrSe.FiddlerSession.state == Fiddler.SessionStates.HandTamperRequest)
                    {
                        ID = ID + "-Request";
                    }
                    else
                    {
                        ID = ID + "-Response";
                    }
                    lock (IronProxy.SessionsQ)
                    {
                        IronProxy.SessionsQ.Enqueue(ID);
                    }
                }
            }
        }
        internal static void FillInterceptorTab(Session IrSe)
        {
            IronProxy.ManualTamperingFree = false;
            IronProxy.CurrentSession = IrSe;
            ResetProxyInterceptionFields();

            if (IrSe.FiddlerSession.state == Fiddler.SessionStates.HandTamperRequest)
            {
                UI.ProxyInterceptTabs.SelectedIndex = 0;
                IronProxy.CurrentSession.OriginalRequest = IrSe.Request.GetClone(true);
                FillProxyFields(IrSe.Request);
                MakeProxyRequestFieldsReadOnly(false);
            }
            else
            {
                UI.ProxyInterceptTabs.SelectedIndex = 1;
                IronProxy.CurrentSession.OriginalResponse = IrSe.Response.GetClone(true);
                FillProxyFields(IrSe.Response, IrSe.Request);
                FillProxyFields(IrSe.Request);
                MakeProxyResponseFieldsReadOnly(false);
            }
            IronProxy.ResetChangedStatus();
            UI.ProxyBaseSplit.Panel1.BackColor = Color.SkyBlue;
            UI.ProxySendBtn.Enabled = true;
            UI.ProxyDropBtn.Enabled = true;
        }

        internal static void FillProxyFields(Request Req)
        {
            UI.ProxyRequestView.SetRequest(Req);
            //FillProxyRequestHeaderFields(Request);
            //UI.ProxyRequestHeadersIDV.ReadOnly = false;
            //UI.ProxyRequestBodyIDV.ReadOnly = false;
            //if (Request.HasBody)
            //{
            //    FillProxyRequestBodyFields(Request);
            //}
            //FillProxyParametersFields(Request);
            //UI.ProxyRequestParametersQueryGrid.Columns[1].ReadOnly = false;
            //UI.ProxyRequestParametersBodyGrid.Columns[1].ReadOnly = false;
            //UI.ProxyRequestParametersCookieGrid.Columns[1].ReadOnly = false;
            //UI.ProxyRequestParametersHeadersGrid.Columns[1].ReadOnly = false;
        }

        //internal static void FillProxyRequestHeaderFields(Request Request)
        //{
        //    UI.ProxyRequestHeadersIDV.Text = Request.GetHeadersAsStringWithoutFullURL();
        //}

        //internal static void FillProxyRequestBodyFields(Request Request)
        //{
        //    if (Request.IsBinary)
        //    {
        //        UI.ProxyRequestBodyIDV.Text = Encoding.UTF8.GetString(Request.BodyArray);
        //        UI.ProxyRequestBodyIDV.ReadOnly = true;
        //    }
        //    else
        //    {
        //        UI.ProxyRequestBodyIDV.Text = Request.BodyString;
        //    }
        //}

        //internal static void FillProxyParametersFields(Request Request)
        //{
        //    UI.ProxyRequestParametersQueryGrid.Rows.Clear();
        //    foreach (string Name in Request.Query.GetNames())
        //    {
        //        foreach (string Value in Request.Query.GetAll(Name))
        //        {
        //            UI.ProxyRequestParametersQueryGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.ProxyRequestParametersBodyGrid.Rows.Clear();
        //    foreach (string Name in Request.Body.GetNames())
        //    {
        //        foreach (string Value in Request.Body.GetAll(Name))
        //        {
        //            UI.ProxyRequestParametersBodyGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.ProxyRequestParametersCookieGrid.Rows.Clear();
        //    foreach (string Name in Request.Cookie.GetNames())
        //    {
        //        foreach (string Value in Request.Cookie.GetAll(Name))
        //        {
        //            UI.ProxyRequestParametersCookieGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.ProxyRequestParametersHeadersGrid.Rows.Clear();
        //    foreach (string Name in Request.Headers.GetNames())
        //    {
        //        if (!Name.Equals("Host", StringComparison.OrdinalIgnoreCase) && !Name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
        //        {
        //            foreach (string Value in Request.Headers.GetAll(Name))
        //            {
        //                UI.ProxyRequestParametersHeadersGrid.Rows.Add(new object[] { Name, Value });
        //            }
        //        }
        //    }
        //}

        delegate void ShowProxyException_d(string Message);
        internal static void ShowProxyException(string Message)
        {
            if (UI.ProxyExceptionTB.InvokeRequired)
            {
                ShowProxyException_d SPE_d = new ShowProxyException_d(ShowProxyException);
                UI.Invoke(SPE_d, new object[] { Message });
            }
            else
            {
                UI.ProxyExceptionTB.Text = Message;
                UI.ProxyExceptionTB.Visible = true;
            }
        }

        internal static void ResetProxyException()
        {
            UI.ProxyExceptionTB.Text = "";
            UI.ProxyExceptionTB.Visible = false;
        }

        //internal static void UpdateProxyHeaderFieldsWithUIQueryParameters()
        //{
        //    IronProxy.CurrentSession.Request.Query.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.ProxyRequestParametersQueryGrid.Rows)
        //    {
        //        IronProxy.CurrentSession.Request.Query.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    FillProxyRequestHeaderFields(IronProxy.CurrentSession.Request);
        //}
        //internal static void UpdateProxyBodyFieldsWithUIBodyParameters()
        //{
        //    IronProxy.CurrentSession.Request.Body.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.ProxyRequestParametersBodyGrid.Rows)
        //    {
        //        IronProxy.CurrentSession.Request.Body.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    FillProxyRequestBodyFields(IronProxy.CurrentSession.Request);
        //}
        //internal static void UpdateProxyHeaderFieldsWithUICookieParameters()
        //{
        //    IronProxy.CurrentSession.Request.Cookie.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.ProxyRequestParametersCookieGrid.Rows)
        //    {
        //        IronProxy.CurrentSession.Request.Cookie.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    FillProxyRequestHeaderFields(IronProxy.CurrentSession.Request);
        //}
        //internal static void UpdateProxyHeaderFieldsWithUIHeadersParameters()
        //{
        //    Parameters TempHolder = new Parameters();
        //    if (IronProxy.CurrentSession.Request.Headers.Has("Host"))
        //    {
        //        TempHolder.Set("Host", IronProxy.CurrentSession.Request.Headers.Get("Host"));
        //    }
        //    if (IronProxy.CurrentSession.Request.Headers.Has("Cookie"))
        //    {
        //        TempHolder.Set("Cookie", IronProxy.CurrentSession.Request.Headers.Get("Cookie"));
        //    }
        //    IronProxy.CurrentSession.Request.Headers.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.ProxyRequestParametersHeadersGrid.Rows)
        //    {
        //        IronProxy.CurrentSession.Request.Headers.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    foreach (string Name in TempHolder.GetNames())
        //    {
        //        IronProxy.CurrentSession.Request.Headers.Set(Name, TempHolder.Get(Name));
        //    }
        //    FillProxyRequestHeaderFields(IronProxy.CurrentSession.Request);
        //}

        //internal static void HandleAnyChangesInRequest()
        //{
        //    if (IronProxy.RequestHeaderChanged)
        //    {
        //        IronProxy.UpdateCurrentSessionWithNewRequestHeader(UI.ProxyRequestHeadersIDV.Text);
        //        IronUI.FillProxyParametersFields(IronProxy.CurrentSession.Request);
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //    if (IronProxy.RequestBodyChanged)
        //    {
        //        IronProxy.UpdateCurrentSessionWithNewRequestBodyText(UI.ProxyRequestBodyIDV.Text);
        //        IronUI.FillProxyParametersFields(IronProxy.CurrentSession.Request);
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //    if (IronProxy.RequestQueryParametersChanged)
        //    {
        //        UpdateProxyHeaderFieldsWithUIQueryParameters();
        //        IronProxy.UpdateFiddlerSessionWithNewRequestHeader();
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //    if (IronProxy.RequestBodyParametersChanged)
        //    {
        //        UpdateProxyBodyFieldsWithUIBodyParameters();
        //        IronProxy.UpdateFiddlerSessionWithNewRequestBody();
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //    if (IronProxy.RequestCookieParametersChanged)
        //    {
        //        UpdateProxyHeaderFieldsWithUICookieParameters();
        //        IronProxy.UpdateFiddlerSessionWithNewRequestHeader();
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //    if (IronProxy.RequestHeaderParametersChanged)
        //    {
        //        UpdateProxyHeaderFieldsWithUIHeadersParameters();
        //        IronProxy.UpdateFiddlerSessionWithNewRequestHeader();
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //}

        internal static void FillProxyFields(Response Res, Request Req)
        {
            UI.ProxyResponseView.SetResponse(Res, Req);
            //UI.ProxyResponseHeadersIDV.Text = Response.GetHeadersAsString();
            //UI.ProxyResponseHeadersIDV.ReadOnly = false;
            //UI.ProxyResponseBodyIDV.ReadOnly = false;
            //if (Response.HasBody)
            //{
            //    if (Response.IsBinary)
            //    {
            //        UI.ProxyResponseBodyIDV.Text = Encoding.UTF8.GetString(Response.BodyArray);
            //        UI.ProxyResponseBodyIDV.ReadOnly = true;
            //    }
            //    else
            //    {
            //        UI.ProxyResponseBodyIDV.Text = Response.BodyString;
            //    }
            //}
        }

        //internal static void HandleAnyChangesInResponse()
        //{
        //    if (IronProxy.ResponseHeaderChanged)
        //    {
        //        IronProxy.UpdateCurrentSessionWithNewResponseHeader(UI.ProxyResponseHeadersIDV.Text);
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //    if (IronProxy.ResponseBodyChanged)
        //    {
        //        IronProxy.UpdateCurrentSessionWithNewResponseBodyText(UI.ProxyResponseBodyIDV.Text);
        //        IronProxy.ResetNonParameterChangedStatus();
        //        IronProxy.ResetParametersChangedStatus();
        //    }
        //}

        //delegate void FillProxyRequestFormatXML_d(string XML);
        //internal static void FillProxyRequestFormatXML(string XML)
        //{
        //    if (UI.ProxyRequestFormatXMLTB.InvokeRequired)
        //    {
        //        FillProxyRequestFormatXML_d FPRFX_d = new FillProxyRequestFormatXML_d(FillProxyRequestFormatXML);
        //        UI.Invoke(FPRFX_d, new object[] { XML });
        //    }
        //    else
        //    {
        //        UI.ProxyRequestFormatXMLTB.Text = XML;
        //        UI.ProxyRequestFormatXMLTB.ReadOnly = false;
        //    }
        //}

        //delegate void FillProxyRequestWithNewRequestFromFormatXML_d(Request Request, string PluginName);
        //internal static void FillProxyRequestWithNewRequestFromFormatXML(Request Request, string PluginName)
        //{
        //    if (UI.ProxyRequestFormatXMLTB.InvokeRequired)
        //    {
        //        FillProxyRequestWithNewRequestFromFormatXML_d FPRWNRFFX_d = new FillProxyRequestWithNewRequestFromFormatXML_d(FillProxyRequestWithNewRequestFromFormatXML);
        //        UI.Invoke(FPRWNRFFX_d, new object[] { Request, PluginName });
        //    }
        //    else
        //    {
        //        try
        //        {
        //            ResetProxyRequestDisplayFields();
        //            FillProxyFields(Request);
        //            IronProxy.ResetChangedStatus();
        //            IronProxy.RequestChanged = true;//only then the edited request will be updated in the logs
        //        }
        //        catch (Exception Exp)
        //        {
        //            IronException.Report("Error displaying Updated 'Proxy' Request from Serializing XML using Format Plugin - " + PluginName, Exp.Message, Exp.StackTrace);
        //            IronUI.ShowMTException("Error displaying updated Request");
        //        }
        //    }
        //}

        //delegate void FillProxyResponseFormatXML_d(string XML);
        //internal static void FillProxyResponseFormatXML(string XML)
        //{
        //    if (UI.ProxyResponseFormatXMLTB.InvokeRequired)
        //    {
        //        FillProxyResponseFormatXML_d FPRFX_d = new FillProxyResponseFormatXML_d(FillProxyResponseFormatXML);
        //        UI.Invoke(FPRFX_d, new object[] { XML });
        //    }
        //    else
        //    {
        //        UI.ProxyResponseFormatXMLTB.Text = XML;
        //        UI.ProxyResponseFormatXMLTB.ReadOnly = false;
        //    }
        //}

        //delegate void FillProxyResponseWithNewResponseFromFormatXML_d(Response Response, string PluginName);
        //internal static void FillProxyResponseWithNewResponseFromFormatXML(Response Response, string PluginName)
        //{
        //    if (UI.ProxyResponseFormatXMLTB.InvokeRequired)
        //    {
        //        FillProxyResponseWithNewResponseFromFormatXML_d FPRWNRFFX_d = new FillProxyResponseWithNewResponseFromFormatXML_d(FillProxyResponseWithNewResponseFromFormatXML);
        //        UI.Invoke(FPRWNRFFX_d, new object[] { Response, PluginName });
        //    }
        //    else
        //    {
        //        try
        //        {
        //            ResetProxyResponseDisplayFields();
        //            FillProxyFields(Response);
        //            IronProxy.ResetChangedStatus();
        //            IronProxy.ResponseChanged = true;//only then the edited response will be updated in the logs
        //        }
        //        catch (Exception Exp)
        //        {
        //            IronException.Report("Error displaying Updated 'Proxy' Request from Serializing XML using Format Plugin - " + PluginName, Exp.Message, Exp.StackTrace);
        //            IronUI.ShowMTException("Error displaying updated Request");
        //        }
        //    }
        //}


        internal static void ResetProxyInterceptionFields()
        {
            ResetProxyRequestDisplayFields();
            ResetProxyResponseDisplayFields();
            MakeProxyFieldsReadOnly(true);
            ResetProxyException();
        }

        internal static void ResetProxyRequestDisplayFields()
        {
            //UI.ProxyRequestHeadersIDV.Text = "";
            //UI.ProxyRequestBodyIDV.Text = "";
            //UI.ProxyRequestParametersQueryGrid.Rows.Clear();
            //UI.ProxyRequestParametersBodyGrid.Rows.Clear();
            //UI.ProxyRequestParametersCookieGrid.Rows.Clear();
            //UI.ProxyRequestParametersHeadersGrid.Rows.Clear();
            UI.ProxyShowOriginalRequestCB.Checked = false;
            //UI.ProxyRequestFormatXMLTB.Text = "";
            UI.ProxyRequestView.ClearRequest();
        }

        internal static void ResetProxyResponseDisplayFields()
        {
            UI.ProxyResponseView.ClearResponse();
            //UI.ProxyResponseHeadersIDV.Text = "";
            //UI.ProxyResponseBodyIDV.Text = "";
            UI.ProxyShowOriginalResponseCB.Checked = false;
            //UI.ProxyResponseFormatXMLTB.Text = "";
        }

        internal static void MakeProxyFieldsReadOnly(bool ReadOnly)
        {
            MakeProxyRequestFieldsReadOnly(ReadOnly);
            MakeProxyResponseFieldsReadOnly(ReadOnly);
        }

        internal static void MakeProxyRequestFieldsReadOnly(bool ReadOnly)
        {
            UI.ProxyRequestView.SetReadOnly(ReadOnly);
            //UI.ProxyRequestHeadersIDV.ReadOnly = true;
            //UI.ProxyRequestBodyIDV.ReadOnly = true;
            //UI.ProxyRequestParametersQueryGrid.Columns[1].ReadOnly = true;
            //UI.ProxyRequestParametersBodyGrid.Columns[1].ReadOnly = true;
            //UI.ProxyRequestParametersCookieGrid.Columns[1].ReadOnly = true;
            //UI.ProxyRequestParametersHeadersGrid.Columns[1].ReadOnly = true;
            //UI.ProxyRequestFormatXMLTB.ReadOnly = true;
        }
        internal static void MakeProxyResponseFieldsReadOnly(bool ReadOnly)
        {
            UI.ProxyResponseView.SetReadOnly(ReadOnly);
            //UI.ProxyResponseHeadersIDV.ReadOnly = true;
            //UI.ProxyResponseBodyIDV.ReadOnly = true;
            //UI.ProxyResponseFormatXMLTB.ReadOnly = true;
        }

        internal static void FillMTFields(Session IrSe)
        {
            IronUI.FillMTFields(IrSe.Request);
            ManualTesting.SetCurrentID(IrSe.Request.ID);
            if (IrSe.Response != null) IronUI.FillMTFields(IrSe.Response, IrSe.Request);
            UI.TestIDLbl.Text = "ID: " + IrSe.Request.ID.ToString();
            string Group = IrSe.Flags["Group"].ToString();
            UI.MTCurrentGroupNameTB.Text = Group;
            foreach (DataGridViewRow Row in UI.TestGroupLogGrid.Rows)
            {
                if (Row.Cells[1].Value.ToString().Equals(IrSe.Request.ID.ToString()))
                {
                    Row.Selected = true;
                    try
                    {
                        UI.TestGroupLogGrid.FirstDisplayedScrollingRowIndex = Row.Index;
                    }
                    catch { }
                    break;
                }
            }
            //switch (GroupColor)
            //{
            //    case("Red"):
            //        UI.TestIDLbl.BackColor = Color.Red;
            //        break;
            //    case ("Blue"):
            //        UI.TestIDLbl.BackColor = Color.RoyalBlue;
            //        break;
            //    case ("Green"):
            //        UI.TestIDLbl.BackColor = Color.Green;
            //        break;
            //    case ("Gray"):
            //        UI.TestIDLbl.BackColor = Color.Gray;
            //        break;
            //    case ("Brown"):
            //        UI.TestIDLbl.BackColor = Color.Brown;
            //        break;
            //}
        }

        internal static void FillMTFields(Request Request)
        {
            //FillMTRequestHeaderFields(Request);
            //if (Request.HasBody)
            //{
            //    FillMTRequestBodyFields(Request);
            //}
            //FillMTParametersFields(Request);
            //UI.MTRequestParametersQueryGrid.Columns[1].ReadOnly = false;
            //UI.MTRequestParametersBodyGrid.Columns[1].ReadOnly = false;
            //UI.MTRequestParametersCookieGrid.Columns[1].ReadOnly = false;
            //UI.MTRequestParametersHeadersGrid.Columns[1].ReadOnly = false;
            UI.TestRequestView.SetRequest(Request);
            ManualTesting.ResetChangedStatus();
            ManualTesting.CurrentRequest = Request;
        }

        //internal static void FillMTRequestHeaderFields(Request Request)
        //{
        //    UI.MTRequestHeadersIDV.Text = Request.GetHeadersAsStringWithoutFullURL();
        //    UI.MTIsSSLCB.Checked = Request.SSL;
        //}

        //internal static void FillMTRequestBodyFields(Request Request)
        //{
        //    if (Request.IsBinary)
        //    {
        //        UI.MTRequestBodyIDV.Text = Encoding.UTF8.GetString(Request.BodyArray);
        //        UI.MTRequestBodyIDV.ReadOnly = true;
        //    }
        //    else
        //    {
        //        UI.MTRequestBodyIDV.Text = Request.BodyString;
        //    }
        //}

        //internal static void FillMTParametersFields(Request Request)
        //{
        //    UI.MTRequestParametersQueryGrid.Rows.Clear();
        //    foreach (string Name in Request.Query.GetNames())
        //    {
        //        foreach (string Value in Request.Query.GetAll(Name))
        //        {
        //            UI.MTRequestParametersQueryGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.MTRequestParametersBodyGrid.Rows.Clear();
        //    foreach (string Name in Request.Body.GetNames())
        //    {
        //        foreach (string Value in Request.Body.GetAll(Name))
        //        {
        //            UI.MTRequestParametersBodyGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.MTRequestParametersCookieGrid.Rows.Clear();
        //    foreach (string Name in Request.Cookie.GetNames())
        //    {
        //        foreach (string Value in Request.Cookie.GetAll(Name))
        //        {
        //            UI.MTRequestParametersCookieGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.MTRequestParametersHeadersGrid.Rows.Clear();
        //    foreach (string Name in Request.Headers.GetNames())
        //    {
        //        if (!Name.Equals("Host", StringComparison.OrdinalIgnoreCase) && !Name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
        //        {
        //            foreach (string Value in Request.Headers.GetAll(Name))
        //            {
        //                UI.MTRequestParametersHeadersGrid.Rows.Add(new object[] { Name, Value });
        //            }
        //        }
        //    }
        //}

        //internal static void UpdateMTHeaderFieldsWithUIQueryParameters()
        //{
        //    if (ManualTesting.CurrentRequest == null) return;
        //    ManualTesting.CurrentRequest.Query.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.MTRequestParametersQueryGrid.Rows)
        //    {
        //        ManualTesting.CurrentRequest.Query.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    FillMTRequestHeaderFields(ManualTesting.CurrentRequest);
        //}
        //internal static void UpdateMTBodyFieldsWithUIBodyParameters()
        //{
        //    if (ManualTesting.CurrentRequest == null) return;
        //    ManualTesting.CurrentRequest.Body.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.MTRequestParametersBodyGrid.Rows)
        //    {
        //        ManualTesting.CurrentRequest.Body.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    FillMTRequestBodyFields(ManualTesting.CurrentRequest);
        //}
        //internal static void UpdateMTHeaderFieldsWithUICookieParameters()
        //{
        //    if (ManualTesting.CurrentRequest == null) return;
        //    ManualTesting.CurrentRequest.Cookie.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.MTRequestParametersCookieGrid.Rows)
        //    {
        //        ManualTesting.CurrentRequest.Cookie.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    FillMTRequestHeaderFields(ManualTesting.CurrentRequest);
        //}
        //internal static void UpdateMTHeaderFieldsWithUIHeadersParameters()
        //{
        //    if (ManualTesting.CurrentRequest == null) return;
        //    Parameters TempHolder = new Parameters();
        //    if (ManualTesting.CurrentRequest.Headers.Has("Host"))
        //    {
        //        TempHolder.Set("Host", ManualTesting.CurrentRequest.Headers.Get("Host"));
        //    }
        //    if (ManualTesting.CurrentRequest.Headers.Has("Cookie"))
        //    {
        //        TempHolder.Set("Cookie", ManualTesting.CurrentRequest.Headers.Get("Cookie"));
        //    }
        //    ManualTesting.CurrentRequest.Headers.RemoveAll();
        //    foreach (DataGridViewRow Row in UI.MTRequestParametersHeadersGrid.Rows)
        //    {
        //        ManualTesting.CurrentRequest.Headers.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
        //    }
        //    foreach (string Name in TempHolder.GetNames())
        //    {
        //        ManualTesting.CurrentRequest.Headers.Set(Name, TempHolder.Get(Name));
        //    }
        //    FillMTRequestHeaderFields(ManualTesting.CurrentRequest);
        //}

        //internal static void HandleAnyChangesInMTRequest()
        //{
        //    if (ManualTesting.RequestHeaderChanged)
        //    {
        //        ManualTesting.UpdateCurrentRequestWithNewHeader(UI.MTRequestHeadersIDV.Text);
        //        if (ManualTesting.CurrentRequest == null) return;
        //        IronUI.FillMTParametersFields(ManualTesting.CurrentRequest);
        //        if (ManualTesting.RequestBodyChanged)
        //        {
        //            ManualTesting.RequestHeaderChanged = false;
        //        }
        //        else
        //        {
        //            ManualTesting.ResetNonParameterChangedStatus();
        //        }
        //        ManualTesting.ResetParametersChangedStatus();
        //    }
        //    if (ManualTesting.RequestBodyChanged)
        //    {
        //        ManualTesting.UpdateCurrentRequestWithNewBodyText(UI.MTRequestBodyIDV.Text);
        //        if (ManualTesting.CurrentRequest == null) return;
        //        IronUI.FillMTParametersFields(ManualTesting.CurrentRequest);
        //        ManualTesting.ResetNonParameterChangedStatus();
        //        ManualTesting.ResetParametersChangedStatus();
        //    }
        //    if (ManualTesting.CurrentRequest == null) return;
        //    if (ManualTesting.RequestQueryParametersChanged)
        //    {
        //        UpdateMTHeaderFieldsWithUIQueryParameters();
        //        ManualTesting.ResetNonParameterChangedStatus();
        //        ManualTesting.ResetParametersChangedStatus();
        //    }
        //    if (ManualTesting.RequestBodyParametersChanged)
        //    {
        //        UpdateMTBodyFieldsWithUIBodyParameters();
        //        ManualTesting.ResetNonParameterChangedStatus();
        //        ManualTesting.ResetParametersChangedStatus();
        //    }
        //    if (ManualTesting.RequestCookieParametersChanged)
        //    {
        //        UpdateMTHeaderFieldsWithUICookieParameters();
        //        ManualTesting.ResetNonParameterChangedStatus();
        //        ManualTesting.ResetParametersChangedStatus();
        //    }
        //    if (ManualTesting.RequestHeaderParametersChanged)
        //    {
        //        UpdateMTHeaderFieldsWithUIHeadersParameters();
        //        ManualTesting.ResetNonParameterChangedStatus();
        //        ManualTesting.ResetParametersChangedStatus();
        //    }
        //}

        internal static void FillMTFields(Response Res, Request Req)
        {
            //UI.MTResponseHeadersIDV.Text = Response.GetHeadersAsString();
            //if (Response.HasBody)
            //{
            //    if (Response.IsBinary)
            //    {
            //        UI.MTResponseBodyIDV.Text = Encoding.UTF8.GetString(Response.BodyArray);
            //        UI.MTResponseBodyIDV.ReadOnly = true;
            //    }
            //    else
            //    {
            //        UI.MTResponseBodyIDV.Text = Response.BodyString;
            //    }
            //}
            UI.TestResponseView.SetResponse(Res, Req);
            try
            {
                Request RedirectRequest = ManualTesting.GetRedirectRequestOnly();
                if (RedirectRequest == null)
                {
                    UI.MTFollowRedirectBtn.Visible = false;
                    UI.MTGetRedirectBtn.Visible = false;
                }
                else
                {
                    UI.MTFollowRedirectBtn.Visible = true;
                    UI.MTGetRedirectBtn.Visible = true;
                }
            }
            catch { }
            if (Res.SetCookies.Count > 0)
            {
                UI.TestUpdateCookieStoreLL.Visible = true;
            }
            else
            {
                UI.TestUpdateCookieStoreLL.Visible = false;
            }
        }

        
        internal static void ResetMTDisplayFields()
        {
            ResetMTRequestDisplayFields();
            ResetMTResponseDisplayFields();
            ResetMTExceptionFields();
        }

        internal static void ResetMTRequestDisplayFields()
        {
            UI.TestRequestView.ClearRequest();
        //    UI.MTRequestHeadersIDV.Text = "";
        //    UI.MTRequestBodyIDV.Text = "";
        //    UI.MTRequestParametersQueryGrid.Rows.Clear();
        //    UI.MTRequestParametersBodyGrid.Rows.Clear();
        //    UI.MTRequestParametersCookieGrid.Rows.Clear();
        //    UI.MTRequestParametersHeadersGrid.Rows.Clear();
        //    UI.MTRequestHeadersIDV.ReadOnly = false;
        //    UI.MTRequestBodyIDV.ReadOnly = false;
        //    UI.MTRequestFormatXMLTB.Text = "";
        //    UI.MTRequestFormatXMLTB.ReadOnly = true;
        }

        internal static void ResetMTResponseDisplayFields()
        {
            //UI.MTResponseHeadersIDV.Text = "";
            //UI.MTResponseBodyIDV.Text = "";
            //FillTestReflection("");
            UI.TestResponseView.ClearResponse();
            UI.MTFollowRedirectBtn.Visible = false;
            UI.MTGetRedirectBtn.Visible = false;
        }

        internal static void ResetMTExceptionFields()
        {
            UI.MTExceptionTB.Text = "";
            UI.MTExceptionTB.Visible = false;
        }

        internal static void ResetScriptedSendScriptExceptionFields()
        {
            UI.CustomSendErrorTB.Text = "";
            UI.CustomSendErrorTB.Visible = false;
            UI.CustomSendActivateCB.Checked = false;
        }

        internal static void ResetScriptedInterceptionScriptExceptionFields()
        {
            UI.ScriptedInterceptionErrorTB.Text = "";
            UI.ScriptedInterceptionErrorTB.Visible = false;
            UI.ScriptedInterceptionActivateScriptCB.Checked = false;
        }

        //delegate void FillMTRequestFormatXML_d(string XML);
        //internal static void FillMTRequestFormatXML(string XML)
        //{
        //    if (UI.MTRequestFormatXMLTB.InvokeRequired)
        //    {
        //        FillMTRequestFormatXML_d FMTRFX_d = new FillMTRequestFormatXML_d(FillMTRequestFormatXML);
        //        UI.Invoke(FMTRFX_d, new object[] { XML });
        //    }
        //    else
        //    {
        //        UI.MTRequestFormatXMLTB.Text = XML;
        //        UI.MTRequestFormatXMLTB.ReadOnly = false;
        //    }
        //}

        //delegate void FillMTRequestWithNewRequestFromFormatXML_d(Request Request, string PluginName);
        //internal static void FillMTRequestWithNewRequestFromFormatXML(Request Request, string PluginName)
        //{
        //    if (UI.MTRequestFormatXMLTB.InvokeRequired)
        //    {
        //        FillMTRequestWithNewRequestFromFormatXML_d FMTRWNRFFX_d = new FillMTRequestWithNewRequestFromFormatXML_d(FillMTRequestWithNewRequestFromFormatXML);
        //        UI.Invoke(FMTRWNRFFX_d, new object[] { Request, PluginName });
        //    }
        //    else
        //    {
        //        try
        //        {
        //            ResetMTRequestDisplayFields();
        //            FillMTFields(Request);
        //        }
        //        catch(Exception Exp)
        //        {
        //            IronException.Report("Error displaying Updated 'Manual Testing' Request from Serializing XML using Format Plugin - " + PluginName, Exp.Message, Exp.StackTrace);
        //            IronUI.ShowMTException("Error displaying updated Request");
        //        }
        //    }
        //}

        delegate void ShowMTException_d(string Message);
        internal static void ShowMTException(string Message)
        {
            if (UI.MTExceptionTB.InvokeRequired)
            {
                ShowMTException_d SMTE_d = new ShowMTException_d(ShowMTException);
                UI.Invoke(SMTE_d, new object[] { Message });
            }
            else
            {
                UI.MTExceptionTB.Text = Message;
                UI.MTExceptionTB.Visible = true;
            }
        }

        delegate void ShowScriptedSendScriptException_d(string Message);
        internal static void ShowScriptedSendScriptException(string Message)
        {
            if (UI.CustomSendErrorTB.InvokeRequired)
            {
                ShowScriptedSendScriptException_d SSSSE_d = new ShowScriptedSendScriptException_d(ShowScriptedSendScriptException);
                UI.Invoke(SSSSE_d, new object[] { Message });
            }
            else
            {
                UI.CustomSendErrorTB.Text = "Exception:\r\n" + Message;
                UI.CustomSendErrorTB.Visible = true;
                UI.CustomSendActivateCB.Checked = false;
            }
        }

        delegate void ShowScriptedInterceptionScriptException_d(string Message);
        internal static void ShowScriptedInterceptionScriptException(string Message)
        {
            if (UI.CustomSendErrorTB.InvokeRequired)
            {
                ShowScriptedInterceptionScriptException_d SSISE_d = new ShowScriptedInterceptionScriptException_d(ShowScriptedSendScriptException);
                UI.Invoke(SSISE_d, new object[] { Message });
            }
            else
            {
                UI.ScriptedInterceptionErrorTB.Text = "Exception:\r\n" + Message;
                UI.ScriptedInterceptionErrorTB.Visible = true;
                UI.ScriptedInterceptionActivateScriptCB.Checked = false;
            }
        }

        delegate void SetAutomatedScanningScanner_d(Scanner ScannerToSet, string XML, string[,] XmlInjectionPoints);
        internal static void SetAutomatedScanningScanner(Scanner ScannerToSet, string XML, string[,] XmlInjectionPoints)
        {
            if (UI.ASRequestTabs.InvokeRequired)
            {
                SetAutomatedScanningScanner_d SASS_d = new SetAutomatedScanningScanner_d(SetAutomatedScanningScanner);
                UI.Invoke(SASS_d, new object[] { ScannerToSet, XML, XmlInjectionPoints });
            }
            else
            {
                try
                {
                    Scanner.CurrentScanner = ScannerToSet;
                    Scanner.CurrentScanID = ScannerToSet.ID;
                    IronUI.FillConfigureScanFullFields(ScannerToSet.OriginalRequest);
                    UI.ASRequestTabs.SelectTab(0);
                    IronUI.UpdateScanTabsWithRequestData();
                    //if (ScannerToSet.BodyFormat.Name.Length > 0 && XmlInjectionPoints.Length > 0 && ScannerToSet.Status.Equals("Not Started"))
                    //    IronUI.FillConfigureScanFormatDetails(XML, XmlInjectionPoints, new List<bool>(), false, ScannerToSet.BodyFormat.Name);
                    UI.ScanIDLbl.Text = string.Format("Scan ID: {0}", ScannerToSet.ID);
                    UI.ScanStatusLbl.Text = string.Format("Scan Status: {0}", ScannerToSet.Status);
                    Scanner.ResetChangedStatus();
                    UI.ScanTopPanel.Visible = false;
                    UI.ScanJobsBaseSplit.SplitterDistance = 350;
                    UI.ScanDisplayPanel.Visible = true;
                }
                catch (Exception Exp)
                {
                    IronException.Report("Unable to display Request in 'Automated Scanning' section", Exp.Message, Exp.StackTrace);
                    IronUI.ShowConfigureScanException("Unable to display request");
                    return;
                }

                if (UI.ASScanPluginsGrid.Rows.Count > 0)
                {
                    UI.ASScanPluginsGrid.Rows[0].Cells[0].Value = false;
                    foreach (DataGridViewRow Row in UI.ASScanPluginsGrid.Rows)
                    {
                        if (Row.Index > 0)
                        {
                            Row.Cells[0].Value = ScannerToSet.ShowChecks().Contains(Row.Cells[1].Value.ToString());
                        }
                    }
                    if (UI.ASScanPluginsGrid.Rows.Count > 1)
                    {
                        bool AllSelected = true;
                        for (int i = 1; i < UI.ASScanPluginsGrid.Rows.Count; i++)
                        {
                            if (!(bool)UI.ASScanPluginsGrid.Rows[i].Cells[0].Value)
                            {
                                AllSelected = false;
                                break;
                            }
                        }
                        if (AllSelected) UI.ASScanPluginsGrid.Rows[0].Cells[0].Value = true;
                    }
                }

                if (ScannerToSet.SessionHandler.Name.Length > 0)
                    UI.SessionPluginLbl.Text = string.Format("Selection Plugin: {0}", ScannerToSet.SessionHandler.Name);
                else
                    UI.SessionPluginLbl.Text = "Selection Plugin: -";

                //UI.ASSessionPluginsCombo.Items.Add("");
                //int SelectedSessionPluginID = -1;
                //bool SelectedSessionPluginFound = false;
                //foreach (string Name in SessionPlugin.List())
                //{
                //    int ItemID = UI.ASSessionPluginsCombo.Items.Add(Name);
                //    if (!SelectedSessionPluginFound)
                //    {
                //        if (ScannerToSet.SessionHandler.Name.Equals(Name))
                //        {
                //            SelectedSessionPluginID = ItemID;
                //            SelectedSessionPluginFound = true;
                //        }
                //    }
                //}

                //if (SelectedSessionPluginID >= 0) UI.ASSessionPluginsCombo.SelectedIndex = SelectedSessionPluginID;
                try
                {
                    FillInjectionsPointsinUI(ScannerToSet);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error restoring 'Automated Scan' configuration information from DB", Exp.Message, Exp.StackTrace);
                    IronUI.ShowConfigureScanException("Error retriving scan information");
                }

                string ScanStatus = ScannerToSet.Status;
                if (ScanStatus.Equals("Completed"))
                {
                    UI.ASStartScanBtn.Text = "Scan Again";
                }
                else if (ScanStatus.Equals("Running"))
                {
                    UI.ASStartScanBtn.Text = "Stop Scan";
                }
                else if (ScanStatus.Equals("Not Started") || ScanStatus.Equals("Incomplete") || ScanStatus.Equals("Aborted") || ScanStatus.Equals("Stopped"))
                {
                    UI.ASStartScanBtn.Text = "Start Scan";
                }
                if (!UI.ASBaseTabs.SelectedTab.Name.Equals("ASRequestTab"))
                    UI.ASBaseTabs.SelectTab("ASRequestTab");
                UI.ASStartScanBtn.Enabled = true;
            }
        }

        static void FillInjectionsPointsinUI(Scanner Scanner)
        {
            bool AllUrl = UI.ASRequestScanURLGrid.Rows.Count > 0;
            foreach (DataGridViewRow Row in UI.ASRequestScanURLGrid.Rows)
            {
                bool Result = Scanner.URLInjections.Contains(Row.Index);
                if (AllUrl)
                {
                    AllUrl = Result;
                }
                Row.Cells[0].Value = Result;
            }

            int SubParameterIndex = 0;
            string LastParameterName = "";

            bool AllQuery = UI.ASRequestScanQueryGrid.Rows.Count > 0;
            foreach (DataGridViewRow Row in UI.ASRequestScanQueryGrid.Rows)
            {
                string Name = Row.Cells[1].Value.ToString();
                if (Name.Equals(LastParameterName))
                {
                    SubParameterIndex++;
                }
                else
                {
                    SubParameterIndex = 0;
                }
                bool Result = Scanner.QueryInjections.Has(Name) && Scanner.QueryInjections.GetAll(Name).Contains(SubParameterIndex);
                if (AllQuery)
                {
                    AllQuery = Result;
                }
                Row.Cells[0].Value = Result;
                LastParameterName = Name;
            }

            SubParameterIndex = 0;
            LastParameterName = "";

            bool AllBody = UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows.Count > 0;
            if (Scanner.BodyFormat.Name.Length > 0)
            {
                foreach (DataGridViewRow Row in UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows)
                {
                    bool Result = Scanner.BodyXmlInjections.Contains(Row.Index);
                    if (AllBody)
                    {
                        AllBody = Result;
                    }
                    Row.Cells[0].Value = Result;
                }
            }
            else if (Scanner.CustomInjectionPointStartMarker.Length > 0 && Scanner.CustomInjectionPointEndMarker.Length > 0)
            {
                AllBody = true;
            }
            else
            {
                foreach (DataGridViewRow Row in UI.ASRequestScanBodyTypeNormalGrid.Rows)
                {
                    string Name = Row.Cells[1].Value.ToString();
                    if (Name.Equals(LastParameterName))
                    {
                        SubParameterIndex++;
                    }
                    else
                    {
                        SubParameterIndex = 0;
                    }
                    bool Result = Scanner.BodyInjections.Has(Name) && Scanner.BodyInjections.GetAll(Name).Contains(SubParameterIndex);
                    if (AllBody)
                    {
                        AllBody = Result;
                    }
                    Row.Cells[0].Value = Result;
                    LastParameterName = Name;
                }
            }

            SubParameterIndex = 0;
            LastParameterName = "";

            bool AllCookie = UI.ASRequestScanCookieGrid.Rows.Count > 0;
            foreach (DataGridViewRow Row in UI.ASRequestScanCookieGrid.Rows)
            {
                string Name = Row.Cells[1].Value.ToString();
                if (Name.Equals(LastParameterName))
                {
                    SubParameterIndex++;
                }
                else
                {
                    SubParameterIndex = 0;
                }
                bool Result = Scanner.CookieInjections.Has(Name) && Scanner.CookieInjections.GetAll(Name).Contains(SubParameterIndex);
                if (AllCookie)
                {
                    AllCookie = Result;
                }
                Row.Cells[0].Value = Result;
                LastParameterName = Name;
            }

            SubParameterIndex = 0;
            LastParameterName = "";

            bool AllHeaders = UI.ASRequestScanHeadersGrid.Rows.Count > 0;
            foreach (DataGridViewRow Row in UI.ASRequestScanHeadersGrid.Rows)
            {
                string Name = Row.Cells[1].Value.ToString();
                if (Name.Equals(LastParameterName))
                {
                    SubParameterIndex++;
                }
                else
                {
                    SubParameterIndex = 0;
                }
                bool Result = Scanner.HeadersInjections.Has(Name) && Scanner.HeadersInjections.GetAll(Name).Contains(SubParameterIndex);
                if (AllHeaders)
                {
                    AllHeaders = Result;
                }
                Row.Cells[0].Value = Result;
                LastParameterName = Name;
            }

            bool AllNames = false;
            UI.ASRequestScanQueryParameterNameCB.Checked = Scanner.ParameterNameInjections.Has("Query");
            UI.ASRequestScanBodyParameterNameCB.Checked = Scanner.ParameterNameInjections.Has("Body");
            UI.ASRequestScanCookieParameterNameCB.Checked = Scanner.ParameterNameInjections.Has("Cookie");
            UI.ASRequestScanHeadersParameterNameCB.Checked = Scanner.ParameterNameInjections.Has("Headers");

            if (UI.ASRequestScanQueryParameterNameCB.Checked && UI.ASRequestScanBodyParameterNameCB.Checked && UI.ASRequestScanCookieParameterNameCB.Checked && UI.ASRequestScanHeadersParameterNameCB.Checked)
            {
                AllNames = true;
            }

            UI.ASRequestScanAllCB.Checked = AllUrl && AllQuery && AllBody && AllCookie && AllHeaders;
            UI.ASRequestScanURLCB.Checked = AllUrl;
            UI.ASRequestScanQueryCB.Checked = AllQuery;
            UI.ASRequestScanBodyCB.Checked = AllBody;
            UI.ASRequestScanCookieCB.Checked = AllCookie;
            UI.ASRequestScanHeadersCB.Checked = AllHeaders;
            UI.ASRequestScanParameterNamesCB.Checked = AllNames;
        }

        internal static void FillConfigureScanFullFields(Request Request)
        {
            UI.ConfigureScanRequestSSLCB.Checked = Request.SSL;
            UI.ASRequestRawHeadersIDV.Text = Request.GetHeadersAsStringWithoutFullURL();// .GetHeadersAsString();
            UI.ASRequestRawHeadersIDV.ReadOnly = false;
            if (Request.HasBody)
            {
                if (Request.IsBinary)
                {
                    UI.ASRequestRawBodyIDV.Text = Encoding.UTF8.GetString(Request.BodyArray);
                    UI.ASRequestRawBodyIDV.ReadOnly = true;
                }
                else
                {
                    UI.ASRequestRawBodyIDV.Text = Request.BodyString;
                    UI.ASRequestRawBodyIDV.ReadOnly = false;
                }
            }
            else
            {
                UI.ASRequestRawBodyIDV.Text = "";
                UI.ASRequestRawBodyIDV.ReadOnly = false;
            }
        }

        delegate void ShowConfigureScanException_d(string Message);
        internal static void ShowConfigureScanException(string Message)
        {
            if (UI.ASExceptionTB.InvokeRequired)
            {
                ShowConfigureScanException_d SCSE_d = new ShowConfigureScanException_d(ShowConfigureScanException);
                UI.Invoke(SCSE_d, new object[] { Message });
            }
            else
            {
                UI.ASExceptionTB.Text = Message;
                UI.ASExceptionTB.Visible = true;
            }
        }

        internal static void ResetConfigureScanException()
        {
            UI.ASExceptionTB.Text = "";
            UI.ASExceptionTB.Visible = false;
        }

        

        //internal static void HandleAnyChangesInConfigureScanRequest()
        //{
        //    if(Scanner.CurrentScanner != null)
        //        if (Scanner.CurrentScanner.OriginalRequest != null)  Scanner.CurrentScanner.OriginalRequest.SSL = UI.ConfigureScanRequestSSLCB.Checked;

        //    if (Scanner.RequestHeadersChanged)
        //    {
        //        if (Scanner.CurrentScanner == null)
        //        {
        //            if (UI.ASStartScanBtn.Text.Equals("Scan"))
        //            {
        //                try
        //                {
        //                    Scanner.CurrentScanner = new Scanner(new Request(UI.ASRequestRawHeadersIDV.Text.TrimEnd() + "\r\n\r\n", UI.ConfigureScanRequestSSLCB.Checked, false));
        //                    UpdateScanTabsWithRequestData();
        //                    Scanner.RequestHeadersChanged = false;
        //                }
        //                catch (Exception Exp)
        //                {
        //                    IronUI.ShowConfigureScanException(Exp.Message);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Scanner.CurrentScanner.ReloadRequestFromHeaderString(UI.ASRequestRawHeadersIDV.Text);
        //            UpdateScanTabsWithRequestData();
        //            Scanner.RequestHeadersChanged = false;
        //        }
        //    }
        //    if (Scanner.RequestBodyChanged)
        //    {
        //        if (Scanner.CurrentScanner != null)
        //        {
        //            Scanner.CurrentScanner.OriginalRequest.BodyString = UI.ASRequestRawBodyIDV.Text;
        //            Scanner.RequestBodyChanged = false;
        //            if (UI.ASBodyTypeCustomRB.Checked)
        //            {
        //                DetectAndHighLightCustomInjectionPoints();
        //            }
        //            else if (UI.ASBodyTypeFormatPluginRB.Checked)
        //            {
        //                Scanner.CurrentScanner.BodyFormat = new FormatPlugin();
        //                UpdateScanBodyTabWithXmlArray();
        //            }
        //            else
        //            {
        //                UpdateScanBodyTabWithDataInDefaultFormat();
        //            }
        //        }
        //    }
        //}

        internal static void UpdateScanTabsWithRequestData()
        {
            UI.ASRequestScanURLGrid.Rows.Clear();
            List<string> URLParts = Scanner.CurrentScanner.OriginalRequest.UrlPathParts;
            for (int i = 0; i < URLParts.Count; i++)
            {
                UI.ASRequestScanURLGrid.Rows.Add(new object[] { false, i + 1, URLParts[i] });
            }
            UI.ASRequestScanQueryGrid.Rows.Clear();
            foreach (string Name in Scanner.CurrentScanner.OriginalRequest.Query.GetNames())
            {
                foreach (string Value in Scanner.CurrentScanner.OriginalRequest.Query.GetAll(Name))
                {
                    UI.ASRequestScanQueryGrid.Rows.Add(new object[] { false, Name, Value });
                }
            }

            if (Scanner.CurrentScanner.CustomInjectionPointStartMarker.Length > 0 && Scanner.CurrentScanner.CustomInjectionPointEndMarker.Length > 0)
            {
                UpdateScanBodyTabWithCustomInjectionMarkers();
            }
            else if (Scanner.CurrentScanner.BodyFormat.Name.Length > 0)
            {
                UpdateScanBodyTabWithXmlArray();
            }
            else
            {
                UpdateScanBodyTabWithDataInDefaultFormat();
            }

            UI.ASRequestScanCookieGrid.Rows.Clear();
            foreach (string Name in Scanner.CurrentScanner.OriginalRequest.Cookie.GetNames())
            {
                foreach (string Value in Scanner.CurrentScanner.OriginalRequest.Cookie.GetAll(Name))
                {
                    UI.ASRequestScanCookieGrid.Rows.Add(new object[] { false, Name, Value });
                }
            }
            UI.ASRequestScanHeadersGrid.Rows.Clear();
            foreach (string Name in Scanner.CurrentScanner.OriginalRequest.Headers.GetNames())
            {
                if (!(Name.Equals("Cookie", StringComparison.InvariantCultureIgnoreCase) || Name.Equals("Host", StringComparison.InvariantCultureIgnoreCase) || Name.Equals("Content-Length", StringComparison.InvariantCultureIgnoreCase)))
                {
                    foreach (string Value in Scanner.CurrentScanner.OriginalRequest.Headers.GetAll(Name))
                    {
                        UI.ASRequestScanHeadersGrid.Rows.Add(new object[] { false, Name, Value });
                    }
                }
            }
        }

        delegate void UpdateScanBodyTabWithDataInDefaultFormat_d();
        internal static void UpdateScanBodyTabWithDataInDefaultFormat()
        {
            if (UI.ConfigureScanRequestBodyTypeFormatPluginGrid.InvokeRequired)
            {
                UpdateScanBodyTabWithDataInDefaultFormat_d USBTWDF_d = new UpdateScanBodyTabWithDataInDefaultFormat_d(UpdateScanBodyTabWithDataInDefaultFormat);
                UI.Invoke(USBTWDF_d, new object[] { });
            }
            else
            {
                UI.ASRequestScanAllCB.Checked = false;
                UI.ASRequestScanBodyCB.Checked = false;
                
                if (Scanner.CurrentScanner == null) return;
                if (Scanner.CurrentScanner.OriginalRequest == null) return;
                
                Scanner.CurrentScanner.BodyFormat = new FormatPlugin();
                Scanner.CurrentScanner.CustomInjectionPointStartMarker = "";
                Scanner.CurrentScanner.CustomInjectionPointEndMarker = "";

                Scanner.CurrentScannerBodyFormatTabIndex = 0;
                if (!UI.ASBodyInjectTypeTabs.SelectedTab.Name.Equals("ASBodyTypeNormalTab")) UI.ASBodyInjectTypeTabs.SelectTab("ASBodyTypeNormalTab");

                UI.ASRequestScanBodyTypeNormalGrid.Rows.Clear();
                foreach (string Name in Scanner.CurrentScanner.OriginalRequest.Body.GetNames())
                {
                    foreach (string Value in Scanner.CurrentScanner.OriginalRequest.Body.GetAll(Name))
                    {
                        UI.ASRequestScanBodyTypeNormalGrid.Rows.Add(new object[] { false, Name, Value });
                    }
                }
            }
        }

        delegate void UpdateScanBodyTabWithXmlArray_d();
        internal static void UpdateScanBodyTabWithXmlArray()
        {
            if (UI.ConfigureScanRequestBodyTypeFormatPluginGrid.InvokeRequired)
            {
                UpdateScanBodyTabWithXmlArray_d USBTWXA_d = new UpdateScanBodyTabWithXmlArray_d(UpdateScanBodyTabWithXmlArray);
                UI.Invoke(USBTWXA_d, new object[] { });
            }
            else
            {
                UI.ASRequestScanAllCB.Checked = false;
                UI.ASRequestScanBodyCB.Checked = false;

                if (Scanner.CurrentScanner == null) return;
                if (Scanner.CurrentScanner.OriginalRequest == null) return;

                Scanner.CurrentScannerBodyFormatTabIndex = 1;
                if (!UI.ASBodyInjectTypeTabs.SelectedTab.Name.Equals("ASBodyTypeFormatPluginTab")) UI.ASBodyInjectTypeTabs.SelectTab("ASBodyTypeFormatPluginTab");

                foreach (DataGridViewRow Row in UI.ConfigureScanRequestFormatPluginsGrid.Rows)
                {
                    if (Row.Cells[1].Value.ToString().Equals(Scanner.CurrentScanner.BodyFormat.Name))
                        Row.Cells[0].Value = true;
                    else
                        Row.Cells[0].Value = false;
                }

                UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows.Clear();
                UI.ConfigureScanRequestFormatXMLTB.Text = "";

                foreach (string Name in Scanner.CurrentScanner.BodyXmlInjectionParameters.GetNames())
                {
                    UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows.Add(new object[] { false, Name, Scanner.CurrentScanner.BodyXmlInjectionParameters.Get(Name) });
                }
            }
        }

        internal static void UpdateScanBodyTabWithCustomInjectionMarkers()
        {
            UI.ASRequestScanAllCB.Checked = false;
            UI.ASRequestScanBodyCB.Checked = false;

            if (Scanner.CurrentScanner == null) return;
            if (Scanner.CurrentScanner.OriginalRequest == null) return;

            Scanner.CurrentScannerBodyFormatTabIndex = 2;
            if (!UI.ASBodyInjectTypeTabs.SelectedTab.Name.Equals("ASBodyTypeCustomTab")) UI.ASBodyInjectTypeTabs.SelectTab("ASBodyTypeCustomTab");

            if (Scanner.CurrentScanner.CustomInjectionPointStartMarker.Length > 0)
                UI.ASCustomStartMarkerTB.Text = Scanner.CurrentScanner.CustomInjectionPointStartMarker;
            else
                UI.ASCustomStartMarkerTB.Text = Scanner.DefaultStartMarker;

            if (Scanner.CurrentScanner.CustomInjectionPointEndMarker.Length > 0)
                UI.ASCustomEndMarkerTB.Text = Scanner.CurrentScanner.CustomInjectionPointEndMarker;
            else
                UI.ASCustomEndMarkerTB.Text = Scanner.DefaultEndMarker;

            DetectAndHighLightCustomInjectionPoints();
        }

        internal static void DetectAndHighLightCustomInjectionPoints()
        {
            if (Scanner.CurrentScanner == null) return;
            if (Scanner.CurrentScanner.OriginalRequest == null) return;

            UI.ASRequestCustomInjectionPointsHighlightTB.Text = Scanner.CurrentScanner.OriginalRequest.BodyString;
            string StartMarker = Scanner.CurrentScanner.CustomInjectionPointStartMarker;
            string EndMarker = Scanner.CurrentScanner.CustomInjectionPointEndMarker;
            if (StartMarker.Length == 0 || EndMarker.Length == 0 || StartMarker.Equals(EndMarker))
            {
                return;
            }
            int SSI = UI.ASRequestCustomInjectionPointsHighlightTB.SelectionStart;
            int SSL = UI.ASRequestCustomInjectionPointsHighlightTB.SelectionLength;
            string TempText = UI.ASRequestCustomInjectionPointsHighlightTB.Text;
            UI.ASRequestCustomInjectionPointsHighlightTB.Text = TempText;//this is done to clear all previous selection highlighting
            bool CheckFurther = true;
            int Pointer = 0;
            string Content = UI.ASRequestCustomInjectionPointsHighlightTB.Text;
            int MatchCount = 0;
            while (CheckFurther && Content.Length > Pointer)
            {
                int Start = Content.IndexOf(StartMarker, Pointer);
                int Stop = -1;
                if (Content.Length >= (Start + StartMarker.Length))
                    Stop = Content.IndexOf(EndMarker, Start + StartMarker.Length);
                if (Start == -1 || Stop == -1) CheckFurther = false;
                if (CheckFurther)
                {
                    UI.ASRequestCustomInjectionPointsHighlightTB.SelectionStart = Start;
                    UI.ASRequestCustomInjectionPointsHighlightTB.SelectionLength = (Stop + EndMarker.Length) - Start;
                    UI.ASRequestCustomInjectionPointsHighlightTB.SelectionBackColor = Color.Orange;
                    MatchCount++;
                }
                Pointer = Stop + EndMarker.Length;
            }
            UI.ASRequestCustomInjectionPointsHighlightTB.SelectionStart = SSI;
            UI.ASRequestCustomInjectionPointsHighlightTB.SelectionLength = SSL;

            UI.ASRequestCustomInjectionPointsHighlightLbl.Text = string.Format("No. of Injection Points: {0}", MatchCount);
            if (MatchCount > 0)
                UI.ASRequestScanBodyCB.Checked = true;
            else
                UI.ASRequestScanBodyCB.Checked = false;
        }

        delegate void ResetConfigureScanFields_d();
        internal static void ResetConfigureScanFields()
        {
            if (UI.ASRequestScanAllCB.InvokeRequired)
            {
                ResetConfigureScanFields_d RCSF_d = new ResetConfigureScanFields_d(ResetConfigureScanFields);
                UI.Invoke(RCSF_d, new object[] { });
            }
            else
            {
                UI.ASRequestScanAllCB.Checked = false;
                UI.ASRequestScanURLCB.Checked = false;
                UI.ASRequestScanQueryCB.Checked = false;
                UI.ASRequestScanBodyCB.Checked = false;
                UI.ASRequestScanCookieCB.Checked = false;
                UI.ASRequestScanHeadersCB.Checked = false;
                UI.ASRequestScanURLGrid.Rows.Clear();
                UI.ASRequestScanQueryGrid.Rows.Clear();
                UI.ASRequestScanBodyTypeNormalGrid.Rows.Clear();
                UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows.Clear();
                UI.ASRequestCustomInjectionPointsHighlightTB.Text = "";
                UI.ASRequestCustomInjectionPointsHighlightLbl.Text = "Number of Injection Points Detected: 0";
                UI.ASRequestScanCookieGrid.Rows.Clear();
                UI.ASRequestScanHeadersGrid.Rows.Clear();
                //UI.ASSessionPluginsCombo.Items.Clear();
                //UI.ASSessionPluginsCombo.Text = "";
                UI.SessionPluginLbl.Text = "Session Plugin: ";
                UI.ASRequestRawHeadersIDV.Text = "";
                UI.ASRequestRawBodyIDV.Text = "";
                UI.ConfigureScanRequestSSLCB.Checked = false;
                UI.ScanIDLbl.Text = "Scan ID:";
                UI.ScanStatusLbl.Text = "Scan Status:";
                ResetConfigureScanException();
            }
        }

        delegate void FillConfigureScanFormatDetails_d(string XML, string[,] InjectionArray, List<bool> CheckStatus, bool CheckAll, string PluginName);
        internal static void FillConfigureScanFormatDetails(string XML, string[,] InjectionArray, List<bool> CheckStatus, bool CheckAll, string PluginName)
        {
            if (UI.ConfigureScanRequestBodyTypeFormatPluginGrid.InvokeRequired)
            {
                FillConfigureScanFormatDetails_d FCSFD_d = new FillConfigureScanFormatDetails_d(FillConfigureScanFormatDetails);
                UI.Invoke(FCSFD_d, new object[] { XML, InjectionArray, CheckStatus, CheckAll, PluginName });
            }
            else
            {
                UI.ConfigureScanRequestFormatXMLTB.Text = XML;
                if (CheckStatus.Count != InjectionArray.GetLength(0))
                {
                    CheckStatus.Clear();
                    for (int i = 0; i < InjectionArray.GetLength(0); i++)
                    {
                        CheckStatus.Add(CheckAll);
                    }
                }
                //UI.ASRequestTabs.SelectTab("ASRequestBodyTab");
                UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows.Clear();
                Parameters BodyXmlInjectionParameters = new Parameters();
                for (int i = 0; i < InjectionArray.GetLength(0); i++)
                {
                    UI.ConfigureScanRequestBodyTypeFormatPluginGrid.Rows.Add(new object[] { CheckStatus[i], InjectionArray[i, 0], InjectionArray[i, 1] });
                    BodyXmlInjectionParameters.Add(InjectionArray[i, 0], InjectionArray[i, 1]);
                }
                Scanner.CurrentScanner.BodyXmlInjectionParameters = BodyXmlInjectionParameters;
                UI.ASRequestScanBodyCB.Checked = CheckAll;
                foreach (DataGridViewRow Row in UI.ConfigureScanRequestFormatPluginsGrid.Rows)
                {
                    if (Row.Cells[1].Value.ToString().Equals(PluginName))
                        Row.Cells[0].Value = true;
                    else
                        Row.Cells[0].Value = false;
                }
            }
        }

        delegate void UpdateResultsTab_d(Finding PR);
        internal static void UpdateResultsTab(Finding PR)
        {
            if (UI.ResultsDisplayRTB.InvokeRequired)
            {
                UpdateResultsTab_d URT_d = new UpdateResultsTab_d(UpdateResultsTab);
                UI.Invoke(URT_d, new object[] { PR });
            }
            else
            {
                ResetPluginResultsTab();
                StringBuilder SB = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;\red247\green150\blue70;\red255\green0\blue0;\red0\green200\blue50;}");
                SB.Append(@" \b \fs30"); SB.Append(Tools.RtfSafe(PR.Title)); SB.Append(@"\b0  \fs20  \par  \par");
                SB.Append(@" \cf1 \b Finding ID: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PR.Id.ToString())); SB.Append(@" \par");
                if (PR.Type == FindingType.Vulnerability)
                {
                    SB.Append(@" \par \cf1 \b Severity: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PR.Severity.ToString())); SB.Append(@" \par");
                    SB.Append(@" \cf1 \b Confidence: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PR.Confidence.ToString())); SB.Append(@" \par  \par");
                }
                switch (PR.FinderType)
                {
                    case("ActivePlugin"):
                        SB.Append(@" \cf1 \b Discovery Mode: \b0 \cf0 Automated Vulnerability Scan \par");
                        SB.Append(@" \cf1 \b Scan ID: \b0 \cf0 "); SB.AppendLine(PR.ScanId.ToString()); SB.Append(@" \par");
                        SB.Append(@" \cf1 \b Vulnerability Check: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PR.FinderName)); SB.Append(@" \par");
                        break;
                    case ("PassivePlugin"):
                        SB.Append(@" \cf1 \b Discovery Mode: \b0 \cf0 Passive Traffic Analysis \par");
                        SB.Append(@" \cf1 \b Passive Plugin Name: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PR.FinderName)); SB.Append(@" \par");
                        break;
                    default:
                        if (PR.FinderType.Length > 0 || PR.FinderName.Length > 0)
                        {
                            SB.Append(@" \cf1 \b Discovered By: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(string.Format("{0} {1}",PR.FinderName, PR.FinderType))); SB.Append(@" \par");
                        }
                        break;
                }
                SB.Append(@" \par");
                SB.Append(@" \cf1 \b Summary: \b0 \cf0  \par ");
                SB.AppendLine(Tools.RtfSafe(PR.Summary));
                SB.Append(@" \par ");
                switch (PR.FinderType)
                {
                    case("ActivePlugin"):
                        StringBuilder ASB = new StringBuilder();
                        if (PR.Reasons.Count > 0)
                        {
                            ASB.Append("<i<br>>IronWASP has reported this issue because of the following reasons:<i<br>>");
                            for (int i = 0; i < PR.Reasons.Count; i++)
                            {
                                ASB.Append(string.Format("<i<br>><i<b>><i<cb>>Reason {0}:<i</b>><i</cb>> <i<br>>", i + 1));
                                ASB.Append(PR.Reasons[i].Reason);

                                if (PR.Reasons[i].TriggerIds.Count > 0)
                                {
                                    ASB.Append("<i<br>><i<br>>The request and response associated with this check can be seen by clicking on ");
                                    if (PR.Reasons[i].TriggerIds.Count == 1)
                                    {
                                        ASB.Append("Trigger "); ASB.Append(PR.Reasons[i].TriggerIds[0]);
                                    }
                                    else
                                    {
                                        ASB.Append("Triggers ");
                                        for(int ii=0; ii < PR.Reasons[i].TriggerIds.Count; ii++)
                                        {
                                            if (ii == 0)
                                            {
                                                ASB.Append(PR.Reasons[i].TriggerIds[ii]);
                                            }
                                            else if (ii == PR.Reasons[i].TriggerIds.Count - 1)
                                            {
                                                ASB.Append(" and "); ASB.Append(PR.Reasons[i].TriggerIds[ii]);
                                            }
                                            else
                                            {
                                                ASB.Append(", "); ASB.Append(PR.Reasons[i].TriggerIds[ii]);
                                            }
                                        }
                                    }
                                    ASB.Append("<i<br>>The 'Trigger Analysis Tools' section has tools to compare the Request/Response of two different triggers or one trigger and the normal Request/Response.");
                                    ASB.Append("<i<br>>Doing a right-click on a Trigger id will show a menu with options to resend selected request or to send it after editing. Click on the 'Select this Request for Manual Testing' option in that menu for this feature.");
                                }
                                if (PR.Reasons[i].FalsePositiveCheck.Length > 0)
                                {
                                    ASB.Append("<i<br>><i<br>><i<cg>><i<b>>False Positive Check Assistance:<i</b>><i</cg>><i<br>>");
                                    ASB.Append(PR.Reasons[i].FalsePositiveCheck);
                                    ASB.Append("<i<br>>");
                                }
                            }
                        }
                        ASB.Append("<i<br>><i<hh>>More Scan Information:<i</hh>><i<br>>");
                        ASB.Append("To view all the payloads, requests/responses and detailed scan trace information associated with this vulnerability detection scan, please click on the associated button in the <i<b>>Trigger Analysis Tools<i</b>> section below.<i<br>>");
                        SB.Append(Tools.RtfSafe(ASB.ToString()));
                        break;
                    case("PassivePlugin"):
                        Request AHR = new Request(PR.AffectedHost);
                        StringBuilder PSB = new StringBuilder();
                        PSB.Append("<i<br>><i<br>><i<b>><i<co>>NOTE:<i</b>><i</co>><i<br>>");
                        PSB.Append(string.Format("There might be more instances of this issue in <i<cb>>{0}<i</cb>> but they have not been reported to avoid creating a lot of noise.", AHR.Host));
                        PSB.Append(string.Format("<i<br>><i<br>>In order to identify every single instance of this issue on <i<cb>>{0}<i</cb>> follow these steps:", AHR.Host));
                        PSB.Append("<i<br>>1) Go to the 'Logs' section in IronWASP and click on the 'Search and Analyze Logs' button");
                        PSB.Append("<i<br>>2) Select the logs on which you want to do the analysis. Proxy logs would be selected by default.");
                        PSB.Append("<i<br>>3) Click the checkbox before the Hostname filter option and then click on the '+' radio button to set this filter option as a white-list.");
                        PSB.Append(string.Format("<i<br>>4) Now enter the hostname <i<cb>>{0}<i</cb>> in to the textbox field in this area. Donot add http:// or https:// or / here.", AHR.Host));
                        PSB.Append("<i<br>>5) If required you can configure some of the other options to set the search filter and then click on the 'Search with this Filter' button to perform a search.");
                        PSB.Append("<i<br>>6) Click on the 'Select all rows' checkbox in the search results section and then click on the 'Test Selected Sessions' button.");
                        PSB.Append("<i<br>>7) A new window wil appear, select the 'Run Passive Plugins on Selected Sessions' option here and click the 'Next Step' button.");
                        PSB.Append(string.Format("<i<br>>8) You will be shown a list of Passive Plugins, select the one named <i<cb>>{0}<i</cb>> and click the 'Start Test' button", PR.FinderName));
                        PSB.Append("<i<br>>9) Now you will be shown all instances of this issue present in the logs matching your search filter.");
                        SB.Append(Tools.RtfSafe(PSB.ToString()));
                        break;

                }
                UI.ResultsDisplayRTB.Rtf = SB.ToString();

                if (PR.FromActiveScan && PR.BaseRequest != null && PR.BaseResponse != null)
                {
                    UI.ResultsTriggersGrid.Rows.Add(new object[] { "Normal" });
                    UI.SelectForDiffTriggersGrid.Rows.Add(new object[] { false, "Normal" });
                }
                for (int i=0; i < PR.Triggers.GetTriggers().Count; i++ )
                {
                    UI.ResultsTriggersGrid.Rows.Add(new object[] { string.Format("Trigger {0}", i + 1) });
                    UI.SelectForDiffTriggersGrid.Rows.Add(new object[] { false, string.Format("Trigger {0}", i + 1) });
                }
                if (UI.ResultsTriggersGrid.Rows.Count > 0)
                {
                    if (PR.FromActiveScan && UI.ResultsTriggersGrid.Rows.Count == 2)
                    {
                        UI.ResultsTriggersGrid.Rows[1].Selected = false;
                        UI.ResultsTriggersGrid.Rows[1].Selected = true;
                    }
                    else
                    {
                        UI.ResultsTriggersGrid.Rows[0].Selected = false;
                        UI.ResultsTriggersGrid.Rows[0].Selected = true;
                    }
                    //DisplayPluginResultsTrigger(0);
                }
                if (!UI.main_tab.SelectedTab.Name.Equals("mt_results")) UI.main_tab.SelectTab("mt_results");
            }
        }

        internal static void ResetPluginResultsTab()
        {
            UI.ResultsDisplayRTB.Text = "";
            UI.ResultsTriggersGrid.Rows.Clear();
            UI.SelectForDiffTriggersGrid.Rows.Clear();
            ResetPluginResultsFields();
        }

        delegate void UpdateResultsTabWithException_d(IronException IrEx);
        internal static void UpdateResultsTab(IronException IrEx)
        {
            if (UI.ResultsDisplayRTB.InvokeRequired)
            {
                UpdateResultsTabWithException_d URTWE_d = new UpdateResultsTabWithException_d(UpdateResultsTab);
                UI.Invoke(URTWE_d, new object[] { IrEx });
            }
            else
            {
                ResetPluginResultsTab();
                StringBuilder SB = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;}");
                SB.Append(@" \b \fs30 "); SB.Append(Tools.RtfSafe(IrEx.Title)); SB.Append(@"\b0 ");
                SB.Append(@" \par"); SB.Append(@" \par"); SB.Append(@" \fs20 ");
                SB.AppendLine(Tools.RtfSafe(IrEx.Message)); SB.Append(@" \par \par ");
                SB.Append(@" \cf1 \b StackTrace: \b0 \cf0 "); SB.Append(@" \par "); SB.AppendLine(Tools.RtfSafe(IrEx.StackTrace.Replace("\r\n","<i<br>>")));
                SB.Append(@" \par "); SB.Append(@" \par ");
                UI.ResultsDisplayRTB.Rtf = SB.ToString();
                UI.ResultsTriggersGrid.Rows.Clear();
                if (!UI.main_tab.SelectedTab.Name.Equals("mt_results")) UI.main_tab.SelectTab("mt_results");
            }
        }

        //delegate void UpdateResultsTabWithSiteMapLog_d(string Host, string Url);
        //internal static void UpdateResultsTab(string Host, string Url)
        delegate void UpdateResultsTabWithSiteMapLog_d(Request UrlReq);
        internal static void UpdateResultsTab(Request UrlReq)
        {
            if (UI.ResultsDisplayRTB.InvokeRequired)
            {
                UpdateResultsTabWithSiteMapLog_d URTWSML_d = new UpdateResultsTabWithSiteMapLog_d(UpdateResultsTab);
                UI.Invoke(URTWSML_d, new object[] { UrlReq });
            }
            else
            {
                ResetPluginResultsTab();
                UI.SiteMapLogGrid.Visible = false;
                UI.SiteMapLogGrid.Rows.Clear();
                foreach (DataGridViewRow Row in UI.ProxyLogGrid.Rows)
                {
                    if ((bool)Row.Cells["ProxyLogGridColumnForSSL"].Value != UrlReq.SSL) continue;
                    if (!Row.Cells["ProxyLogGridColumnForHostName"].Value.ToString().Equals(UrlReq.Host)) continue;
                    if (!Row.Cells["ProxyLogGridColumnForURL"].Value.ToString().StartsWith(UrlReq.Url)) continue;

                    UI.SiteMapLogGrid.Rows.Add(new object[] { Row.Cells["ProxyLogGridColumnForID"].Value, "Proxy", Row.Cells["ProxyLogGridColumnForHostName"].Value, Row.Cells["ProxyLogGridColumnForMethod"].Value, Row.Cells["ProxyLogGridColumnForURL"].Value, Row.Cells["ProxyLogGridColumnForFile"].Value, Row.Cells["ProxyLogGridColumnForSSL"].Value, Row.Cells["ProxyLogGridColumnForParameters"].Value, Row.Cells["ProxyLogGridColumnForCode"].Value, Row.Cells["ProxyLogGridColumnForLength"].Value, Row.Cells["ProxyLogGridColumnForMIME"].Value, Row.Cells["ProxyLogGridColumnForSetCookie"].Value });
                }
                foreach (DataGridViewRow Row in UI.TestLogGrid.Rows)
                {
                    if ((bool)Row.Cells["MTLogGridColumnForSSL"].Value != UrlReq.SSL) continue;
                    if (!Row.Cells["MTLogGridColumnForHostName"].Value.ToString().Equals(UrlReq.Host)) continue;
                    if (!Row.Cells["MTLogGridColumnForURL"].Value.ToString().StartsWith(UrlReq.Url)) continue;

                    UI.SiteMapLogGrid.Rows.Add(new object[] { Row.Cells["MTLogGridColumnForID"].Value, "Test", Row.Cells["MTLogGridColumnForHostName"].Value, Row.Cells["MTLogGridColumnForMethod"].Value, Row.Cells["MTLogGridColumnForURL"].Value, Row.Cells["MTLogGridColumnForFile"].Value, Row.Cells["MTLogGridColumnForSSL"].Value, Row.Cells["MTLogGridColumnForParameters"].Value, Row.Cells["MTLogGridColumnForCode"].Value, Row.Cells["MTLogGridColumnForLength"].Value, Row.Cells["MTLogGridColumnForMIME"].Value, Row.Cells["MTLogGridColumnForSetCookie"].Value });
                }
                foreach (DataGridViewRow Row in UI.ShellLogGrid.Rows)
                {
                    if ((bool)Row.Cells["ScriptingLogGridColumnForSSL"].Value != UrlReq.SSL) continue;
                    if (!Row.Cells["ScriptingLogGridColumnForHostName"].Value.ToString().Equals(UrlReq.Host)) continue;
                    if (!Row.Cells["ScriptingLogGridColumnForURL"].Value.ToString().StartsWith(UrlReq.Url)) continue;

                    UI.SiteMapLogGrid.Rows.Add(new object[] { Row.Cells["ScriptingLogGridColumnForID"].Value, "Shell", Row.Cells["ScriptingLogGridColumnForHostName"].Value, Row.Cells["ScriptingLogGridColumnForMethod"].Value, Row.Cells["ScriptingLogGridColumnForURL"].Value, Row.Cells["ScriptingLogGridColumnForFile"].Value, Row.Cells["ScriptingLogGridColumnForSSL"].Value, Row.Cells["ScriptingLogGridColumnForParameters"].Value, Row.Cells["ScriptingLogGridColumnForCode"].Value, Row.Cells["ScriptingLogGridColumnForLength"].Value, Row.Cells["ScriptingLogGridColumnForMIME"].Value, Row.Cells["ScriptingLogGridColumnForSetCookie"].Value });
                }
                foreach (DataGridViewRow Row in UI.ProbeLogGrid.Rows)
                {
                    if ((bool)Row.Cells["ProbeLogGridColumnForSSL"].Value != UrlReq.SSL) continue;
                    if (!Row.Cells["ProbeLogGridColumnForHostName"].Value.ToString().Equals(UrlReq.Host)) continue;
                    if (!Row.Cells["ProbeLogGridColumnForURL"].Value.ToString().StartsWith(UrlReq.Url)) continue;

                    UI.SiteMapLogGrid.Rows.Add(new object[] { Row.Cells["ProbeLogGridColumnForID"].Value, "Probe", Row.Cells["ProbeLogGridColumnForHostName"].Value, Row.Cells["ProbeLogGridColumnForMethod"].Value, Row.Cells["ProbeLogGridColumnForURL"].Value, Row.Cells["ProbeLogGridColumnForFile"].Value, Row.Cells["ProbeLogGridColumnForSSL"].Value, Row.Cells["ProbeLogGridColumnForParameters"].Value, Row.Cells["ProbeLogGridColumnForCode"].Value, Row.Cells["ProbeLogGridColumnForLength"].Value, Row.Cells["ProbeLogGridColumnForMIME"].Value, Row.Cells["ProbeLogGridColumnForSetCookie"].Value });
                }
                foreach (DataGridViewRow Row in UI.ScanLogGrid.Rows)
                {
                    if ((bool)Row.Cells["ScanLogGridColumnForSSL"].Value != UrlReq.SSL) continue;
                    if (!Row.Cells["ScanLogGridColumnForHost"].Value.ToString().Equals(UrlReq.Host)) continue;
                    if (!Row.Cells["ScanLogGridColumnForURL"].Value.ToString().StartsWith(UrlReq.Url)) continue;

                    UI.SiteMapLogGrid.Rows.Add(new object[] { Row.Cells["ScanLogGridColumnForID"].Value, "Scan", Row.Cells["ScanLogGridColumnForHost"].Value, Row.Cells["ScanLogGridColumnForMethod"].Value, Row.Cells["ScanLogGridColumnForURL"].Value, Row.Cells["ScanLogGridColumnForFile"].Value, Row.Cells["ScanLogGridColumnForSSL"].Value, Row.Cells["ScanLogGridColumnForParameters"].Value, Row.Cells["ScanLogGridColumnForCode"].Value, Row.Cells["ScanLogGridColumnForLength"].Value, Row.Cells["ScanLogGridColumnForMIME"].Value, Row.Cells["ScanLogGridColumnForSetCookie"].Value });
                }
                if (!UI.main_tab.SelectedTab.Name.Equals("mt_logs")) UI.main_tab.SelectTab("mt_logs");
                if (!UI.LogTabs.SelectedTab.Name.Equals("SiteMapLogTab")) UI.LogTabs.SelectTab("SiteMapLogTab");
                IronUI.LogGridStatus(true);
                ResetPluginResultsTab();
                UI.SiteMapLogGrid.Visible = true;
            }
        }

        internal static void DisplayPluginDetails(string[] PluginDetails)
        {
            string Language = "Python";
            if (PluginDetails[4].Equals("Ruby")) Language = "Ruby";
            StringBuilder SB = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;}");
            SB.Append(@" \b \fs30"); SB.Append(Tools.RtfSafe(PluginDetails[0])); SB.Append(@"\b0 ");
            SB.Append(@" \par \par \fs20");
            SB.Append(@" \cf1 \b Language: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PluginDetails[4])); SB.Append(@" \par");
            SB.Append(@" \cf1 \b File: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PluginDetails[2])); SB.Append(@" \par");
            SB.Append(@" \par");
            SB.Append(@" \cf1 \b Description: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(PluginDetails[1])); SB.Append(@" \par");
            SB.Append(@" \par");
            UI.PluginDetailsRTB.Rtf = SB.ToString();
            UI.PluginEditorInTE.Text = PluginDetails[3];
            Directory.SetCurrentDirectory(Config.RootDir);
            UI.PluginEditorInTE.SetHighlighting(Language);
            UI.PluginEditorInTE.Refresh();
        }

        internal static void DisplayModuleDetails(string[] ModuleDetails)
        {
            string Language = "Python";
            if (ModuleDetails[4].Equals("Ruby")) Language = "Ruby";
            StringBuilder SB = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;}");
            SB.Append(@" \b \fs30"); SB.Append(Tools.RtfSafe(ModuleDetails[5])); SB.Append(@"\b0 ");
            SB.Append(@" \par \par \fs20");
            SB.Append(@" \cf1 \b Name: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(ModuleDetails[0])); SB.Append(@" \par");
            SB.Append(@" \cf1 \b Language: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(ModuleDetails[4])); SB.Append(@" \par");
            SB.Append(@" \cf1 \b File: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(ModuleDetails[2])); SB.Append(@" \par");
            SB.Append(@" \par");
            SB.Append(@" \cf1 \b Description: \b0 \cf0 "); SB.AppendLine(Tools.RtfSafe(ModuleDetails[1])); SB.Append(@" \par");
            SB.Append(@" \par");
            UI.PluginDetailsRTB.Rtf = SB.ToString();
            UI.PluginEditorInTE.Text = ModuleDetails[3];
            Directory.SetCurrentDirectory(Config.RootDir);
            UI.PluginEditorInTE.SetHighlighting(Language);
            UI.PluginEditorInTE.Refresh();
        }

        delegate void FreezeInteractiveShellUI_d();
        internal static void FreezeInteractiveShellUI()
        {
            if (UI.InteractiveShellOut.InvokeRequired)
            {
                FreezeInteractiveShellUI_d FISU_d = new FreezeInteractiveShellUI_d(FreezeInteractiveShellUI);
                UI.Invoke(FISU_d, new object[] { });
            }
            else
            {
                UI.InteractiveShellCtrlCBtn.Enabled = true;
                UI.MultiLineShellExecuteBtn.Enabled = false;
                UI.InteractiveShellPromptBox.BackColor = Color.DimGray;
                UI.InteractiveShellIn.BackColor = Color.DimGray;
                UI.InteractiveShellIn.ReadOnly = true;
                //
                UI.InteractiveShellPythonRB.Enabled = false;
                UI.InteractiveShellRubyRB.Enabled = false;
            }
        }

        delegate void ActivateinteractiveShellUI_d();
        internal static void ActivateinteractiveShellUI()
        {
            if (UI.InteractiveShellOut.InvokeRequired)
            {
                ActivateinteractiveShellUI_d AISU_d = new ActivateinteractiveShellUI_d(ActivateinteractiveShellUI);
                UI.Invoke(AISU_d, new object[] { });
            }
            else
            {
                UI.InteractiveShellCtrlCBtn.Enabled = false;
                UI.MultiLineShellExecuteBtn.Enabled = true;
                UI.InteractiveShellPromptBox.BackColor = Color.Black;
                UI.InteractiveShellIn.BackColor = Color.Black;
                UI.InteractiveShellIn.ReadOnly = false;
                UI.InteractiveShellPythonRB.Enabled = true;
                UI.InteractiveShellRubyRB.Enabled = true;
                UI.InteractiveShellIn.Focus();
            }
        }

        delegate void UpdateInteractiveShellOut_d(string Output);
        internal static void UpdateInteractiveShellOut(string Output)
        {
            if (UI.InteractiveShellOut.InvokeRequired)
            {
                UpdateInteractiveShellOut_d UISO_d = new UpdateInteractiveShellOut_d(UpdateInteractiveShellOut);
                UI.Invoke(UISO_d, new object[] { Output });
            }
            else
            {
                IronScripting.ShellOutText.Append(Output);
                if (IronScripting.CheckOnOutText())
                {
                    UI.InteractiveShellOut.Text = IronScripting.ShellOutText.ToString().TrimEnd();
                    UI.InteractiveShellOut.AppendText("\r\n");
                }
                else
                {
                    UI.InteractiveShellOut.AppendText(Output);
                }
            }
        }

        delegate void UpdateInteractiveShellResult_d(InteractiveShellResult ISR);
        internal static void UpdateInteractiveShellResult(InteractiveShellResult ISR)
        {
            if (UI.InteractiveShellOut.InvokeRequired)
            {
                UpdateInteractiveShellResult_d UISR_d = new UpdateInteractiveShellResult_d(UpdateInteractiveShellResult);
                UI.Invoke(UISR_d, new object[] { ISR });
            }
            else
            {
                if (ISR.MoreExpected)
                {
                    UpdateInteractiveShellOut(IronScripting.ShellPrompt);
                }
                else if(ISR.ResultString.Length > 0)
                {
                    UpdateInteractiveShellOut(ISR.ResultString);
                }
                UpdateShellInPrompt(IronScripting.ShellPrompt);
                ActivateinteractiveShellUI();
            }
        }

        delegate void ResetInteractiveShellResult_d();
        internal static void ResetInteractiveShellResult()
        {
            if (UI.InteractiveShellOut.InvokeRequired)
            {
                ResetInteractiveShellResult_d RISR_d = new ResetInteractiveShellResult_d(ResetInteractiveShellResult);
                UI.Invoke(RISR_d, new object[] { });
            }
            else
            {
                IronUI.UI.InteractiveShellOut.Text = "";
            }

        }

        delegate void EndInitialiseAll_d();
        internal static void EndInitialiseAll()
        {
            if (UI.InvokeRequired)
            {
                EndInitialiseAll_d EIA_d = new EndInitialiseAll_d(EndInitialiseAll);
                UI.Invoke(EIA_d, new object[] { });
            }
            else
            {
                UI.Visible = true;
            }

        }

        delegate void EnableStoredRequestBtn_d();
        internal static void EnableStoredRequestBtn()
        {
            if (UI.MTStoredRequestBtn.InvokeRequired)
            {
                EnableStoredRequestBtn_d ESRB_d = new EnableStoredRequestBtn_d(EnableStoredRequestBtn);
                UI.Invoke(ESRB_d, new object[] { });
            }
            else
            {
                UI.MTStoredRequestBtn.Enabled = true;
            }
        }
        delegate void UpdateProxyStatusInConfigPanel_d(bool Started);
        internal static void UpdateProxyStatusInConfigPanel(bool Started)
        {
            if (UI.ConfigSetAsSystemProxyCB.InvokeRequired)
            {
                UpdateProxyStatusInConfigPanel_d UPSICP_d = new UpdateProxyStatusInConfigPanel_d(UpdateProxyStatusInConfigPanel);
                UI.Invoke(UPSICP_d, new object[] { Started });
            }
            else
            {
                if (Started)
                {
                    IronUI.UI.ConfigSetAsSystemProxyCB.Enabled = true;
                }
                else
                {
                    IronUI.UI.ConfigSetAsSystemProxyCB.Enabled = false;
                }
            }
        }

        delegate void ShowProxyStoppedError_d(string Error);
        internal static void ShowProxyStoppedError(string Error)
        {
            if (UI.ConfigSetAsSystemProxyCB.InvokeRequired)
            {
                ShowProxyStoppedError_d SPSE_d = new ShowProxyStoppedError_d(ShowProxyStoppedError);
                UI.Invoke(SPSE_d, new object[] { Error });
            }
            else
            {
                IronUI.ShowProxyException(Error);
                if(!IronUI.UI.main_tab.SelectedTab.Name.Equals("mt_proxy")) IronUI.UI.main_tab.SelectTab("mt_proxy");
                MakeUiTopMost(true);
            }
        }

        delegate void UpdateUIFromConfig_d();
        internal static void UpdateUIFromConfig()
        {
            if (UI.InvokeRequired)
            {
                UpdateUIFromConfig_d UUFC_d = new UpdateUIFromConfig_d(UpdateUIFromConfig);
                UI.Invoke(UUFC_d, new object[] { });
            }
            else
            {
                UpdateProxySettingFromConfig();
                UpdateUpstreamProxySettingFromConfig();
                UpdatePyPathsFromConfig();
                UpdateRbPathsFromConfig();
                UpdatePyCommandsFromConfig();
                UpdateRbCommandsFromConfig();
                UpdateRequestTextTypesFromConfig();
                UpdateResponseTextTypesFromConfig();
                UpdateLogDisplayFilterInUIFromConfig();
                UpdateInterceptionRulesInUIFromConfig();
                UpdateJSTaintConfigInUIFromConfig();
                UpdateScannerSettingsInUIFromConfig();
                UpdatePassiveAnalysisSettingsInUIFromConfig();
            }
        }

        delegate void SetProxyPortLabel_d();
        internal static void SetProxyPortLabel()
        {
            if (UI.ProxyPortLbl.InvokeRequired)
            {
                SetProxyPortLabel_d CALL_d = new SetProxyPortLabel_d(SetProxyPortLabel);
                UI.Invoke(CALL_d, new object[] { });
            }
            else
            {
                UI.ProxyPortLbl.Text = IronProxy.Port.ToString();
            }
        }

        delegate void SetProxyLoopbackLabel_d();
        internal static void SetProxyLoopbackLabel()
        {
            if (UI.ProxyLoopbackLbl.InvokeRequired)
            {
                SetProxyLoopbackLabel_d CALL_d = new SetProxyLoopbackLabel_d(SetProxyLoopbackLabel);
                UI.Invoke(CALL_d, new object[] { });
            }
            else
            {
                if (IronProxy.LoopBackOnly)
                {
                    UI.ProxyLoopbackLbl.Text = "No";
                }
                else
                {
                    UI.ProxyLoopbackLbl.Text = "Yes";
                }
            }
        }

        internal static void UpdateProxySettingFromConfig()
        {
            UI.ConfigSetAsSystemProxyCB.Checked = IronProxy.SystemProxy;
        }

        internal static void UpdateUpstreamProxySettingFromConfig()
        {
            bool UseUpStream = IronProxy.UseUpstreamProxy;
            IronProxy.UseUpstreamProxy = UseUpStream;
        }

        internal static void UpdatePyPathsFromConfig()
        {
            StringBuilder PyPaths = new StringBuilder();
            foreach(string Path in IronScripting.PyPaths)
            {
                PyPaths.AppendLine(Path);
            }
            UI.ConfigScriptPyPathsTB.Text = PyPaths.ToString();
        }

        internal static void UpdateRbPathsFromConfig()
        {
            StringBuilder RbPaths = new StringBuilder();
            foreach (string Path in IronScripting.RbPaths)
            {
                RbPaths.AppendLine(Path);
            }
            UI.ConfigScriptRbPathsTB.Text = RbPaths.ToString();
        }

        internal static void UpdatePyCommandsFromConfig()
        {
            StringBuilder PyCommands = new StringBuilder();
            foreach (string Command in IronScripting.PyCommands)
            {
                PyCommands.AppendLine(Command);
            }
            UI.ConfigScriptPyCommandsTB.Text = PyCommands.ToString();
        }

        internal static void UpdateRbCommandsFromConfig()
        {
            StringBuilder RbCommands = new StringBuilder();
            foreach (string Command in IronScripting.RbCommands)
            {
                RbCommands.AppendLine(Command);
            }
            UI.ConfigScriptRbCommandsTB.Text = RbCommands.ToString();
        }

        internal static void UpdateRequestTextTypesFromConfig()
        {
            StringBuilder Types = new StringBuilder();
            foreach (string Type in Request.TextContentTypes)
            {
                Types.AppendLine(Type);
            }
            UI.ConfigRequestTypesTB.Text = Types.ToString();
        }

        internal static void UpdateResponseTextTypesFromConfig()
        {
            StringBuilder Types = new StringBuilder();
            foreach (string Type in Response.TextContentTypes)
            {
                Types.AppendLine(Type);
            }
            UI.ConfigResponseTypesTB.Text = Types.ToString();
        }

        internal static void UpdateLogDisplayFilterInUIFromConfig()
        {
            UI.ConfigDisplayRuleGETMethodCB.Checked = IronProxy.DisplayGET;
            UI.ConfigDisplayRulePOSTMethodCB.Checked = IronProxy.DisplayPOST;
            UI.ConfigDisplayRuleOtherMethodsCB.Checked = IronProxy.DisplayOtherMethods;
            UI.ConfigDisplayRuleCode200CB.Checked = IronProxy.Display200;
            UI.ConfigDisplayRuleCode2xxCB.Checked = IronProxy.Display2xx;
            UI.ConfigDisplayRuleCode301_2CB.Checked = IronProxy.Display301_2;
            UI.ConfigDisplayRuleCode3xxCB.Checked = IronProxy.Display3xx;
            UI.ConfigDisplayRuleCode403CB.Checked = IronProxy.Display403;
            UI.ConfigDisplayRuleCode4xxCB.Checked = IronProxy.Display4xx;
            UI.ConfigDisplayRuleCode500CB.Checked = IronProxy.Display500;
            UI.ConfigDisplayRuleCode5xxCB.Checked = IronProxy.Display5xx;
            UI.ConfigDisplayRuleContentHTMLCB.Checked = IronProxy.DisplayHTML;
            UI.ConfigDisplayRuleContentCSSCB.Checked = IronProxy.DisplayCSS;
            UI.ConfigDisplayRuleContentJSCB.Checked = IronProxy.DisplayJS;
            UI.ConfigDisplayRuleContentXMLCB.Checked = IronProxy.DisplayXML;
            UI.ConfigDisplayRuleContentJSONCB.Checked = IronProxy.DisplayJSON;
            UI.ConfigDisplayRuleContentOtherTextCB.Checked = IronProxy.DisplayOtherText;
            UI.ConfigDisplayRuleContentImgCB.Checked = IronProxy.DisplayImg;
            UI.ConfigDisplayRuleContentOtherBinaryCB.Checked = IronProxy.DisplayOtherBinary;

            UI.ConfigDisplayRuleHostNamesCB.Checked = IronProxy.DisplayCheckHostNames;
            UI.ConfigDisplayRuleHostNamesPlusRB.Checked = IronProxy.DisplayCheckHostNamesPlus;
            UI.ConfigDisplayRuleHostNamesMinusRB.Checked = IronProxy.DisplayCheckHostNamesMinus;
            UI.ConfigDisplayRuleHostNamesPlusTB.Text = Tools.ListToCsv(IronProxy.DisplayHostNames);
            UI.ConfigDisplayRuleHostNamesMinusTB.Text = Tools.ListToCsv(IronProxy.DontDisplayHostNames);
            UI.ConfigDisplayRuleFileExtensionsCB.Checked = IronProxy.DisplayCheckFileExtensions;
            UI.ConfigDisplayRuleFileExtensionsPlusRB.Checked = IronProxy.DisplayCheckFileExtensionsPlus;
            UI.ConfigDisplayRuleFileExtensionsMinusRB.Checked = IronProxy.DisplayCheckFileExtensionsMinus;
            UI.ConfigDisplayRuleFileExtensionsPlusTB.Text = Tools.ListToCsv(IronProxy.DisplayFileExtensions);
            UI.ConfigDisplayRuleFileExtensionsMinusTB.Text = Tools.ListToCsv(IronProxy.DontDisplayFileExtensions);
        }

        internal static void UpdateInterceptionRulesInUIFromConfig()
        {
            UI.ConfigRuleGETMethodCB.Checked = IronProxy.InterceptGET;
            UI.ConfigRulePOSTMethodCB.Checked = IronProxy.InterceptPOST;
            UI.ConfigRuleOtherMethodsCB.Checked = IronProxy.InterceptOtherMethods;
            UI.ConfigRuleCode200CB.Checked = IronProxy.Intercept200;
            UI.ConfigRuleCode2xxCB.Checked = IronProxy.Intercept2xx;
            UI.ConfigRuleCode301_2CB.Checked = IronProxy.Intercept301_2;
            UI.ConfigRuleCode3xxCB.Checked = IronProxy.Intercept3xx;
            UI.ConfigRuleCode403CB.Checked = IronProxy.Intercept403;
            UI.ConfigRuleCode4xxCB.Checked = IronProxy.Intercept4xx;
            UI.ConfigRuleCode500CB.Checked = IronProxy.Intercept500;
            UI.ConfigRuleCode5xxCB.Checked = IronProxy.Intercept5xx;
            UI.ConfigRuleContentHTMLCB.Checked = IronProxy.InterceptHTML;
            UI.ConfigRuleContentCSSCB.Checked = IronProxy.InterceptCSS;
            UI.ConfigRuleContentJSCB.Checked = IronProxy.InterceptJS;
            UI.ConfigRuleContentXMLCB.Checked = IronProxy.InterceptXML;
            UI.ConfigRuleContentJSONCB.Checked = IronProxy.InterceptJSON;
            UI.ConfigRuleContentOtherTextCB.Checked = IronProxy.InterceptOtherText;
            UI.ConfigRuleContentImgCB.Checked = IronProxy.InterceptImg;
            UI.ConfigRuleContentOtherBinaryCB.Checked = IronProxy.InterceptOtherBinary;

            UI.ConfigRuleHostNamesCB.Checked = IronProxy.InterceptCheckHostNames;
            UI.ConfigRuleHostNamesPlusRB.Checked = IronProxy.InterceptCheckHostNamesPlus;
            UI.ConfigRuleHostNamesMinusRB.Checked = IronProxy.InterceptCheckHostNamesMinus;
            UI.ConfigRuleHostNamesPlusTB.Text = Tools.ListToCsv(IronProxy.InterceptHostNames);
            UI.ConfigRuleHostNamesMinusTB.Text = Tools.ListToCsv(IronProxy.DontInterceptHostNames);

            UI.ConfigRuleFileExtensionsCB.Checked = IronProxy.InterceptCheckFileExtensions;
            UI.ConfigRuleFileExtensionsPlusRB.Checked = IronProxy.InterceptCheckFileExtensionsPlus;
            UI.ConfigRuleFileExtensionsMinusRB.Checked = IronProxy.InterceptCheckFileExtensionsMinus;
            UI.ConfigRuleFileExtensionsPlusTB.Text = Tools.ListToCsv(IronProxy.InterceptFileExtensions);
            UI.ConfigRuleFileExtensionsMinusTB.Text = Tools.ListToCsv(IronProxy.DontInterceptFileExtensions);
            
            UI.ConfigRuleKeywordInRequestCB.Checked = IronProxy.InterceptCheckRequestWithKeyword;
            UI.ConfigRuleKeywordInRequestPlusRB.Checked = IronProxy.InterceptCheckRequestWithKeywordPlus;
            UI.ConfigRuleKeywordInRequestMinusRB.Checked = IronProxy.InterceptCheckRequestWithKeywordMinus;
            UI.ConfigRuleKeywordInRequestPlusTB.Text = IronProxy.InterceptRequestWithKeyword;
            UI.ConfigRuleKeywordInRequestMinusTB.Text = IronProxy.DontInterceptRequestWithKeyword;

            UI.ConfigRuleKeywordInResponseCB.Checked = IronProxy.InterceptCheckResponseWithKeyword;
            UI.ConfigRuleKeywordInResponsePlusRB.Checked = IronProxy.InterceptCheckResponseWithKeywordPlus;
            UI.ConfigRuleKeywordInResponseMinusRB.Checked = IronProxy.InterceptCheckResponseWithKeywordMinus;
            UI.ConfigRuleKeywordInResponsePlusTB.Text = IronProxy.InterceptResponseWithKeyword;
            UI.ConfigRuleKeywordInResponseMinusTB.Text = IronProxy.DontInterceptResponseWithKeyword;

            UI.ConfigRuleRequestOnResponseRulesCB.Checked = IronProxy.RequestRulesOnResponse;
        }

        internal static void UpdateJSTaintConfigInUIFromConfig()
        {
            UI.ConfigDefaultJSTaintConfigGrid.Rows.Clear();
            List<List<string>> Lists = new List<List<string>>() { new List<string>(IronJint.DefaultSourceObjects), new List<string>(IronJint.DefaultSinkObjects), new List<string>(IronJint.DefaultSourceReturningMethods), new List<string>(IronJint.DefaultSinkReturningMethods), new List<string>(IronJint.DefaultArgumentReturningMethods), new List<string>(IronJint.DefaultArgumentAssignedASourceMethods), new List<string>(IronJint.DefaultArgumentAssignedToSinkMethods) };
            int MaxCount = 0;
            foreach (List<string> List in Lists)
            {
                if (List.Count > MaxCount) MaxCount = List.Count;
            }
            foreach (List<string> List in Lists)
            {
                while (List.Count < MaxCount)
                {
                    List.Add("");
                }
            }
            for (int i = 0; i < MaxCount; i++)
            {
                UI.ConfigDefaultJSTaintConfigGrid.Rows.Add(new object[] { Lists[0][i], Lists[1][i], Lists[5][i], Lists[6][i], Lists[2][i], Lists[3][i], Lists[4][i] });
            }
        }

        delegate void UpdateScannerSettingsInUIFromConfig_d();
        internal static void UpdateScannerSettingsInUIFromConfig()
        {
            if (UI.ConfigScannerThreadMaxCountTB.InvokeRequired)
            {
                UpdateScannerSettingsInUIFromConfig_d CALL_d = new UpdateScannerSettingsInUIFromConfig_d(UpdateScannerSettingsInUIFromConfig);
                UI.Invoke(CALL_d, new object[] { });
            }
            else
            {
                UI.ConfigScannerThreadMaxCountTB.Value = Scanner.MaxParallelScanCount;
                UI.ConfigScannerThreadMaxCountLbl.Text = UI.ConfigScannerThreadMaxCountTB.Value.ToString();
            }
        }

        internal static void UpdatePassiveAnalysisSettingsInUIFromConfig()
        {
            UI.ConfigPassiveAnalysisOnProxyTrafficCB.Checked = PassiveChecker.RunOnProxyTraffic;
            UI.ConfigPassiveAnalysisOnShellTrafficCB.Checked = PassiveChecker.RunOnShellTraffic;
            UI.ConfigPassiveAnalysisOnTestTrafficCB.Checked = PassiveChecker.RunOnTestTraffic;
            UI.ConfigPassiveAnalysisOnScanTrafficCB.Checked = PassiveChecker.RunOnScanTraffic;
            UI.ConfigPassiveAnalysisOnProbeTrafficCB.Checked = PassiveChecker.RunOnProbeTraffic;
        }

        delegate void UpdateProxyLogBasedOnDisplayFilter_d();
        internal static void UpdateProxyLogBasedOnDisplayFilter()
        {

            if (UI.ProxyLogGrid.InvokeRequired)
            {
                UpdateProxyLogBasedOnDisplayFilter_d UPLBODF_d = new UpdateProxyLogBasedOnDisplayFilter_d(UpdateProxyLogBasedOnDisplayFilter);
                UI.Invoke(UPLBODF_d, new object[] { });
            }
            else
            {
                string Method = null;
                string Host = null;
                string FileExtension = null;
                int Code = 0;
                string ContentType = null;
                bool IgnoreContentType = false;

                foreach (DataGridViewRow Row in UI.ProxyLogGrid.Rows)
                {
                    if (Row.Cells["ProxyLogGridColumnForMethod"].Value != null)
                    {
                        Method = Row.Cells["ProxyLogGridColumnForMethod"].Value.ToString();
                    }
                    else
                    {
                        Method = null;
                    }
                    if (Row.Cells["ProxyLogGridColumnForHostName"].Value != null)
                    {
                        Host = Row.Cells["ProxyLogGridColumnForHostName"].Value.ToString();
                    }
                    else
                    {
                        Host = null;
                    }
                    if (Row.Cells["ProxyLogGridColumnForFile"].Value != null)
                    {
                        FileExtension = Row.Cells["ProxyLogGridColumnForFile"].Value.ToString();
                    }
                    else
                    {
                        FileExtension = null;
                    }
                    if (Row.Cells["ProxyLogGridColumnForCode"].Value != null)
                    {
                        if (!Int32.TryParse(Row.Cells["ProxyLogGridColumnForCode"].Value.ToString(), out Code))
                        {
                            Code = 0;
                        }
                    }
                    else
                    {
                        Code = 0;
                    }
                    if (Row.Cells["ProxyLogGridColumnForMIME"].Value != null)
                    {
                        ContentType = Row.Cells["ProxyLogGridColumnForMIME"].Value.ToString();
                    }
                    else
                    {
                        ContentType = null;
                    }
                    if (Row.Cells["ProxyLogGridColumnForLength"].Value != null)
                    {
                        if (Row.Cells["ProxyLogGridColumnForLength"].Value.ToString().Equals("0"))
                        {
                            IgnoreContentType = true;
                        }
                    }
                    else
                    {
                        IgnoreContentType = false;
                    }
                    Row.Visible = IronProxy.CanDisplayRowInLogDisplay(Method, Host, FileExtension, Code, ContentType, IgnoreContentType);
                }
            }
        }

        internal static void StartUpdatingFullUIFromDB()
        {
            IronProxy.Stop();
            SetUIVisibility(false);

            IronUI.WF = new WaitForm();
            
            Thread T = new Thread(new ThreadStart(ShowWaitForm));
            T.Start();
            Thread.Sleep(1000);
            UpdateFullUIFromDB();
        }

        static void ShowWaitForm()
        {
            IronUI.WF.Text = "Reading Iron Project file...";
            IronUI.WF.WaitFormProgressBar.Minimum = 1;
            IronUI.WF.WaitFormProgressBar.Maximum = 12;
            IronUI.WF.WaitFormProgressBar.Value = 1;
            IronUI.WF.WaitFormProgressBar.Step = 1;
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Tasks", "Count", "Status" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Proxy Logs", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Manual Testing Logs", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Scripting Shell Logs", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Automated Scanning Queue", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load ScanTrace Messages", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Automated Scanning Logs", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Probe Logs", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Plugin Results", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Exceptions", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Sitemap", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows.Add(new object[] { "Load Trace Messages", "0", "Not Done" });
            IronUI.WF.ProjectLoadGrid.Rows[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            IronUI.WF.ShowDialog();
        }

        delegate void StopUpdatingFullUIFromDB_d();
        static void StopUpdatingFullUIFromDB()
        {
            if (IronUI.WF.InvokeRequired)
            {
                SetUIVisibility(true);
                StopUpdatingFullUIFromDB_d SUFUFD_d = new StopUpdatingFullUIFromDB_d(StopUpdatingFullUIFromDB);
                IronUI.WF.Invoke(SUFUFD_d, new object[] {  });
            }
            else
            {
                WF.OK.Visible = true;
                WF.TopMost = true;
                WF.TopMost = false;
            }
        }

        delegate void ClearAllProxyGridRows_d();
        static void ClearAllProxyGridRows()
        {
            if (UI.ProxyLogGrid.InvokeRequired)
            {
                ClearAllProxyGridRows_d CAPGR_d = new ClearAllProxyGridRows_d(ClearAllProxyGridRows);
                UI.Invoke(CAPGR_d, new object[] {  });
            }
            else
            {
                UI.ProxyLogGrid.Rows.Clear();
            }
        }

        //delegate void AddProxyGridRows_d(List<object[]> Rows);
        //static void AddProxyGridRows(List<object[]> Rows)
        //{
        //    if (UI.ProxyLogGrid.InvokeRequired)
        //    {
        //        AddProxyGridRows_d APGR_d = new AddProxyGridRows_d(AddProxyGridRows);
        //        UI.Invoke(APGR_d, new object[] { Rows });
        //    }
        //    else
        //    {
        //        foreach (object[] Row in Rows)
        //        {
        //            if (UI.ProxyLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
        //            try 
        //            { 
        //                UI.ProxyLogGrid.Rows.Add(Row);
        //                int ID = (int)Row[0];
        //                if (ID > IronLog.ProxyMax) IronLog.ProxyMax = ID;
        //                if (ID < IronLog.ProxyMin || IronLog.ProxyMin < 1) IronLog.ProxyMin = ID;
        //            }
        //            catch { }
        //        }
        //        Rows.Clear();
        //    }
        //}

        delegate void SetProxyGridRows_d(List<object[]> Rows);
        internal static void SetProxyGridRows(List<object[]> Rows)
        {
            if (UI.ProxyLogGrid.InvokeRequired)
            {
                SetProxyGridRows_d SPGR_d = new SetProxyGridRows_d(SetProxyGridRows);
                UI.Invoke(SPGR_d, new object[] { Rows });
            }
            else
            {
                UI.ProxyLogGrid.Rows.Clear();
                IronLog.ProxyMin = 0;
                IronLog.ProxyMax = 0;
                foreach (object[] Row in Rows)
                {
                    if (UI.ProxyLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        UI.ProxyLogGrid.Rows.Add(Row);
                        int ID = (int)Row[0];
                        if (ID > IronLog.ProxyMax) IronLog.ProxyMax = ID;
                        if (ID < IronLog.ProxyMin || IronLog.ProxyMin < 1) IronLog.ProxyMin = ID;
                    }
                    catch { }
                }
                Rows.Clear();
                ShowCurrentLogStat();
                ShowLogBottomStatus("", false);
            }
        }

        delegate void ClearAllMTGridRows_d();
        static void ClearAllMTGridRows()
        {
            if (UI.TestLogGrid.InvokeRequired)
            {
                ClearAllMTGridRows_d CAMTGR_d = new ClearAllMTGridRows_d(ClearAllMTGridRows);
                UI.Invoke(CAMTGR_d, new object[] { });
            }
            else
            {
                UI.TestLogGrid.Rows.Clear();
            }
        }

        //delegate void AddTestGridRows_d(List<object[]> Rows);
        //static void AddTestGridRows(List<object[]> Rows)
        //{
        //    if (UI.TestLogGrid.InvokeRequired)
        //    {
        //        AddTestGridRows_d ATGR_d = new AddTestGridRows_d(AddTestGridRows);
        //        UI.Invoke(ATGR_d, new object[] { Rows });
        //    }
        //    else
        //    {
        //        foreach (object[] Row in Rows)
        //        {
        //            if (UI.TestLogGrid.Rows.Count > IronLog.MaxRowCount) break;
        //            try 
        //            {
        //                UI.TestLogGrid.Rows.Add(Row);
        //                int ID = (int)Row[0];
        //                if (ID > IronLog.TestMax) IronLog.TestMax = ID;
        //                if (ID < IronLog.TestMin || IronLog.TestMin < 1) IronLog.TestMin = ID;
        //            }
        //            catch { }
        //        }
        //        Rows.Clear();
        //    }
        //}

        delegate void SetTestGridRows_d(List<object[]> Rows);
        internal static void SetTestGridRows(List<object[]> Rows)
        {
            if (UI.TestLogGrid.InvokeRequired)
            {
                SetTestGridRows_d STGR_d = new SetTestGridRows_d(SetTestGridRows);
                UI.Invoke(STGR_d, new object[] { Rows });
            }
            else
            {
                UI.TestLogGrid.Rows.Clear();
                IronLog.TestMin = 0;
                IronLog.TestMax = 0;
                foreach (object[] Row in Rows)
                {
                    if (UI.TestLogGrid.Rows.Count > IronLog.MaxRowCount) break;
                    try
                    {
                        UI.TestLogGrid.Rows.Add(Row);
                        int ID = (int)Row[0];
                        if (ID > IronLog.TestMax) IronLog.TestMax = ID;
                        if (ID < IronLog.TestMin || IronLog.TestMin < 1) IronLog.TestMin = ID;
                    }
                    catch { }
                }
                Rows.Clear();
                ShowCurrentLogStat();
                ShowLogBottomStatus("", false);
            }
        }

        delegate void ClearAllShellGridRows_d();
        static void ClearAllShellGridRows()
        {
            if (UI.ShellLogGrid.InvokeRequired)
            {
                ClearAllShellGridRows_d CASGR_d = new ClearAllShellGridRows_d(ClearAllShellGridRows);
                UI.Invoke(CASGR_d, new object[] { });
            }
            else
            {
                UI.ShellLogGrid.Rows.Clear();
            }
        }

        
        //delegate void AddShellGridRows_d(List<object[]> Rows);
        //static void AddShellGridRows(List<object[]> Rows)
        //{
        //    if (UI.ShellLogGrid.InvokeRequired)
        //    {
        //        AddShellGridRows_d ASGR_d = new AddShellGridRows_d(AddShellGridRows);
        //        UI.Invoke(ASGR_d, new object[] { Rows });
        //    }
        //    else
        //    {
        //        foreach (object[] Row in Rows)
        //        {
        //            if (UI.ShellLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
        //            try
        //            { 
        //                UI.ShellLogGrid.Rows.Add(Row);
        //                int ID = (int)Row[0];
        //                if (ID > IronLog.ShellMax) IronLog.ShellMax = ID;
        //                if (ID < IronLog.ShellMin || IronLog.ShellMin < 1) IronLog.ShellMin = ID;
        //            }
        //            catch { }
        //        }
        //        Rows.Clear();
        //    }
        //}

        delegate void SetShellGridRows_d(List<object[]> Rows);
        internal static void SetShellGridRows(List<object[]> Rows)
        {
            if (UI.ShellLogGrid.InvokeRequired)
            {
                SetShellGridRows_d SSGR_d = new SetShellGridRows_d(SetShellGridRows);
                UI.Invoke(SSGR_d, new object[] { Rows });
            }
            else
            {
                UI.ShellLogGrid.Rows.Clear();
                IronLog.ShellMin = 0;
                IronLog.ShellMax = 0;
                foreach (object[] Row in Rows)
                {
                    if (UI.ShellLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        UI.ShellLogGrid.Rows.Add(Row);
                        int ID = (int)Row[0];
                        if (ID > IronLog.ShellMax) IronLog.ShellMax = ID;
                        if (ID < IronLog.ShellMin || IronLog.ShellMin < 1) IronLog.ShellMin = ID;
                    }
                    catch { }
                }
                Rows.Clear();
                ShowCurrentLogStat();
                ShowLogBottomStatus("", false);
            }
        }

        delegate void SetOtherSourceGridRows_d(List<object[]> Rows, string Source);
        internal static void SetOtherSourceGridRows(List<object[]> Rows, string Source)
        {
            if (UI.OtherLogGrid.InvokeRequired)
            {
                SetOtherSourceGridRows_d SOSGR_d = new SetOtherSourceGridRows_d(SetOtherSourceGridRows);
                UI.Invoke(SOSGR_d, new object[] { Rows, Source });
            }
            else
            {
                List<string> Sources = new List<string>();
                foreach (DataGridViewRow Row in UI.OtherLogSourceGrid.Rows)
                {
                    Sources.Add(Row.Cells[0].Value.ToString());
                }
                if (!Source.Contains(Source))
                {
                    UI.OtherLogSourceGrid.Rows.Add(new object[]{Source});
                }
                UI.OtherLogGrid.Rows.Clear();
                IronLog.OtherSourceMin = 0;
                IronLog.OtherSourceMax = 0;
                foreach (object[] Row in Rows)
                {
                    if (UI.OtherLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        UI.OtherLogGrid.Rows.Add(Row);
                        int ID = (int)Row[0];
                        if (ID > IronLog.OtherSourceMax) IronLog.OtherSourceMax = ID;
                        if (ID < IronLog.OtherSourceMin || IronLog.OtherSourceMin < 1) IronLog.OtherSourceMin = ID;
                    }
                    catch { }
                }
                Rows.Clear();
                ShowCurrentLogStat();
                ShowLogBottomStatus("", false);
            }
        }

        delegate void ClearAllProbeGridRows_d();
        static void ClearAllProbeGridRows()
        {
            if (UI.ProbeLogGrid.InvokeRequired)
            {
                ClearAllProbeGridRows_d CAPGR_d = new ClearAllProbeGridRows_d(ClearAllProbeGridRows);
                UI.Invoke(CAPGR_d, new object[] { });
            }
            else
            {
                UI.ProbeLogGrid.Rows.Clear();
            }
        }


        //delegate void AddProbeGridRows_d(List<object[]> Rows);
        //static void AddProbeGridRows(List<object[]> Rows)
        //{
        //    if (UI.ProbeLogGrid.InvokeRequired)
        //    {
        //        AddProbeGridRows_d APGR_d = new AddProbeGridRows_d(AddProbeGridRows);
        //        UI.Invoke(APGR_d, new object[] { Rows });
        //    }
        //    else
        //    {
        //        foreach (object[] Row in Rows)
        //        {
        //            if (UI.ProbeLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
        //            try
        //            { 
        //                UI.ProbeLogGrid.Rows.Add(Row);
        //                int ID = (int)Row[0];
        //                if (ID > IronLog.ProbeMax) IronLog.ProbeMax = ID;
        //                if (ID < IronLog.ProbeMin || IronLog.ProbeMin < 1) IronLog.ProbeMin = ID;
        //            }
        //            catch { }
        //        }
        //        Rows.Clear();
        //    }
        //}

        delegate void SetProbeGridRows_d(List<object[]> Rows);
        internal static void SetProbeGridRows(List<object[]> Rows)
        {
            if (UI.ProbeLogGrid.InvokeRequired)
            {
                SetProbeGridRows_d SPGR_d = new SetProbeGridRows_d(SetProbeGridRows);
                UI.Invoke(SPGR_d, new object[] { Rows });
            }
            else
            {
                UI.ProbeLogGrid.Rows.Clear();
                IronLog.ProbeMin = 0;
                IronLog.ProbeMax = 0;
                foreach (object[] Row in Rows)
                {
                    if (UI.ProbeLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        UI.ProbeLogGrid.Rows.Add(Row);
                        int ID = (int)Row[0];
                        if (ID > IronLog.ProbeMax) IronLog.ProbeMax = ID;
                        if (ID < IronLog.ProbeMin || IronLog.ProbeMin < 1) IronLog.ProbeMin = ID;
                    }
                    catch { }
                }
                Rows.Clear();
                ShowCurrentLogStat();
                ShowLogBottomStatus("", false);
            }
        }

        delegate void ClearAllScanGridRows_d();
        static void ClearAllScanGridRows()
        {
            if (UI.ScanLogGrid.InvokeRequired)
            {
                ClearAllScanGridRows_d CASGR_d = new ClearAllScanGridRows_d(ClearAllScanGridRows);
                UI.Invoke(CASGR_d, new object[] { });
            }
            else
            {
                UI.ScanLogGrid.Rows.Clear();
            }
        }

        //delegate void AddScanGridRows_d(List<object[]> Rows);
        //static void AddScanGridRows(List<object[]> Rows)
        //{
        //    if (UI.ScanLogGrid.InvokeRequired)
        //    {
        //        AddScanGridRows_d ASGR_d = new AddScanGridRows_d(AddScanGridRows);
        //        UI.Invoke(ASGR_d, new object[] { Rows });
        //    }
        //    else
        //    {
        //        foreach (object[] Row in Rows)
        //        {
        //            if (UI.ScanLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
        //            try
        //            {
        //                UI.ScanLogGrid.Rows.Add(Row);
        //                int ID = (int)Row[0];
        //                if (ID > IronLog.ScanMax) IronLog.ScanMax = ID;
        //                if (ID < IronLog.ScanMin || IronLog.ScanMin < 1) IronLog.ScanMin = ID;
        //            }
        //            catch { }
        //        }
        //        Rows.Clear();
        //    }
        //}

        delegate void SetScanGridRows_d(List<object[]> Rows);
        internal static void SetScanGridRows(List<object[]> Rows)
        {
            if (UI.ScanLogGrid.InvokeRequired)
            {
                SetScanGridRows_d SSGR_d = new SetScanGridRows_d(SetScanGridRows);
                UI.Invoke(SSGR_d, new object[] { Rows });
            }
            else
            {
                UI.ScanLogGrid.Rows.Clear();
                IronLog.ScanMax = 0;
                IronLog.ScanMin = 0;
                foreach (object[] Row in Rows)
                {
                    if (UI.ScanLogGrid.Rows.Count >= IronLog.MaxRowCount) break;
                    try
                    {
                        UI.ScanLogGrid.Rows.Add(Row);
                        int ID = (int)Row[0];
                        if (ID > IronLog.ScanMax) IronLog.ScanMax = ID;
                        if (ID < IronLog.ScanMin || IronLog.ScanMin < 1) IronLog.ScanMin = ID;
                    }
                    catch { }
                }
                Rows.Clear();
                ShowCurrentLogStat();
                ShowLogBottomStatus("", false);
            }
        }

        delegate void ClearAllScanQueueGridRows_d();
        static void ClearAllScanQueueGridRows()
        {
            if (UI.ASQueueGrid.InvokeRequired)
            {
                ClearAllScanQueueGridRows_d CASQGR_d = new ClearAllScanQueueGridRows_d(ClearAllScanQueueGridRows);
                UI.Invoke(CASQGR_d, new object[] { });
            }
            else
            {
                UI.ASQueueGrid.Rows.Clear();
            }
        }

        delegate void ClearAllTraceGridRows_d();
        static void ClearAllTraceGridRows()
        {
            if (UI.TraceGrid.InvokeRequired)
            {
                ClearAllTraceGridRows_d CATGR_d = new ClearAllTraceGridRows_d(ClearAllTraceGridRows);
                UI.Invoke(CATGR_d, new object[] { });
            }
            else
            {
                UI.TraceGrid.Rows.Clear();
            }
        }

        delegate void ClearAllScanTraceGridRows_d();
        static void ClearAllScanTraceGridRows()
        {
            if (UI.ScanTraceGrid.InvokeRequired)
            {
                ClearAllScanTraceGridRows_d CASTGR_d = new ClearAllScanTraceGridRows_d(ClearAllScanTraceGridRows);
                UI.Invoke(CASTGR_d, new object[] { });
            }
            else
            {
                UI.ScanTraceGrid.Rows.Clear();
            }
        }

        delegate void AddScanQueueGridRows_d(List<object[]> Rows);
        static void AddScanQueueGridRows(List<object[]> Rows)
        {
            if (UI.ASQueueGrid.InvokeRequired)
            {
                AddScanQueueGridRows_d ASQGR_d = new AddScanQueueGridRows_d(AddScanQueueGridRows);
                UI.Invoke(ASQGR_d, new object[] { Rows });
            }
            else
            {
                foreach (object[] Row in Rows)
                {
                    int GridID =  UI.ASQueueGrid.Rows.Add(Row);
                    DataGridViewRow GridRow = UI.ASQueueGrid.Rows[GridID];
                    string Status = Row[1].ToString();
                    switch (Status)
                    {
                        case "Running":
                            GridRow.DefaultCellStyle.BackColor = Color.Green;
                            break;
                        case "Aborted":
                            GridRow.DefaultCellStyle.BackColor = Color.Red;
                            break;
                        case "Completed":
                            GridRow.DefaultCellStyle.BackColor = Color.Gray;
                            break;
                        case "Incomplete":
                        case "Stopped":
                            GridRow.DefaultCellStyle.BackColor = Color.IndianRed;
                            break;
                        default:
                            GridRow.DefaultCellStyle.BackColor = Color.White;
                            break;
                    }
                }
                Rows.Clear();
            }
        }

        delegate void ResetIronTree_d();
        static void ResetIronTree()
        {
            if (UI.IronTree.InvokeRequired)
            {
                ResetIronTree_d RIT_d = new ResetIronTree_d(ResetIronTree);
                UI.Invoke(RIT_d, new object[] { });
            }
            else
            {
                UI.IronTree.Nodes.Clear();
                BuildIronTree();
            }
        }

        delegate void ShowWaitFormMessage_d(string Message);
        static void ShowWaitFormMessage(string Message)
        {
            if (IronUI.WF.InvokeRequired)
            {
                ShowWaitFormMessage_d ASQGR_d = new ShowWaitFormMessage_d(ShowWaitFormMessage);
                IronUI.WF.Invoke(ASQGR_d, new object[] { Message });
            }
            else
            {
                IronUI.WF.Text = Message;
            }
        }

        delegate void ShowWaitFormOkBtn_d();
        static void ShowWaitFormOkBtn()
        {
            if (IronUI.WF.InvokeRequired)
            {
                ShowWaitFormOkBtn_d SWFOB_d = new ShowWaitFormOkBtn_d(ShowWaitFormOkBtn);
                IronUI.WF.Invoke(SWFOB_d, new object[] { });
            }
            else
            {
                IronUI.WF.OK.Visible = true;
            }
        }

        delegate void ShowWaitFormGridMessage_d(int ID, int Count, string Message, int Colour, bool Step);
        static void ShowWaitFormGridMessage(int ID, int Count, string Message, int Colour, bool Step)
        {
            if (IronUI.WF.InvokeRequired)
            {
                ShowWaitFormGridMessage_d SWFGM_d = new ShowWaitFormGridMessage_d(ShowWaitFormGridMessage);
                IronUI.WF.Invoke(SWFGM_d, new object[] { ID, Count, Message, Colour, Step });
            }
            else
            {
                IronUI.WF.ProjectLoadGrid.Rows[ID].Cells[1].Value = Count.ToString();
                IronUI.WF.ProjectLoadGrid.Rows[ID].Cells[2].Value = Message;
                switch(Colour)
                {
                    case(1):
                        IronUI.WF.ProjectLoadGrid.Rows[ID].Cells[2].Style.ForeColor = Color.Green;
                        break;
                    case (2):
                        IronUI.WF.ProjectLoadGrid.Rows[ID].Cells[2].Style.ForeColor = Color.Orange;
                        break;
                    case (3):
                        IronUI.WF.ProjectLoadGrid.Rows[ID].Cells[2].Style.ForeColor = Color.Red;
                        break;
                }
                IronUI.WF.ProjectLoadGrid.Rows[0].Cells[2].Value = "Status";
                if(Step) IronUI.WF.WaitFormProgressBar.PerformStep();
            }
        }

        delegate void StepWaitFormProgressBar_d();
        static void StepWaitFormProgressBar()
        {
            if (IronUI.WF.InvokeRequired)
            {
                StepWaitFormProgressBar_d SWFPB_d = new StepWaitFormProgressBar_d(StepWaitFormProgressBar);
                IronUI.WF.Invoke(SWFPB_d, new object[] { });
            }
            else
            {
                IronUI.WF.WaitFormProgressBar.PerformStep();
            }
        }

        internal static void UpdateFullUIFromDB()
        {
            bool Success = true;

            List<List<string>> Urls = new List<List<string>>();

            int StartID = 0;
            int Counter = 0;

            try
            {
                IronDB.ReadOtherSourceLogInformation();
                IronDB.MakeLogFileVersionCompliant();
                IronUI.UpdateOtherSourceLogGrid(new List<Request>(), new List<Response>(), "", new List<string>(Config.OtherSourceCounterDict.Keys));
            }
            catch{ ShowWaitFormMessage("Error reading Other Source Log DB.."); }

            //Clear all data from the results section
            ResetPluginResultsTab();

            //StepWaitFormProgressBar();
            ShowWaitFormGridMessage(1, Counter, "In Progress", 2, true);
            ShowWaitFormMessage("Updating Proxy Logs..");
            
            //Thread.Sleep(500);
            Success = true;
            List<object[]> ProxyRows = new List<object[]>();

            List<LogRow> ProxyLogRecords = new List<LogRow>();
            
            ClearAllProxyGridRows();
            try
            {
                ProxyLogRecords = IronDB.GetRecordsFromProxyLog(0, IronLog.MaxRowCount); //.GetProxyLogRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading Proxy Log DB..");
                ShowWaitFormGridMessage(1, 0, "Failed", 3, false);
                Success = false;
            }
            if (ProxyLogRecords.Count > 0)
            {
                Counter = Counter + ProxyLogRecords.Count;
                foreach (LogRow Fields in ProxyLogRecords)
                {
                    if (Fields.ID > StartID) StartID = Fields.ID;
                    ProxyRows.Add(Fields.ToProxyGridRowObjectArray());
                    try
                    {
                        Request Req = new Request("http://" + Fields.Host + Fields.Url);
                        Urls.Add(IronUpdater.GetUrlForList(Req));
                    }
                    catch
                    {
                        //IronException.Report("Error creating Request from ProxyLogRow", Exp.Message, Exp.StackTrace);
                    }

                    if (Fields.OriginalRequestHeaders.Length > 0)
                    {
                        try
                        {
                            Request OriginalRequest = new Request(Fields.OriginalRequestHeaders, false, false);
                            Urls.Add(IronUpdater.GetUrlForList(OriginalRequest));
                        }
                        catch
                        {
                            //IronException.Report("Error creating Request from OriginalRequestHeaders of ProxyLogRow", Exp.Message, Exp.StackTrace);
                        }
                    }
                }
                ShowWaitFormGridMessage(1, Counter, "In Progress", 2, true);
                //try
                //{
                //    ProxyLogRecords = IronDB.GetProxyLogRecords(StartID);
                //}
                //catch 
                //{ 
                //    ShowWaitFormMessage("Error reading Proxy Log DB..");
                //    ShowWaitFormGridMessage(1, Counter, "Failed", 3, false);
                //    Success = false;
                //}
            }

            SetProxyGridRows(ProxyRows);

            UpdateProxyLogBasedOnDisplayFilter();
            //Config.ProxyRequestsCount = StartID;
            Config.ProxyRequestsCount = IronDB.GetLastProxyLogRowId();
            if (Success) ShowWaitFormGridMessage(1, Counter, "Done", 1, false);

            //test groups
            ClearTestGroupLogGrid();
            try
            {
                IronDB.LoadTestGroups();
            }
            catch { }
            //UI.TestIDLbl.BackColor = Color.Red;
            UI.TestIDLbl.Text = "ID: 0";
            //ManualTesting.CurrentGroup = "Red";
            UI.MTCurrentGroupNameTB.Text = "";
            IronUI.ResetMTDisplayFields();
            UI.TestGroupsLV.Items.Clear();
            foreach (string Group in ManualTesting.GroupSessions.Keys)
            {
                UI.TestGroupsLV.Items.Add(Group, Group, 0);
            }
            if (UI.TestGroupsLV.Items.Count > 0)
            {
                UI.TestGroupsTitleTB.Visible = true;
                UI.TestGroupsLV.Visible = true;
            }
            else
            {
                UI.TestGroupsTitleTB.Visible = false;
                UI.TestGroupsLV.Visible = false;
            }
            //if (ManualTesting.GroupSessions.Keys.Count > 0)
            //{
                //string FirstKey = (new List<string>(ManualTesting.GroupSessions.Keys))[0];
                //IronUI.UpdateTestGroupLogGrid(ManualTesting.RedGroupSessions);
                //IronUI.UpdateTestGroupLogGrid(ManualTesting.GroupSessions[FirstKey]);
                //ManualTesting.ShowSession(ManualTesting.RedGroupID);
                //ManualTesting.ShowSession(ManualTesting.CurrentGroupLogId[FirstKey]);
                
            //}
            Counter = 0;

            //StepWaitFormProgressBar();
            Success = true;
            ShowWaitFormGridMessage(2, Counter, "In Progress", 2, true);
            ShowWaitFormMessage("Updating Manual Testing Logs..");
            
            //Thread.Sleep(500);

            StartID = 0;
            List<object[]> MTRows = new List<object[]>();
            List<LogRow> MTLogRecords = new List<LogRow>();

            ClearAllMTGridRows();
            try
            {
                MTLogRecords = IronDB.GetRecordsFromTestLog(0, IronLog.MaxRowCount); //.GetTestLogRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading MT Log DB..");
                ShowWaitFormGridMessage(2, 0, "Failed", 3, false);
                Success = false;
            }
            
            if (MTLogRecords.Count > 0)
            {
                Counter = Counter + MTLogRecords.Count;
                foreach (LogRow Fields in MTLogRecords)
                {
                    if (Fields.ID > StartID) StartID = Fields.ID;
                    MTRows.Add(Fields.ToTestGridRowObjectArray());
                }
                ShowWaitFormGridMessage(2, Counter, "In Progress", 2, true);
                //try
                //{
                //    MTLogRecords = IronDB.GetTestLogRecords(StartID);
                //}
                //catch
                //{
                //    ShowWaitFormMessage("Error reading MT Log DB..");
                //    ShowWaitFormGridMessage(2, Counter, "Failed", 3, false);
                //    Success = false;
                //}
            }
            SetTestGridRows(MTRows);
            //Config.ManualRequestsCount = StartID;
            Config.TestRequestsCount = IronDB.GetLastTestLogRowId();
            if (Success) ShowWaitFormGridMessage(2, Counter, "Done", 1, false);

            //StepWaitFormProgressBar();

            Counter = 0;
            Success = true;
            ShowWaitFormMessage("Updating Scripting Logs..");
            ShowWaitFormGridMessage(3, Counter, "In Progress", 2, true);
            //Thread.Sleep(500);

            StartID = 0;
            List<object[]> ShellRows = new List<object[]>();
            List<LogRow> ShellLogRecords = new List<LogRow>();

            ClearAllShellGridRows();
            try
            {
                ShellLogRecords = IronDB.GetRecordsFromShellLog(0, IronLog.MaxRowCount); //.GetShellLogRecords(StartID);
            }
            catch
            { 
                ShowWaitFormMessage("Error reading Shell Log DB..");
                ShowWaitFormGridMessage(3, 0, "Failed", 3, false);
                Success = false;
            }
            if (ShellLogRecords.Count > 0)
            {
                Counter = Counter + ShellLogRecords.Count;
                foreach (LogRow Fields in ShellLogRecords)
                {
                    if (Fields.ID > StartID) StartID = Fields.ID;
                    ShellRows.Add(Fields.ToShellGridRowObjectArray());
                }
                ShowWaitFormGridMessage(3, Counter, "In Progress", 2, true);
                //try
                //{
                //    ShellLogRecords = IronDB.GetShellLogRecords(StartID);
                //}
                //catch
                //{
                //    ShowWaitFormMessage("Error reading Shell Log DB..");
                //    ShowWaitFormGridMessage(3, Counter, "Failed", 3, false);
                //    Success = false;
                //}
            }
            //Config.ShellRequestsCount = StartID;
            Config.ShellRequestsCount = IronDB.GetLastShellLogRowId();
            SetShellGridRows(ShellRows);
            if(Success) ShowWaitFormGridMessage(3, Counter, "Done", 1, false);

            //WF.WaitFormProgressBar.PerformStep();
            //StepWaitFormProgressBar();
            Success = true;
            Counter = 0;
            //WF.Text = "Updating Automated Scanning Queue..";
            ShowWaitFormMessage("Updating Automated Scanning Queue..");
            ShowWaitFormGridMessage(4, Counter, "In Progress", 2, true);
            //Thread.Sleep(500);

            StartID = 0;
            List<object[]> ScanQueueRows = new List<object[]>();
            List<string[]> ScanQueueRecords = new List<string[]>();

            ClearAllScanQueueGridRows();
            try
            {
                ScanQueueRecords = IronDB.GetScanQueueRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading ScanQueue Log DB..");
                ShowWaitFormGridMessage(4, 0, "Failed", 3, false);
                Success = false;
            }
            while (ScanQueueRecords.Count > 0)
            {
                Counter = Counter + ScanQueueRecords.Count;
                foreach (string[] Fields in ScanQueueRecords)
                {
                    int ID = 0;
                    try
                    {
                        ID = Int32.Parse(Fields[0]);
                    }
                    catch { continue; }
                    if (ID > StartID) StartID = ID;

                    string Status = Fields[1];

                    if (Fields[1].Equals("Running") || Fields[1].Equals("Started")) Status = "Aborted";
                    if (Fields[1].Equals("Queued")) Status = "Stopped";

                    ScanQueueRows.Add(new object[] { ID, Status, Fields[2], Fields[3] });
                }
                ShowWaitFormGridMessage(4, Counter, "In Progress", 2, true);
                try
                {
                    ScanQueueRecords = IronDB.GetScanQueueRecords(StartID);
                }
                catch
                {
                    ShowWaitFormMessage("Error reading ScanQueue Log DB..");
                    ShowWaitFormGridMessage(4, Counter, "Failed", 3, false);
                    Success = false;
                }
            }
            AddScanQueueGridRows(ScanQueueRows);
            //Config.ScanCount = StartID;
            Config.ScanCount = IronDB.GetLastScanJobRowId();
            //int ScanCount = 1;
            //while (ScanCount <= StartID)
            //{
            //    try
            //    {
            //        //Scanner Scan = IronDB.GetScannerFromDB(ScanCount);
            //        //ScanBranch.CanScan(Scan.OriginalRequest);
            //    }
            //    catch { }
            //    finally { ScanCount++; }
            //}

            if(Success) ShowWaitFormGridMessage(4, Counter, "Done", 1, false);

            //WF.WaitFormProgressBar.PerformStep();
            //StepWaitFormProgressBar();

            StartID = 0;
            Counter = 0;
            //WF.Text = "Updating Sitemap...";
            ShowWaitFormGridMessage(5, Counter, "In Progress", 2, true);
            Success = true;
            ShowWaitFormMessage("Updating ScanTrace Messages...");
            ClearAllTraceGridRows();
            List<IronTrace> AllScanTraces = new List<IronTrace>();
            List<IronTrace> ScanTraces = new List<IronTrace>();
            try
            {
                ScanTraces = IronDB.GetScanTraces(StartID, IronLog.MaxRowCount);
            }
            catch
            {
                ShowWaitFormMessage("Error ScanTrace Log DB..");
                ShowWaitFormGridMessage(5, 0, "Failed", 3, false);
                Success = false;
            }
            if (ScanTraces.Count > 0)
            {
                Counter = Counter + ScanTraces.Count;
                foreach (IronTrace Trace in ScanTraces)
                {
                    if (Trace.ID > StartID) StartID = Trace.ID;
                }
                //UpdatePluginResultTree(PluginResultLogRecords);
                AllScanTraces.AddRange(ScanTraces);
                ShowWaitFormGridMessage(5, Counter, "In Progress", 2, true);
                //try
                //{
                //    ScanTraces = IronDB.GetScanTraceRecords(StartID);
                //}
                //catch
                //{
                //    ShowWaitFormMessage("Error Trace Log DB..");
                //    ShowWaitFormGridMessage(5, Counter, "Failed", 3, false);
                //    Success = false;
                //}
            }
            SetScanTraceGrid(AllScanTraces);
            //Config.ScanTraceCount = StartID;
            Config.ScanTraceCount = IronDB.GetLastScanTraceLogRowId();
            Config.SessionPluginTraceCount = IronDB.GetLastSessionPluginTraceLogRowId();
            if(Success) ShowWaitFormGridMessage(5, Counter, "Done", 1, false);

            //WF.Text = "Updating Automated Scanning Logs..";
            Counter = 0;
            ShowWaitFormGridMessage(6, Counter, "In Progress", 2, true);
            Success = true;
            ShowWaitFormMessage("Updating Automated Scanning Logs..");
            //Thread.Sleep(500);

            StartID = 0;
            List<object[]> ScanRows = new List<object[]>();
            List<LogRow> ScanLogRecords = new List<LogRow>();

            ClearAllScanGridRows();
            try
            {
                ScanLogRecords = IronDB.GetRecordsFromScanLog(0, IronLog.MaxRowCount); //.GetScanLogRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading Scan Log DB..");
                ShowWaitFormGridMessage(6, 0, "Failed", 3, false);
                Success = false;
            }
            if (ScanLogRecords.Count > 0)
            {
                Counter = Counter + ScanLogRecords.Count;
                foreach (LogRow Fields in ScanLogRecords)
                {
                    if (Fields.ID > StartID) StartID = Fields.ID;
                    ScanRows.Add(Fields.ToScanGridRowObjectArray());
                }
                ShowWaitFormGridMessage(6, Counter, "In Progress", 2, true);
                //try
                //{
                //    ScanLogRecords = IronDB.GetScanLogRecords(StartID);
                //}
                //catch
                //{
                //    ShowWaitFormMessage("Error reading Scan Log DB..");
                //    ShowWaitFormGridMessage(6, Counter, "Failed", 3, false);
                //    Success = false;
                //}
            }
            SetScanGridRows(ScanRows);
            //Config.PluginRequestsCount = StartID;
            Config.ScanRequestsCount = IronDB.GetLastScanLogRowId();
            if(Success) ShowWaitFormGridMessage(6, Counter, "Done", 1, false);


            //Probe Log
            Counter = 0;
            ShowWaitFormGridMessage(7, Counter, "In Progress", 2, true);
            Success = true;
            ShowWaitFormMessage("Updating Probe Logs..");
            //Thread.Sleep(500);

            StartID = 0;
            List<object[]> ProbeRows = new List<object[]>();
            List<LogRow> ProbeLogRecords = new List<LogRow>();

            ClearAllProbeGridRows();
            try
            {
                ProbeLogRecords = IronDB.GetRecordsFromProbeLog(0, IronLog.MaxRowCount); //.GetProbeLogRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading Probe Log DB..");
                ShowWaitFormGridMessage(7, 0, "Failed", 3, false);
                Success = false;
            }

            if (ProbeLogRecords.Count > 0)
            {
                Counter = Counter + ProbeLogRecords.Count;
                foreach (LogRow Fields in ProbeLogRecords)
                {
                    if (Fields.ID > StartID) StartID = Fields.ID;
                    ProbeRows.Add(Fields.ToProbeGridRowObjectArray());
                    if (Fields.Code == 200)
                    {
                        try
                        {
                            Request Req = new Request("http://" + Fields.Host + Fields.Url);
                            Urls.Add(IronUpdater.GetUrlForList(Req));
                        }
                        catch
                        {
                            //IronException.Report("Error creating Request from ProxyLogRow", Exp.Message, Exp.StackTrace);
                        }
                    }
                }
                ShowWaitFormGridMessage(7, Counter, "In Progress", 2, true);
                //try
                //{
                //    ProbeLogRecords = IronDB.GetProbeLogRecords(StartID);
                //}
                //catch
                //{
                //    ShowWaitFormMessage("Error reading Probe Log DB..");
                //    ShowWaitFormGridMessage(7, Counter, "Failed", 3, false);
                //    Success = false;
                //}
            }
            SetProbeGridRows(ProbeRows);
            //Config.ProbeRequestsCount = StartID;
            Config.ProbeRequestsCount = IronDB.GetLastProbeLogRowId();
            if (Success) ShowWaitFormGridMessage(7, Counter, "Done", 1, false);


            //WF.WaitFormProgressBar.PerformStep();
            //StepWaitFormProgressBar();
            //WF.Text = "Updating Plugin Results Information...";
            Counter = 0;
            ShowWaitFormGridMessage(8, Counter, "In Progress", 2, true);
            Success = true;
            ShowWaitFormMessage("Updating Plugin Results Information...");
            //Thread.Sleep(500);

            //UI.IronTree.Nodes.Clear();
            ResetIronTree();
            //BuildIronTree();

            StartID = 0;
            List<Finding> AllPluginResultLogRecords = new List<Finding>();
            List<Finding> PluginResultLogRecords = new List<Finding>();

            try
            {
                PluginResultLogRecords = IronDB.GetPluginResultsLogRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading PluginResult Log DB..");
                ShowWaitFormGridMessage(8, 0, "Failed", 3, false);
                Success = false;
            }
            while (PluginResultLogRecords.Count > 0)
            {
                Counter = Counter + PluginResultLogRecords.Count;
                foreach (Finding PR in PluginResultLogRecords)
                {
                    if (PR.Id > StartID) StartID = PR.Id;
                    Finding.IsSignatureUnique(PR.Plugin, PR.AffectedHost, PR.Type, PR.Signature, true);
                }

                AllPluginResultLogRecords.AddRange(PluginResultLogRecords);
                ShowWaitFormGridMessage(8, Counter, "In Progress", 2, true);
                try
                {
                    PluginResultLogRecords = IronDB.GetPluginResultsLogRecords(StartID);
                }
                catch
                {
                    ShowWaitFormMessage("Error reading PluginResult Log DB..");
                    ShowWaitFormGridMessage(8, Counter, "Failed", 3, false);
                    Success = false;
                }
            }

            UpdatePluginResultTree(AllPluginResultLogRecords);
            //Config.PluginResultCount = StartID;
            Config.PluginResultCount = IronDB.GetLastPluginResultLogRowId();
            if (Success) ShowWaitFormGridMessage(8, Counter, "Done", 1, false);
            

            //WF.WaitFormProgressBar.PerformStep();
            //StepWaitFormProgressBar();
            Counter = 0;
            ShowWaitFormGridMessage(9, Counter, "In Progress", 2, true);
            Success = true;
            //WF.Text = "Exceptions Information...";
            ShowWaitFormMessage("Updating Exceptions Information...");
            //Thread.Sleep(500);

            ////////////////////////////////////////////

            StartID = 0;
            List<IronException> AllExceptionLogRecords = new List<IronException>();
            List<IronException> ExceptionLogRecords = new List<IronException>();
            try
            {
                ExceptionLogRecords = IronDB.GetExceptionLogRecords(StartID);
            }
            catch
            {
                ShowWaitFormMessage("Error reading Exception Log DB..");
                ShowWaitFormGridMessage(9, 0, "Failed", 3, false);
                Success = false;
            }
            while (ExceptionLogRecords.Count > 0)
            {
                Counter = Counter + ExceptionLogRecords.Count;
                foreach (IronException IrEx in ExceptionLogRecords)
                {
                    if (IrEx.ID > StartID) StartID = IrEx.ID;
                }
                AllExceptionLogRecords.AddRange(ExceptionLogRecords);
                ShowWaitFormGridMessage(9, Counter, "In Progress", 2, true);
                try
                {
                    ExceptionLogRecords = IronDB.GetExceptionLogRecords(StartID);
                }
                catch
                {
                    ShowWaitFormMessage("Error reading Exception Log DB..");
                    ShowWaitFormGridMessage(9, Counter, "Failed", 3, false);
                    Success = false;
                }
            }
            //Config.ExceptionsCount = StartID;
            Config.ExceptionsCount = IronDB.GetLastExceptionLogRowId();
            UpdateExceptions(AllExceptionLogRecords);
            if(Success) ShowWaitFormGridMessage(9, Counter, "Done", 1, false);
            Success = false;
            //WF.WaitFormProgressBar.PerformStep();
            //StepWaitFormProgressBar();
            //WF.Text = "Updating Sitemap...";
            Counter = 0;
            ShowWaitFormGridMessage(10, Counter, "In Progress", 2, true);
            Success = true;
            ShowWaitFormMessage("Updating Sitemap...");
            UpdateSitemapTree(Urls);
            if(Success) ShowWaitFormGridMessage(10, Urls.Count, "Done", 1, false);
            

            //StepWaitFormProgressBar();
            StartID = 0;
            Counter = 0;
            //WF.Text = "Updating Sitemap...";
            ShowWaitFormGridMessage(11, Counter, "In Progress", 2, true);
            Success = true;
            ShowWaitFormMessage("Updating Trace Messages...");
            ClearAllTraceGridRows();
            List<IronTrace> AllTraces = new List<IronTrace>();
            List<IronTrace> Traces = new List<IronTrace>();
            try
            {
                Traces = IronDB.GetTraceRecords(StartID, 1000);
            }
            catch
            {
                ShowWaitFormMessage("Error Trace Log DB..");
                ShowWaitFormGridMessage(11, 0, "Failed", 3, false);
                Success = false;
            }
            while (Traces.Count > 0)
            {
                Counter = Counter + Traces.Count;
                foreach (IronTrace Trace in Traces)
                {
                    if (Trace.ID > StartID) StartID = Trace.ID;
                }
                AllTraces.AddRange(Traces);
                ShowWaitFormGridMessage(11, Counter, "In Progress", 2, true);
                try
                {
                    Traces = IronDB.GetTraceRecords(StartID, 1000);
                }
                catch
                {
                    ShowWaitFormMessage("Error Trace Log DB..");
                    ShowWaitFormGridMessage(11, Counter, "Failed", 3, false);
                    Success = false;
                }
            }
            UpdateTraceGrid(AllTraces);
            //Config.TraceCount = StartID;
            Config.TraceCount = IronDB.GetLastTraceLogRowId();
            if(Success) ShowWaitFormGridMessage(11, Counter, "Done", 1, false);
            
            StartID = 0;


            IronProxy.Start();

            //Thread.Sleep(500);

            //WF.WaitFormProgressBar.PerformStep();
            StepWaitFormProgressBar();
            //WF.Text = "Complete";
            ShowWaitFormMessage("Complete");
            //Thread.Sleep(500);

            StopUpdatingFullUIFromDB();
            //Thread.Sleep(5000);
        }

        internal static void DisplayPluginResultsTrigger(int TriggerID)
        {
            ResetPluginResultsFields();
            Trigger SelectedTrigger;
            if (TriggerID == -1)
            {
                if (Finding.CurrentPluginResult.BaseRequest != null && Finding.CurrentPluginResult.BaseResponse != null)
                {
                    SelectedTrigger = new Trigger("", Finding.CurrentPluginResult.BaseRequest, "", Finding.CurrentPluginResult.BaseResponse);
                }
                else
                {
                    return;
                }
            }
            else
            {
                SelectedTrigger = Finding.CurrentPluginResult.Triggers.GetTrigger(TriggerID);
            }
            if (SelectedTrigger.Request != null)
            {
                DisplayPluginResultsRequest(SelectedTrigger.Request);
                if (SelectedTrigger.Response != null)
                {
                    DisplayPluginResultsResponse(SelectedTrigger.Response, SelectedTrigger.Request);
                }
            }
            
            if (SelectedTrigger.Request != null || SelectedTrigger.Response != null)
            {
                UI.ResultsTriggerOptionsPanel.Visible = true;
            }
            if (UI.SelectForDiffTriggersGrid.Rows.Count > 1)
            {
                UI.ResultsTriggersComparePanel.Visible = true;
            }
            if (Finding.CurrentPluginResult.FromActiveScan)
            {
                UI.ResultsScanTracePanel.Visible = true;
            }

            StringBuilder SB = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;\red247\green150\blue70;\red255\green0\blue0;\red0\green200\blue50;}");
            SB.AppendLine(@" \fs20 ");
            SB.AppendLine(Tools.RtfSafe(Finding.GetTriggerHighlighting(SelectedTrigger, Finding.CurrentPluginResult.FinderType, TriggerID == -1)));
            SB.Append(@" \par \par");
            UI.TriggerHighlightRTB.Rtf = SB.ToString();
        }

        internal static void DisplayPluginResultsRequest(Request Req)
        {
            if (Req != null)
            {
                UI.ResultsRequestView.SetRequest(Req);
            }
        }
        internal static void DisplayPluginResultsResponse(Response Res, Request Req)
        {
            if (Res != null)
            {
                UI.ResultsResponseView.SetResponse(Res, Req);
            }
        }
        internal static void ResetPluginResultsFields()
        {
            UI.TriggerHighlightRTB.Text = "";
            UI.ResultsRequestView.ClearRequest();
            UI.ResultsResponseView.ClearResponse();
            UI.ResultsTriggerOptionsPanel.Visible = false;
            UI.ResultsScanTracePanel.Visible = false;
            UI.ResultsTriggersComparePanel.Visible = false;
        }

        delegate void UpdateException_d(IronException IrEx);
        internal static void UpdateException(IronException IrEx)
        {
            if (UI.IronTree.InvokeRequired)
            {
                UpdateException_d UE_d = new UpdateException_d(UpdateException);
                UI.Invoke(UE_d, new object[] { IrEx });
            }
            else
            {
                TreeNode ExceptionsNode = UI.IronTree.Nodes[0].Nodes["Exceptions"];
                ExceptionsNode.Nodes.Add(IrEx.ID.ToString(), IrEx.Title);
                ExceptionsNode.Text = string.Format("Exceptions ({0})", ExceptionsNode.Nodes.Count.ToString());
            }
        }

        delegate void UpdateExceptions_d(List<IronException> IrExs);
        internal static void UpdateExceptions(List<IronException> IrExs)
        {
            if (UI.IronTree.InvokeRequired)
            {
                UpdateExceptions_d UE_d = new UpdateExceptions_d(UpdateExceptions);
                UI.Invoke(UE_d, new object[] { IrExs });
            }
            else
            {
                UI.IronTree.BeginUpdate();
                TreeNode ExceptionsNode = UI.IronTree.Nodes[0].Nodes["Exceptions"];
                foreach (IronException IrEx in IrExs)
                {
                    ExceptionsNode.Nodes.Add(IrEx.ID.ToString(), IrEx.Title);
                }
                ExceptionsNode.Text = string.Format("Exceptions ({0})", ExceptionsNode.Nodes.Count.ToString());
                UI.IronTree.EndUpdate();
            }
        }

        internal static void UpdateShellInPrompt(string Prompt)
        {
            UI.InteractiveShellPromptBox.ReadOnly = false;
            UI.InteractiveShellPromptBox.Text = Prompt;
            UI.InteractiveShellPromptBox.ReadOnly = true;
        }

        internal static void StartMTSend(int ID)
        {
            UI.TestIDLbl.Text = "ID: " + ID.ToString();
            UI.TestResponseView.ShowStatusMsg("Waiting for Response");
            UI.TestResponseView.ShowProgressBar(true);
            //UI.MTReqResTabs.SelectTab("MTResponseTab");
            UI.MTResponseSideTabs.SelectTab("MTResponseTab");
        }

        internal static void EndMTSend(bool ShowResponseTab)
        {
            UI.MTScriptedSendBtn.Enabled = ManualTesting.ScriptedSendEnabled;
            UI.MTSendBtn.Enabled = true;
            if(ShowResponseTab)
                UI.MTResponseSideTabs.SelectTab("MTResponseTab");
                //UI.MTReqResTabs.SelectTab("MTResponseTab");
            //else
            //    UI.MTReqResTabs.SelectTab("MTRequestTab");
        }

        delegate void AskUser_d();
        internal static void AskUser()
        {
            if(UI.InvokeRequired)
            {
                AskUser_d AU_d = new AskUser_d(AskUser);
                UI.Invoke(AU_d, new object[] { });
            }
            else
            {
                if (IsAskUserWindowOpen()  &&  !IronWASP.AskUser.AskUserWindowFree)
                {
                    IronUI.AUW.Text = "1/" + (IronWASP.AskUser.QueueLength + 1 ).ToString() + "   " + IronWASP.AskUser.CurrentlyAsked.Title;
                    return;
                }
                AskUser AU = IronWASP.AskUser.GetNext();
                IronWASP.AskUser.CurrentlyAsked = AU;
                if (AU == null)
                {
                    return;
                }
                else
                {
                    if (!IsAskUserWindowOpen())
                    {
                        IronUI.AUW = new AskUserWindow();
                    }
                    IronUI.AUW.Text = "IronWASP AskUser API Call - 1/" + (IronWASP.AskUser.QueueLength + 1).ToString();// +"   " + AU.Title;
                    StringBuilder Message = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;\red247\green150\blue70;\red255\green0\blue0;\red0\green200\blue50;} \qc \fs22 \cf1 \b ");
                    Message.Append(Tools.RtfSafe(AU.Title));
                    Message.Append(@" \b0  \cf0 \par \pard \ql \fs18 \par ");
                    Message.Append(Tools.RtfSafe(AU.Message));
                    IronUI.AUW.AskUserMessageRTB.Rtf = Message.ToString();
                    IronUI.AUW.Show();
                    IronWASP.AskUser.AskUserWindowFree = false;
                    if (AU.ReturnType.Equals("Bool"))
                    {
                        IronUI.AUW.Height = 350;
                        IronUI.AUW.AskUserYesBtn.Visible = true;
                        IronUI.AUW.AskUserNoBtn.Visible = true;
                        IronUI.AUW.AskUserAnswerTB.Visible = false;
                        IronUI.AUW.AskUserAnswerGrid.Visible = false;
                        IronUI.AUW.AskUserAnswerRBOne.Visible = false;
                        IronUI.AUW.AskUserAnswerRBTwo.Visible = false;
                        IronUI.AUW.AskUserAnswerLbl.Visible = false;
                        IronUI.AUW.AskUserSubmitBtn.Visible = false;
                        IronUI.AUW.AskUserPB.Visible = false;
                        IronUI.AUW.AskUserYesBtn.Focus();
                    }
                    else if (AU.ReturnType.Equals("List"))
                    {
                        IronUI.AUW.Height = 600;
                        foreach (string ListItem in AU.List)
                        {
                            IronUI.AUW.AskUserAnswerGrid.Rows.Add(new object[] { false, ListItem });
                        }
                        if (AU.RBOne.Length + AU.RBTwo.Length + AU.Label.Length > 0)
                        {
                            IronUI.AUW.AskUserAnswerRBOne.Text = AU.RBOne;
                            IronUI.AUW.AskUserAnswerRBTwo.Text = AU.RBTwo;
                            IronUI.AUW.AskUserAnswerRBTwo.Checked = true;
                            IronUI.AUW.AskUserAnswerLbl.Text = AU.Label;
                            
                            IronUI.AUW.AskUserAnswerRBOne.Visible = true;
                            IronUI.AUW.AskUserAnswerRBTwo.Visible = true;
                            IronUI.AUW.AskUserAnswerLbl.Visible = true;

                            IronUI.AUW.AskUserAnswerGrid.Location = new Point(0,57);
                        }
                        else
                        {
                            IronUI.AUW.AskUserAnswerRBOne.Visible = false;
                            IronUI.AUW.AskUserAnswerRBTwo.Visible = false;
                            IronUI.AUW.AskUserAnswerLbl.Visible = false;
                            IronUI.AUW.AskUserAnswerGrid.Location = new Point(0, 0);
                        }
                        
                        IronUI.AUW.AskUserAnswerGrid.Visible = true;
                        IronUI.AUW.AskUserSubmitBtn.Visible = true;
                        IronUI.AUW.AskUserAnswerTB.Visible = false;
                        IronUI.AUW.AskUserYesBtn.Visible = false;
                        IronUI.AUW.AskUserNoBtn.Visible = false;
                        IronUI.AUW.AskUserPB.Visible = false;
                        IronUI.AUW.AskUserAnswerGrid.Focus();
                    }
                    else
                    {
                        IronUI.AUW.Height = 350;
                        IronUI.AUW.AskUserAnswerTB.Visible = true;
                        IronUI.AUW.AskUserAnswerGrid.Visible = false;
                        IronUI.AUW.AskUserAnswerRBOne.Visible = false;
                        IronUI.AUW.AskUserAnswerRBTwo.Visible = false;
                        IronUI.AUW.AskUserAnswerLbl.Visible = false;
                        IronUI.AUW.AskUserSubmitBtn.Visible = true;
                        IronUI.AUW.AskUserYesBtn.Visible = false;
                        IronUI.AUW.AskUserNoBtn.Visible = false;
                        if (AU.ImageFileLocation.Length > 0)
                        {
                            IronUI.AUW.AskUserPB.ImageLocation = AU.ImageFileLocation;
                            IronUI.AUW.AskUserPB.Visible = true;
                        }
                        else
                        {
                            IronUI.AUW.AskUserPB.Visible = false;
                        }
                        IronUI.AUW.AskUserAnswerTB.Focus();
                    }
                }
            }
        }

        static bool IsAskUserWindowOpen()
        {
            if (IronUI.AUW == null)
            {
                return false;
            }
            else if (IronUI.AUW.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        delegate void AskUserAnswered_d();
        internal static void AskUserAnswered()
        {
            if (UI.InvokeRequired)
            {
                AskUserAnswered_d AUA_d = new AskUserAnswered_d(AskUserAnswered);
                UI.Invoke(AUA_d, new object[] { });
            }
            else
            {
                IronWASP.AskUser.AskUserWindowFree = true;
                if (IronWASP.AskUser.QueueLength == 0)
                {
                    if (IronUI.AUW != null)
                    {
                        if (!IronUI.AUW.IsDisposed)
                        {
                            //IronUI.AUW.Close();
                            IronUI.AUW.Dispose();
                        }
                    }
                }
                else
                {
                    IronUI.AskUser();
                }
            }
        }

        internal static bool IsScanBranchFormOpen()
        {
            if (IronUI.SBF == null)
            {
                return false;
            }
            else if (IronUI.SBF.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //internal static void ShowScanBranchForm(string HostName, string UrlPattern)
        internal static void ShowScanBranchForm(Request UrlReq)
        {
            if (IsScanBranchFormOpen())
            {
                IronUI.SBF.Activate();
            }
            else
            {
                IronUI.SBF = new ScanBranchWizard();
                IronUI.SBF.BaseRequest = UrlReq;
                IronUI.SBF.ScanBranchHostNameTB.Text = UrlReq.BaseUrl;
                IronUI.SBF.ScanBranchUrlPatternTB.Text = UrlReq.Url;

                IronUI.SBF.ScanBranchStatsPanel.Visible = false;

                IronUI.SBF.Show();
            }
        }

        delegate void UpdateScanBranchStats_d(int ScanDone, int TotalScans, string Message, bool Progress, bool CloseWindow);
        internal static void UpdateScanBranchStats(int ScanDone, int TotalScans, string Message, bool Progress, bool CloseWindow)
        {
            if (IronUI.SBF.InvokeRequired)
            {
                UpdateScanBranchStats_d USBS_d = new UpdateScanBranchStats_d(UpdateScanBranchStats);
                UI.Invoke(USBS_d, new object[] { ScanDone, TotalScans , Message, Progress, CloseWindow});
            }
            else
            {
                if (Progress) IronUI.SBF.ScanBranchProgressBar.PerformStep();
                IronUI.SBF.ScanBranchProgressLbl.Text = Message;
                if (ScanDone == TotalScans) IronUI.SBF.FinalBtn.Text = "Close";
                if (CloseWindow) IronUI.SBF.CloseWindow();
            }
        }

        

        internal static bool IsStartScanWizardOpen()
        {
            if (IronUI.SSW == null)
            {
                return false;
            }
            else if (IronUI.SSW.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static void ShowStartScanWizard(Request Req)
        {
            if (IsStartScanWizardOpen())
            {
                IronUI.SSW.Activate();
            }
            else
            {
                UI.ConsoleScanUrlTB.ReadOnly = true;
                IronUI.SSW = new StartScanWizard();
                IronUI.SSW.BaseRequest = Req;
                IronUI.SSW.Show();
            }
        }

        //delegate void ShowConfiguredScanMessage_d(string Message, bool Error);
        //internal static void ShowConfiguredScanMessage(string Message, bool Error)
        //{
        //    if (SSW.InvokeRequired)
        //    {
        //        ShowConfiguredScanMessage_d SCSM_d = new ShowConfiguredScanMessage_d(ShowConfiguredScanMessage);
        //        LF.Invoke(SCSM_d, new object[] { Message, Error });
        //    }
        //    else
        //    {
        //        if (Message.Equals("0"))
        //        {
        //            SSW.Close();
        //        }
        //        else
        //        {
        //            SSW.ConfigureScanErrorTB.Text = Message;
        //            if (Error)
        //                SSW.ConfigureScanErrorTB.ForeColor = Color.Red;
        //            else
        //                SSW.ConfigureScanErrorTB.ForeColor = Color.Black;
        //        }
        //    }
        //}


        internal static bool IsReportGenerationWizardOpen()
        {
            if (IronUI.RGW == null)
            {
                return false;
            }
            else if (IronUI.RGW.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static bool IsPluginEditorOpen()
        {
            if (IronUI.PE == null)
            {
                return false;
            }
            else if (IronUI.PE.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static bool IsUIDesignerOpen()
        {
            if (IronUI.UD == null)
            {
                return false;
            }
            else if (IronUI.UD.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        delegate void ShowPluginCompilerError_d(string Message);
        internal static void ShowPluginCompilerError(string Message)
        {
            if (PE.InvokeRequired)
            {
                ShowPluginCompilerError_d SPCE_d = new ShowPluginCompilerError_d(ShowPluginCompilerError);
                PE.Invoke(SPCE_d, new object[] { Message });
            }
            else
            {
                if (Message.Equals("0"))
                {
                    IronUI.PE.PluginEditorErrorTB.Text = "";
                    IronUI.PE.PluginEditorErrorTB.BackColor = Color.White;
                }
                else
                {
                    IronUI.PE.PluginEditorErrorTB.Text = Message + Environment.NewLine.ToString();
                    IronUI.PE.PluginEditorErrorTB.BackColor = Color.Red;
                }
            }
        }

        internal static void OpenDiffWindow()
        {
            if (!IsDiffWindowOpen())
            {
                IronUI.DW = new DiffWindow();
                IronUI.DW.Show();
            }
            IronUI.DW.Activate();
        }

        internal static bool IsDiffWindowOpen()
        {
            if (IronUI.DW == null)
            {
                return false;
            }
            else if (IronUI.DW.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static void OpenEncodeDecodeWindow()
        {
            if (!IsEncodeDecodeWindowOpen())
            {
                IronUI.EDW = new EncodeDecodeWindow();
                IronUI.EDW.Show();
            }
            IronUI.EDW.Activate();
        }

        internal static bool IsEncodeDecodeWindowOpen()
        {
            if (IronUI.EDW == null)
            {
                return false;
            }
            else if (IronUI.EDW.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        delegate void ShowDiffResults_d(string Status, string SideBySideSource, string SideBySideDestination, string SinglePage);
        internal static void ShowDiffResults(string Status, string SideBySideSource, string SideBySideDestination, string SinglePage)
        {
            if (DW.InvokeRequired)
            {
                ShowDiffResults_d SDR_d = new ShowDiffResults_d(ShowDiffResults);
                DW.Invoke(SDR_d, new object[] { Status, SideBySideSource, SideBySideDestination, SinglePage });
            }
            else
            {
                IronUI.DW.DiffResultRTB.Text = "";
                IronUI.DW.SourceResultRTB.Text = "";
                IronUI.DW.DestinationResultRTB.Text = "";
                
                IronUI.DW.DiffResultRTB.Rtf = SinglePage;
                IronUI.DW.SourceResultRTB.Rtf = SideBySideSource;                
                IronUI.DW.DestinationResultRTB.Rtf = SideBySideDestination;

                IronUI.DW.DiffStatusTB.Text = Status;

                if (Status.Length == 0 || Status.StartsWith("Done. Diff Level - ")) IronUI.DW.BaseTabs.SelectedIndex = 1;
                IronUI.DW.DiffWindowShowDiffBtn.Enabled = true;
            }
        }

        internal static void OpenImportForm()
        {
            if (!IsImportFormOpen())
            {
                IronUI.IF = new ImportForm();
                IronUI.IF.Show();
            }
            IronUI.IF.Activate();
        }

        internal static bool IsImportFormOpen()
        {
            if (IronUI.IF == null)
            {
                return false;
            }
            else if (IronUI.IF.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        delegate void CloseImportForm_d();
        internal static void CloseImportForm()
        {
            if (IF.InvokeRequired)
            {
                CloseImportForm_d CIF_d = new CloseImportForm_d(CloseImportForm);
                IF.Invoke(CIF_d, new object[] { });
            }
            else
            {
                IronUI.IF.Close();
            }
        }

        internal static bool IsCloseFormOpen()
        {
            if (IronUI.CF == null)
            {
                return false;
            }
            else if (IronUI.CF.IsDisposed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        delegate void LogGridStatus_d(bool Show);
        internal static void LogGridStatus(bool Show)
        {
            if (UI.InvokeRequired)
            {
                LogGridStatus_d LGS_d = new LogGridStatus_d(LogGridStatus);
                UI.Invoke(LGS_d, new object[] { Show });
            }
            else
            {
                if (Show)
                {
                    UI.ShowLogGridBtn.Text = "Hide Log Grids";
                    UI.LogBaseSplit.SplitterDistance = (UI.LogBaseSplit.Height / 2);
                    
                }
                else
                {
                    UI.ShowLogGridBtn.Text = "Show Log Grids";
                    if (UI.LogBaseSplit.Height > 50) UI.LogBaseSplit.SplitterDistance = UI.LogBaseSplit.Height;
                }
            }
        }

        internal static void ResetLogStatus()
        {
            UI.LogStatusTB.Text = "";
            UI.LogStatusTB.Visible = false;
        }

        internal static void ResetLogDisplayFields()
        {
            ResetLogRequestDisplayFields();
            ResetLogResponseDisplayFields();
            ResetLogStatus();
        }

        internal static void ResetLogRequestDisplayFields()
        {
            UI.LogRequestView.ClearRequest();
            //UI.LogRequestHeadersIDV.Text = "";
            //UI.LogRequestBodyIDV.Text = "";
            //UI.LogRequestParametersQueryGrid.Rows.Clear();
            //UI.LogRequestParametersBodyGrid.Rows.Clear();
            //UI.LogRequestParametersCookieGrid.Rows.Clear();
            //UI.LogRequestParametersHeadersGrid.Rows.Clear();
            UI.ProxyShowOriginalRequestCB.Checked = false;
            //UI.LogRequestFormatXMLTB.Text = "";
        }

        internal static void ResetLogResponseDisplayFields()
        {
            UI.LogResponseView.ClearResponse();
            //UI.LogResponseHeadersIDV.Text = "";
            //UI.LogResponseBodyIDV.Text = "";
            UI.ProxyShowOriginalResponseCB.Checked = false;
            //UI.LogResponseFormatXMLTB.Text = "";
            //UI.LogReflectionRTB.Text = "";
        }

        delegate void ShowLogStatus_d(string Message, bool Error);
        internal static void ShowLogStatus(string Message, bool Error)
        {
            if (UI.LogStatusTB.InvokeRequired)
            {
                ShowLogStatus_d SLS_d = new ShowLogStatus_d(ShowLogStatus);
                UI.Invoke(SLS_d, new object[] { Message, Error });
            }
            else
            {
                if (Error)
                {
                    UI.LogStatusTB.ForeColor = Color.Red;
                }
                else
                {
                    UI.LogStatusTB.ForeColor = Color.Black;
                }
                UI.LogStatusTB.Text = Message;
                UI.LogStatusTB.Visible = true;
            }
        }

        delegate void ShowLogBottomStatus_d(string Message, bool Error);
        internal static void ShowLogBottomStatus(string Message, bool Error)
        {
            if (UI.MainLogStatusLbl.InvokeRequired)
            {
                ShowLogBottomStatus_d SLBS_d = new ShowLogBottomStatus_d(ShowLogBottomStatus);
                UI.Invoke(SLBS_d, new object[] { Message, Error });
            }
            else
            {
                if (Error)
                {
                    UI.MainLogStatusLbl.ForeColor = Color.Red;
                }
                else
                {
                    UI.MainLogStatusLbl.ForeColor = Color.Black;
                }
                if (Message.Length == 0)
                {
                    UI.MainLogStatusLbl.ForeColor = Color.Black;
                    UI.MainLogStatusLbl.Text = IronLog.MainLogDefaultMsg;
                }
                else
                {
                    UI.MainLogStatusLbl.Text = Message;
                }
                UI.MainLogStatusLbl.Visible = true;
            }
        }

        delegate void ShowCurrentLogStat_d();
        internal static void ShowCurrentLogStat()
        {
            if (UI.MainLogStatLbl.InvokeRequired)
            {
                ShowCurrentLogStat_d SCLS_d = new ShowCurrentLogStat_d(ShowCurrentLogStat);
                UI.Invoke(SCLS_d, new object[] { });
            }
            else
            {
                switch (UI.LogTabs.SelectedTab.Name)
                {
                    case ("ProxyLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing {0} - {1} of Proxy Logs", IronLog.ProxyMin, IronLog.ProxyMax);
                        break;
                    case ("ScanLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing {0} - {1} of Scan Logs", IronLog.ScanMin, IronLog.ScanMax);
                        break;
                    case ("TestLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing {0} - {1} of Test Logs", IronLog.TestMin, IronLog.TestMax);
                        break;
                    case ("ShellLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing {0} - {1} of Shell Logs", IronLog.ShellMin, IronLog.ShellMax);
                        break;
                    case ("ProbeLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing {0} - {1} of Probe Logs", IronLog.ProbeMin, IronLog.ProbeMax);
                        break;
                    case ("OtherLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing {0} - {1} of {2} Logs", IronLog.OtherSourceMin, IronLog.OtherSourceMax, IronLog.SelectedOtherSource);
                        break;
                    case ("SiteMapLogTab"):
                        UI.MainLogStatLbl.Text = string.Format("Showing Logs based on SiteMap");
                        break;
                }
            }
        }

        delegate void FillLogDisplayFields_d(Session IrSe);
        internal static void FillLogDisplayFields(Session IrSe)
        {
            if (UI.LogDisplayTabs.InvokeRequired)
            {
                FillLogDisplayFields_d FLDF_d = new FillLogDisplayFields_d(FillLogDisplayFields);
                UI.Invoke(FLDF_d, new object[] { IrSe });
            }
            else
            {
                if (IrSe == null) return;
                if (IrSe.Request != null) FillLogFields(IrSe.Request);
                if (IrSe.Response != null) FillLogFields(IrSe.Response, IrSe.Request);
                //FillLogReflection(Reflection);
                try
                {
                    UI.LogSourceLbl.Text = "Source: " + IronLog.CurrentSourceName;
                    UI.LogIDLbl.Text = "ID: " + IronLog.CurrentID.ToString();
                }
                catch { }
                UI.ProxyShowOriginalRequestCB.Checked = false;
                UI.ProxyShowOriginalResponseCB.Checked = false;
                UI.ProxyShowOriginalRequestCB.Visible = IrSe.OriginalRequest != null;
                UI.ProxyShowOriginalResponseCB.Visible = IrSe.OriginalResponse != null;
                IronUI.ResetLogStatus();
            }
        }

        internal static void FillLogFields(Request Req)
        {
            UI.LogRequestView.SetRequest(Req);
            //FillLogRequestHeaderFields(Request);
            //if (Request.HasBody)
            //{
            //    FillLogRequestBodyFields(Request);
            //}
            //else
            //{
            //    FillLogRequestBodyFields(null);
            //}
            //FillLogParametersFields(Request);
        }

        //internal static void FillLogRequestHeaderFields(Request Request)
        //{
        //    UI.LogRequestHeadersIDV.Text = Request.GetHeadersAsStringWithoutFullURL();
        //}

        //internal static void FillLogRequestBodyFields(Request Request)
        //{
        //    if (Request == null)
        //    {
        //        UI.LogRequestBodyIDV.Text = "";
        //        return;
        //    }
        //    if (Request.IsBinary)
        //    {
        //        UI.LogRequestBodyIDV.Text = Encoding.UTF8.GetString(Request.BodyArray);
        //    }
        //    else
        //    {
        //        UI.LogRequestBodyIDV.Text = Request.BodyString;
        //    }
        //}

        //internal static void FillLogParametersFields(Request Request)
        //{
        //    UI.LogRequestParametersQueryGrid.Rows.Clear();
        //    foreach (string Name in Request.Query.GetNames())
        //    {
        //        foreach (string Value in Request.Query.GetAll(Name))
        //        {
        //            UI.LogRequestParametersQueryGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.LogRequestParametersBodyGrid.Rows.Clear();
        //    foreach (string Name in Request.Body.GetNames())
        //    {
        //        foreach (string Value in Request.Body.GetAll(Name))
        //        {
        //            UI.LogRequestParametersBodyGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.LogRequestParametersCookieGrid.Rows.Clear();
        //    foreach (string Name in Request.Cookie.GetNames())
        //    {
        //        foreach (string Value in Request.Cookie.GetAll(Name))
        //        {
        //            UI.LogRequestParametersCookieGrid.Rows.Add(new object[] { Name, Value });
        //        }
        //    }
        //    UI.LogRequestParametersHeadersGrid.Rows.Clear();
        //    foreach (string Name in Request.Headers.GetNames())
        //    {
        //        if (!Name.Equals("Host", StringComparison.OrdinalIgnoreCase) && !Name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
        //        {
        //            foreach (string Value in Request.Headers.GetAll(Name))
        //            {
        //                UI.LogRequestParametersHeadersGrid.Rows.Add(new object[] { Name, Value });
        //            }
        //        }
        //    }
        //}

        //internal static void FillLogFields(Response Res)
        //{
        //    
        //    UI.LogResponseHeadersIDV.Text = Response.GetHeadersAsString();
        //    if (Response.HasBody)
        //    {
        //        if (Response.IsBinary)
        //        {
        //            UI.LogResponseBodyIDV.Text = Encoding.UTF8.GetString(Response.BodyArray);
        //        }
        //        else
        //        {
        //            UI.LogResponseBodyIDV.Text = Response.BodyString;
        //        }
        //    }
        //    else
        //    {
        //        UI.LogResponseBodyIDV.Text = "";
        //    }
        //}
        internal static void FillLogFields(Response Res, Request Req)
        {
            UI.LogResponseView.SetResponse(Res, Req);
        }

        //internal static void FillLogReflection(string Reflection)
        //{
        //    StringBuilder ReflectionBuilder = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;\red247\green150\blue70;\red255\green0\blue0;\red0\green200\blue50;}");
        //    ReflectionBuilder.Append(Tools.RtfSafe( Reflection));
        //    UI.LogReflectionRTB.Rtf = ReflectionBuilder.ToString();
        //}

        //internal static void FillTestReflection(string Reflection)
        //{
        //    StringBuilder ReflectionBuilder = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;\red247\green150\blue70;\red255\green0\blue0;\red0\green200\blue50;}");
        //    ReflectionBuilder.Append(Tools.RtfSafe(Reflection));
        //    UI.MTReflectionsRTB.Rtf = ReflectionBuilder.ToString();
        //}

        delegate void SetClipBoard_d(string Message);
        internal static void SetClipBoard(string Message)
        {
            if (UI.LogStatusTB.InvokeRequired)
            {
                SetClipBoard_d SCB_d = new SetClipBoard_d(SetClipBoard);
                UI.Invoke(SCB_d, new object[] { Message });
            }
            else
            {
                try
                {
                    Clipboard.SetText(Message);
                }
                catch { }
            }
        }

        //delegate void SetJSTaintTraceCode_d(string Code, bool RichText);
        //internal static void SetJSTaintTraceCode(string Code, bool RichText)
        //{
        //    if (UI.JSTaintTraceInRTB.InvokeRequired)
        //    {
        //        SetJSTaintTraceCode_d SJTTC_d = new SetJSTaintTraceCode_d(SetJSTaintTraceCode);
        //        UI.Invoke(SJTTC_d, new object[] { Code, RichText });
        //    }
        //    else
        //    {
        //        if (RichText)
        //        {
        //            try
        //            {
        //                UI.JSTaintTraceInRTB.Rtf = Code;
        //            }
        //            catch { UI.JSTaintTraceInRTB.Text = Code; }
        //        }
        //        else
        //        {
        //            UI.JSTaintTraceInRTB.Text = Code;
        //        }
        //        UI.JSTaintResultGrid.Rows.Clear();
        //    }
        //}

        //delegate void SetJSTaintTraceResult_d();
        //internal static void SetJSTaintTraceResult()
        //{
        //    if (UI.JSTaintResultGrid.InvokeRequired)
        //    {
        //        SetJSTaintTraceResult_d SJTTR_d = new SetJSTaintTraceResult_d(SetJSTaintTraceResult);
        //        UI.Invoke(SJTTR_d, new object[] { });
        //    }
        //    else
        //    {
        //        List<string> CodeLines = IronJint.UIIJ.Lines;
        //        List<int> Sources = IronJint.UIIJ.SourceLines;
        //        List<int> Sinks = IronJint.UIIJ.SinkLines;
        //        List<int> SourceToSinks = IronJint.UIIJ.SourceToSinkLines;
                
        //        bool ShowCleanLines = UI.JSTaintShowCleanCB.Checked;
        //        bool ShowSourceLines = UI.JSTaintShowSourceCB.Checked;
        //        bool ShowSinkLines = UI.JSTaintShowSinkCB.Checked;
        //        bool ShowSourceToSinkLines = UI.JSTaintShowSourceToSinkCB.Checked;

        //        bool LineAdded = false;
        //        bool SourceLine = false;
        //        bool SinkLine = false;
        //        bool SourceToSinkLine = false;
        //        int RowId = 0;

        //        UI.JSTaintResultGrid.Rows.Clear();
        //        IronJint.LineNoToGridRowNoMapping.Clear();

        //        for (int i = 0; i <CodeLines.Count; i++)
        //        {
        //            SourceLine = Sources.Contains(i + 1);
        //            SinkLine = Sinks.Contains(i + 1);
        //            SourceToSinkLine = SourceToSinks.Contains(i + 1);
        //            LineAdded = false;

        //            if ((ShowSourceLines || ShowSinkLines || ShowSourceToSinkLines) && SourceToSinkLine)
        //            {
        //                if (!LineAdded)
        //                {
        //                    RowId = UI.JSTaintResultGrid.Rows.Add(new object[] { i + 1, CodeLines[i] });
        //                    IronJint.LineNoToGridRowNoMapping[i + 1] = RowId;
        //                    LineAdded = true;
        //                }
        //                UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.BackColor = Color.Red;
        //                UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionBackColor = Color.Red;
        //                UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionForeColor = Color.White;
        //            }
        //            else if (SourceLine && (ShowSourceLines || ShowSinkLines))
        //            {
        //                if (!LineAdded)
        //                {
        //                    RowId = UI.JSTaintResultGrid.Rows.Add(new object[] { i + 1, CodeLines[i] });
        //                    IronJint.LineNoToGridRowNoMapping[i + 1] = RowId;
        //                    LineAdded = true;
        //                }
        //                if (SinkLine)
        //                {
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.BackColor = Color.IndianRed;
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionBackColor = Color.IndianRed;
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionForeColor = Color.White;
        //                }
        //                else if (ShowSourceLines)
        //                {
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.BackColor = Color.Orange;
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionBackColor = Color.Orange;
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionForeColor = Color.White;
        //                }
        //            }
        //            else if (SinkLine && ShowSinkLines)
        //            {
        //                if (!LineAdded)
        //                {
        //                    RowId = UI.JSTaintResultGrid.Rows.Add(new object[] { i + 1, CodeLines[i] });
        //                    IronJint.LineNoToGridRowNoMapping[i + 1] = RowId;
        //                    LineAdded = true;
        //                }
        //                UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.BackColor = Color.HotPink;
        //                UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionBackColor = Color.HotPink;
        //                UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.SelectionForeColor = Color.White;
        //            }
        //            else if (ShowCleanLines && !(SourceLine || SinkLine || SourceToSinkLine))
        //            {
        //                if (!LineAdded)
        //                {
        //                    RowId = UI.JSTaintResultGrid.Rows.Add(new object[] { i + 1, CodeLines[i] });
        //                    UI.JSTaintResultGrid.Rows[RowId].Cells[1].Style.BackColor = Color.White;
        //                    IronJint.LineNoToGridRowNoMapping[i + 1] = RowId;
        //                    LineAdded = true;
        //                }                        
        //            }
        //        }
        //        UI.JSTaintTabs.SelectTab("JSTaintResultTab");
        //    }
        //}


        //delegate void SetJSTaintTraceLine_d(string Type, int LineNo);
        //internal static void SetJSTaintTraceLine(string Type, int LineNo)
        //{
        //    if (UI.JSTaintResultGrid.InvokeRequired)
        //    {
        //        SetJSTaintTraceLine_d SJTTL_d = new SetJSTaintTraceLine_d(SetJSTaintTraceLine);
        //        UI.Invoke(SJTTL_d, new object[] { Type, LineNo });
        //    }
        //    else
        //    {
        //        if (UI.JSTaintShowCleanCB.Checked)
        //        {
        //            LineNo = IronJint.LineNoToGridRowNoMapping[LineNo];
        //        }
        //        else
        //        {
        //            if (UI.JSTaintResultGrid.Rows.Count > 0)
        //            {
        //                if (UI.JSTaintResultGrid.Rows[UI.JSTaintResultGrid.Rows.Count - 1].Cells[0].Value.ToString().Equals(LineNo.ToString()))
        //                {
        //                    LineNo = UI.JSTaintResultGrid.Rows.Count - 1;
        //                }
        //                else
        //                {
        //                    LineNo = UI.JSTaintResultGrid.Rows.Add(new object[] { LineNo, IronJint.UIIJ.Lines[LineNo - 1] });
        //                }
        //            }
        //            else
        //            {
        //                LineNo = UI.JSTaintResultGrid.Rows.Add(new object[] { LineNo, IronJint.UIIJ.Lines[LineNo - 1] });
        //            }
        //        }
        //        if(LineNo > UI.JSTaintResultGrid.Rows.Count) return;
        //        switch (Type)
        //        {
        //            case("Source"):
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.BackColor = Color.Orange;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionBackColor = Color.Orange;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionForeColor = Color.White;
        //                break;
        //            case ("Sink"):
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.BackColor = Color.HotPink;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionBackColor = Color.HotPink;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionForeColor = Color.White;
        //                break;
        //            case ("SourcePlusSink"):
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.BackColor = Color.IndianRed;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionBackColor = Color.IndianRed;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionForeColor = Color.White;
        //                break;
        //            case ("SourceToSink"):
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.BackColor = Color.Red;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionBackColor = Color.Red;
        //                UI.JSTaintResultGrid.Rows[LineNo].Cells[1].Style.SelectionForeColor = Color.White;
        //                break;
        //        }
        //        UI.JSTaintResultGrid.Rows[LineNo].Cells[0].Style.BackColor = Color.DarkBlue;
        //        UI.JSTaintResultGrid.Rows[LineNo].Cells[0].Style.SelectionBackColor = Color.DarkBlue;
        //        try
        //        {
        //            UI.JSTaintResultGrid.FirstDisplayedScrollingRowIndex = LineNo;
        //        }
        //        catch { }
        //    }
        //}

        //delegate void RemoveTaintPauseMarker_d(int LineNo);
        //internal static void RemoveTaintPauseMarker(int LineNo)
        //{
        //    if (UI.JSTaintResultGrid.InvokeRequired)
        //    {
        //        RemoveTaintPauseMarker_d RTPM_d = new RemoveTaintPauseMarker_d(RemoveTaintPauseMarker);
        //        UI.Invoke(RTPM_d, new object[] { LineNo});
        //    }
        //    else
        //    {
        //        if (!UI.JSTaintShowCleanCB.Checked) LineNo = UI.JSTaintResultGrid.Rows.Count;
        //        UI.JSTaintResultGrid.Rows[LineNo - 1].Cells[0].Style.BackColor = Color.White;
        //        UI.JSTaintResultGrid.Rows[LineNo - 1].Cells[0].Style.SelectionBackColor = Color.White;
        //    }
        //}

        //delegate void ResetTraceStatus_d();
        //internal static void ResetTraceStatus()
        //{
        //    if (UI.JSTaintContinueBtn.InvokeRequired)
        //    {
        //        ResetTraceStatus_d RTS_d = new ResetTraceStatus_d(ResetTraceStatus);
        //        UI.Invoke(RTS_d, new object[] { });
        //    }
        //    else
        //    {
        //        UI.JSTaintTraceControlBtn.Text = "Start Taint Trace";
        //        UI.PauseAtTaintCB.Visible = true;
        //        UI.JSTaintContinueBtn.Visible = false;
        //    }
        //}

        //delegate void ShowTraceStatus_d(string Message, bool Error);
        //internal static void ShowTraceStatus(string Message, bool Error)
        //{
        //    if (UI.JSTaintContinueBtn.InvokeRequired)
        //    {
        //        ShowTraceStatus_d STS_d = new ShowTraceStatus_d(ShowTraceStatus);
        //        UI.Invoke(STS_d, new object[] { Message, Error });
        //    }
        //    else
        //    {
        //        UI.JSTaintStatusTB.Text = Message;
        //        if (Error)
        //            UI.JSTaintStatusTB.ForeColor = Color.Red;
        //        else
        //            UI.JSTaintStatusTB.ForeColor = Color.Black;
        //    }
        //}

        //delegate void ShowTraceContinuteButton_d();
        //internal static void ShowTraceContinuteButton()
        //{
        //    if (UI.JSTaintContinueBtn.InvokeRequired)
        //    {
        //        ShowTraceContinuteButton_d STCB_d = new ShowTraceContinuteButton_d(ShowTraceContinuteButton);
        //        UI.Invoke(STCB_d, new object[] { });
        //    }
        //    else
        //    {
        //        UI.JSTaintContinueBtn.Visible = true;
        //    }
        //}

        //delegate void SetTaintConfig_d(List<List<string>> Lists, int MaxCount);
        //internal static void SetTaintConfig(List<List<string>> Lists, int MaxCount)
        //{
        //    if (UI.JSTaintConfigGrid.InvokeRequired)
        //    {
        //        SetTaintConfig_d STC_d = new SetTaintConfig_d(SetTaintConfig);
        //        UI.Invoke(STC_d, new object[] { Lists, MaxCount });
        //    }
        //    else
        //    {
        //        UI.JSTaintConfigGrid.Rows.Clear();
        //        for (int i = 0; i < MaxCount; i++)
        //        {
        //            UI.JSTaintConfigGrid.Rows.Add(new object[] { Lists[0][i], Lists[1][i], Lists[5][i], Lists[6][i], Lists[2][i], Lists[3][i], Lists[4][i] });
        //        }
        //    }
        //}

        //delegate void SetTaintHighlighting_d(string HighLightedCode);
        //internal static void SetTaintHighlighting(string HighLightedCode)
        //{
        //    if (UI.JSTaintTraceInRTB.InvokeRequired)
        //    {
        //        SetTaintHighlighting_d JSTTIR_d = new SetTaintHighlighting_d(SetTaintHighlighting);
        //        UI.Invoke(JSTTIR_d, new object[] { HighLightedCode });
        //    }
        //    else
        //    {
        //        StringBuilder Rtf = new StringBuilder(@"{\rtf1{\colortbl ;\red0\green77\blue187;\red247\green150\blue70;\red255\green0\blue0;\red0\green200\blue50;}");
        //        Rtf.Append(Tools.RtfSafe(HighLightedCode));
        //        UI.JSTaintTraceInRTB.Rtf = Rtf.ToString();
        //    }
        //}

        //delegate void SetTaintTraceStatus_d(string Message, bool Error);
        //internal static void SetTaintTraceStatus(string Message, bool Error)
        //{
        //    if (UI.JSTaintStatusTB.InvokeRequired)
        //    {
        //        SetTaintTraceStatus_d STTS_d = new SetTaintTraceStatus_d(SetTaintTraceStatus);
        //        UI.Invoke(STTS_d, new object[] { Message, Error });
        //    }
        //    else
        //    {
        //        if (Message.Length == 0)
        //        {
        //            UI.JSTaintStatusTB.Visible = false;
        //        }
        //        else
        //        {
        //            UI.JSTaintStatusTB.Text = Message;
        //            if (Error)
        //                UI.JSTaintStatusTB.ForeColor = Color.Red;
        //            else
        //                UI.JSTaintStatusTB.ForeColor = Color.Black;
        //            UI.JSTaintStatusTB.Visible = true;
        //        }
        //    }
        //}

        //delegate void ShowTaintReasons_d(int LineNo, List<string> SourceReasons, List<string> SinkReasons);
        //internal static void ShowTaintReasons(int LineNo, List<string> SourceReasons, List<string> SinkReasons)
        //{
        //    if (UI.JSTaintReasonsRTB.InvokeRequired)
        //    {
        //        ShowTaintReasons_d STR_d = new ShowTaintReasons_d(ShowTaintReasons);
        //        UI.Invoke(STR_d, new object[] { LineNo, SourceReasons, SinkReasons });
        //    }
        //    else
        //    {
        //        StringBuilder Message = new StringBuilder();
        //        if (SourceReasons.Count > 0)
        //        {
        //            Message.AppendLine("Source Reasons:");
        //        }
        //        foreach (string Reason in SourceReasons)
        //        {
        //            Message.AppendLine(Reason);
        //        }
        //        Message.AppendLine(""); Message.AppendLine("");
        //        if (SinkReasons.Count > 0)
        //        {
        //            Message.AppendLine("Sink Reasons:");
        //        }
        //        foreach (string Reason in SinkReasons)
        //        {
        //            Message.AppendLine(Reason);
        //        }
        //        IronUI.UI.JSTaintReasonsRTB.Text = Message.ToString();
        //    }
        //}

        delegate void ShowConsoleStatus_d(string Message, bool Error);
        internal static void ShowConsoleStatus(string Message, bool Error)
        {
            if (UI.ConsoleStatusTB.InvokeRequired)
            {
                ShowConsoleStatus_d SCS_d = new ShowConsoleStatus_d(ShowConsoleStatus);
                UI.Invoke(SCS_d, new object[] { Message, Error });
            }
            else
            {
                if (Message.Length == 0)
                {
                    UI.ConsoleStatusTB.Text = "";
                    UI.ConsoleStatusTB.Visible = false;
                }
                else
                {
                    UI.ConsoleStatusTB.Text = Message;
                    if (Error)
                        UI.ConsoleStatusTB.ForeColor = Color.Red;
                    else
                        UI.ConsoleStatusTB.ForeColor = Color.Black;
                    UI.ConsoleStatusTB.Visible = true;
                }
            }
        }

        delegate void UpdateConsoleControlsStatus_d(bool ScanActive);
        internal static void UpdateConsoleControlsStatus(bool ScanActive)
        {
            if (UI.ConsoleScanUrlTB.InvokeRequired)
            {
                UpdateConsoleControlsStatus_d UCCS_d = new UpdateConsoleControlsStatus_d(UpdateConsoleControlsStatus);
                UI.Invoke(UCCS_d, new object[] { ScanActive });
            }
            else
            {
                if (ScanActive)
                {
                    UI.ConsoleStartScanBtn.Text = "Stop Scan";
                    UI.ConsoleScanUrlTB.ReadOnly = true;
                    UI.ConsoleScanUrlTB.BackColor = Color.LightGreen;
                    UI.CrawlerRequestsLbl.Text = "Requests From Crawler: 0";
                    UI.ScanJobsCreatedLbl.Text = "ScanJobs Created: 0";
                    UI.ScanJobsCompletedLbl.Text = "ScanJobs Completed: 0";
                    UI.CrawlerRequestsLbl.Visible = true;
                    UI.ScanJobsCreatedLbl.Visible = true;
                    UI.ScanJobsCompletedLbl.Visible = true;
                }
                else
                {
                    UI.ConsoleStartScanBtn.Text = "Start Scan";
                    UI.ConsoleScanUrlTB.ReadOnly = false;
                    UI.ConsoleScanUrlTB.BackColor = Color.White;
                    UI.CrawlerRequestsLbl.Visible = false;
                    UI.ScanJobsCreatedLbl.Visible = false;
                    UI.ScanJobsCompletedLbl.Visible = false;
                    UI.ConsoleStartScanBtn.Enabled = true;
                    ShowConsoleStatus("", false);
                }
            }
        }

        delegate void UpdateConsoleCrawledRequestsCount_d(int Count);
        internal static void UpdateConsoleCrawledRequestsCount(int Count)
        {
            if (UI.ConsoleScanUrlTB.InvokeRequired)
            {
                UpdateConsoleCrawledRequestsCount_d UCCRC_d = new UpdateConsoleCrawledRequestsCount_d(UpdateConsoleCrawledRequestsCount);
                UI.Invoke(UCCRC_d, new object[] { Count });
            }
            else
            {
                UI.CrawlerRequestsLbl.Text = "Requests From Crawler: " + Count.ToString();
            }
        }

        delegate void UpdateConsoleScanJobsCreatedCount_d(int Count);
        internal static void UpdateConsoleScanJobsCreatedCount(int Count)
        {
            if (UI.ConsoleScanUrlTB.InvokeRequired)
            {
                UpdateConsoleScanJobsCreatedCount_d UCSJCC_d = new UpdateConsoleScanJobsCreatedCount_d(UpdateConsoleScanJobsCreatedCount);
                UI.Invoke(UCSJCC_d, new object[] { Count });
            }
            else
            {
                UI.ScanJobsCreatedLbl.Text = "ScanJobs Created: " + Count.ToString();
            }
        }

        delegate void UpdateConsoleScanJobsCompletedCount_d(int Count);
        internal static void UpdateConsoleScanJobsCompletedCount(int Count)
        {
            if (UI.ConsoleScanUrlTB.InvokeRequired)
            {
                UpdateConsoleScanJobsCompletedCount_d UCSJCC_d = new UpdateConsoleScanJobsCompletedCount_d(UpdateConsoleScanJobsCompletedCount);
                UI.Invoke(UCSJCC_d, new object[] { Count });
            }
            else
            {
                UI.ScanJobsCompletedLbl.Text = "ScanJobs Completed: " + Count.ToString();
            }
        }

        delegate void SetUIVisibility_d(bool Visble);
        internal static void SetUIVisibility(bool Visble)
        {
            if (UI.InvokeRequired)
            {
                SetUIVisibility_d SUIV_d = new SetUIVisibility_d(SetUIVisibility);
                UI.Invoke(SUIV_d, new object[] { Visble });
            }
            else
            {
                try { UI.Visible = Visble; }
                catch { }
                try { AF.Visible = Visble; }
                catch { }
                try { AUW.Visible = Visble; }
                catch { }
                try { SBF.Visible = Visble; }
                catch { }
                try { SSW.Visible = Visble; }
                catch { }
                try { PE.Visible = Visble; }
                catch { }
                try { DW.Visible = Visble; }
                catch { }
                try { EDW.Visible = Visble; }
                catch { }
                try { CF.Visible = Visble; }
                catch { }
            }
        }

        //delegate void UpdateSessionPluginsInASTab_d();
        //internal static void UpdateSessionPluginsInASTab()
        //{
        //    if (UI.ConsoleScanUrlTB.InvokeRequired)
        //    {
        //        UpdateSessionPluginsInASTab_d USPIAST_d = new UpdateSessionPluginsInASTab_d(UpdateSessionPluginsInASTab);
        //        UI.Invoke(USPIAST_d, new object[] { });
        //    }
        //    else
        //    {
        //        UI.ASSessionPluginsCombo.Items.Clear();
        //        UI.ASSessionPluginsCombo.Items.Add("");
        //        foreach (string Name in SessionPlugin.List())
        //        {
        //            UI.ASSessionPluginsCombo.Items.Add(Name);
        //        }
        //    }
        //}

        //internal static void AddStartScanJobWizard(StartScanJobWizard SSW)
        //{
        //    lock (StartScanJobWizards)
        //    {
        //        List<int> ToRemove = new List<int>();
        //        for (int i = 0; i < StartScanJobWizards.Count; i++)
        //        {
        //            if (StartScanJobWizards[i] == null)
        //                ToRemove.Add(i);
        //            else if (StartScanJobWizards[i].IsDisposed)
        //                ToRemove.Add(i);
        //        }
        //        for (int i = 0; i < ToRemove.Count; i++)
        //        {
        //            StartScanJobWizards.RemoveAt(ToRemove[i] + i);
        //        }
        //        StartScanJobWizards.Add(SSW);
        //    }
        //}

        delegate void ShowHideSessionPluginTraceProgressBar_d(bool Show);
        internal static void ShowHideSessionPluginTraceProgressBar(bool Show)
        {
            if (UI.SessionPluginTraceLoadLogProgressBar.InvokeRequired)
            {
                ShowHideSessionPluginTraceProgressBar_d SHSPTPB_d = new ShowHideSessionPluginTraceProgressBar_d(ShowHideSessionPluginTraceProgressBar);
                UI.Invoke(SHSPTPB_d, new object[] { Show });
            }
            else
            {
                UI.SessionPluginTraceLoadLogProgressBar.Visible = Show;
            }
        }

        delegate void ShowSessionPluginTraceLog_d(Request Req, Response Res);
        internal static void ShowSessionPluginTraceLog(Request Req, Response Res)
        {
            if (UI.SessionPluginTraceBottomTabs.InvokeRequired)
            {
                ShowSessionPluginTraceLog_d SSPTL_d = new ShowSessionPluginTraceLog_d(ShowSessionPluginTraceLog);
                UI.Invoke(SSPTL_d, new object[] { Req, Res });
            }
            else
            {
                if (Req == null)
                {
                    UI.SessionPluginTraceRequestView.ClearRequest();
                }
                else
                {
                    UI.SessionPluginTraceRequestView.SetRequest(Req);
                    if (Res == null)
                    {
                        UI.SessionPluginTraceResponseView.ClearResponse();
                    }
                    else
                    {
                        UI.SessionPluginTraceResponseView.SetResponse(Res, Req);
                    }
                }
            }
        }

        static void TurnOffTopMost()
        {
            Thread.Sleep(1000);
            MakeUiTopMost(false);
        }

        delegate void MakeUiTopMost_d(bool TopMost);
        internal static void MakeUiTopMost(bool TopMost)
        {
            if (UI.InvokeRequired)
            {
                MakeUiTopMost_d MUTM_d = new MakeUiTopMost_d(MakeUiTopMost);
                UI.Invoke(MUTM_d, new object[] { TopMost });
            }
            else
            {
                if (TopMost)
                {
                    UI.TopMost = true;
                    TopMostTime = DateTime.Now;
                    Thread T = new Thread(TurnOffTopMost);
                    T.Start();
                }
                else
                {
                    if (TopMostTime == null)
                    {
                        UI.TopMost = false;
                    }
                    else
                    {
                        DateTime CurrentTime = DateTime.Now;
                        if ((CurrentTime.TimeOfDay.TotalSeconds - TopMostTime.TimeOfDay.TotalSeconds) > 1)
                        {
                            UI.TopMost = false;
                        }
                    }
                }
            }
        }
    }
}
