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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace IronWASP
{
    public partial class RequestView : UserControl
    {
        public RequestView()
        {
            InitializeComponent();
        }

        string ExpandedParameterSection = "";
        int ExpandedParameterIndex = 0;

        Request DisplayedRequest;

        bool readOnly = false;

        bool UseSslChanged = false;
        bool HeadersChanged = false;
        bool BodyChanged = false;
        bool UrlPathPartsChanged = false;
        bool QueryParametersChanged = false;
        bool BodyParametersChanged = false;
        bool BodyFormatXmlChanged = false;
        bool CookieParametersChanged = false;
        bool HeadersParametersChanged = false;

        Thread FormatPluginCallingThread;

        string CurrentFormatXml = "";
        string[,] CurrentXmlNameValueArray = new string[,] { };

        public delegate void RequestChangedEvent();

        public event RequestChangedEvent RequestChanged;

        public bool ReadOnly
        {
            get
            {
                return readOnly;
            }
            set
            {
                SetReadOnly(value);
            }
        }

        delegate void SetReadOnly_d(bool ReadOnlyVal);
        public void SetReadOnly(bool ReadOnlyVal)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetReadOnly_d InvokeDelegate_d = new SetReadOnly_d(SetReadOnly);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { ReadOnlyVal });
            }
            else
            {
                readOnly = ReadOnlyVal;
                UseSSLCB.Enabled = !ReadOnlyVal;
                HeadersTBP.ReadOnly = ReadOnlyVal;
                BodyTBP.ReadOnly = ReadOnlyVal;
                FormatXmlTBP.ReadOnly = ReadOnlyVal;
                EditTBP.ReadOnly = ReadOnly;
                SaveEditsLbl.Visible = !ReadOnly;
                //Disable format plugins
                //Make all parameters grid value fields read-only
                foreach (DataGridViewRow Row in UrlPathPartsParametersGrid.Rows)
                {
                    Row.Cells[1].ReadOnly = this.ReadOnly;
                }
                foreach (DataGridViewRow Row in QueryParametersGrid.Rows)
                {
                    Row.Cells[1].ReadOnly = this.ReadOnly;
                }
                foreach (DataGridViewRow Row in BodyParametersGrid.Rows)
                {
                    Row.Cells[1].ReadOnly = this.ReadOnly;
                }
                foreach (DataGridViewRow Row in CookieParametersGrid.Rows)
                {
                    Row.Cells[1].ReadOnly = this.ReadOnly;
                }
                foreach (DataGridViewRow Row in HeadersParametersGrid.Rows)
                {
                    Row.Cells[1].ReadOnly = this.ReadOnly;
                }
            }
        }

        public void ClearRequest()
        {
            this.DisplayedRequest = null;
            this.ClearData();
        }
        
        delegate void ClearData_d();
        void ClearData()
        {
            if (this.BaseTabs.InvokeRequired)
            {
                ClearData_d InvokeDelegate_d = new ClearData_d(ClearData);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { });
            }
            else
            {
                UseSSLCB.Checked = false;
                HeadersTBP.ClearData();
                BodyTBP.ClearData();
                FormatXmlTBP.ClearData();
                ConvertXmlToObjectBtn.Text = "Convert this XML to Object";
                UrlPathPartsParametersGrid.Rows.Clear();
                QueryParametersGrid.Rows.Clear();
                CookieParametersGrid.Rows.Clear();
                HeadersParametersGrid.Rows.Clear();
                BodyParametersGrid.Rows.Clear();
                ClearEditTab();
                foreach (DataGridViewRow Row in FormatPluginsGrid.Rows)
                {
                    Row.Cells[0].Value = false;
                }
                if (FormatPluginCallingThread != null)
                {
                    try
                    {
                        FormatPluginCallingThread.Abort();
                    }
                    catch { }
                }
                ShowStatusMsg("");
                ShowProgressBar(false);
            }
        }

        public void ClearStatusAndError()
        {
            ShowStatusMsg("");
        }

        delegate void ShowStatusMsg_d(string Msg);
        public void ShowStatusMsg(string Msg)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                ShowStatusMsg_d InvokeDelegate_d = new ShowStatusMsg_d(ShowStatusMsg);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Msg });
            }
            else
            {
                StatusAndErrorTB.Text = Msg;
                if (Msg.Length == 0)
                {
                    StatusAndErrorTB.Visible = false;
                }
                else
                {
                    StatusAndErrorTB.ForeColor = Color.Black;
                    StatusAndErrorTB.Visible = true;
                }
            }
        }
        
        delegate void ShowErrorMsg_d(string Msg);
        public void ShowErrorMsg(string Msg)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                ShowErrorMsg_d InvokeDelegate_d = new ShowErrorMsg_d(ShowErrorMsg);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Msg });
            }
            else
            {
                StatusAndErrorTB.Text = Msg;
                if (Msg.Length == 0)
                {
                    StatusAndErrorTB.Visible = false;
                }
                else
                {
                    StatusAndErrorTB.ForeColor = Color.Red;
                    StatusAndErrorTB.Visible = true;
                }
            }
        }

        delegate void ShowProgressBar_d(bool Show);
        public void ShowProgressBar(bool Show)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                ShowProgressBar_d InvokeDelegate_d = new ShowProgressBar_d(ShowProgressBar);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Show });
            }
            else
            {
                this.WaitProgressBar.Visible = Show;
            }
        }

        delegate void SetRequest_d(Request Req);
        public void SetRequest(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetRequest_d InvokeDelegate_d = new SetRequest_d(SetRequest);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                this.ClearData();

                this.SetHeader(Req);
                this.SetBody(Req);
                this.SetUrlPathPartsParameters(Req);
                this.SetQueryParameters(Req);
                
                this.SetCookieParameters(Req);
                this.SetHeadersParameters(Req);

                FormatPluginsGrid.Rows.Clear();
                FormatPluginsGrid.Rows.Add(new object[] { false, "   --   " });
                foreach (string Name in FormatPlugin.List())
                {
                    FormatPluginsGrid.Rows.Add(new object[]{false, Name});
                }
                //this.SetBodyParameters(Req);
                this.AutoDetectFormatAndSetBodyParameters(Req);
                this.ResetAllChangedValueStatus();
                this.DisplayedRequest = Req;
            }
        }

        void AutoDetectFormatAndSetBodyParameters(Request Req)
        {
            if (FormatPluginCallingThread != null)
            {
                try
                {
                    FormatPluginCallingThread.Abort();
                }
                catch { }
            }
            ShowStatusMsg("Detecting Request body format..");
            ShowProgressBar(true);
            FormatPluginCallingThread = new Thread(AutoDetectFormatAndSetBodyParameters);
            FormatPluginCallingThread.Start(Req);
        }
        void AutoDetectFormatAndSetBodyParameters(object ReqObj)
        {
            try
            {
                Request Req = ((Request)ReqObj).GetClone();
                string FPName = FormatPlugin.Get(Req);

                if (FPName == "Normal")
                {
                    SetBodyParameters(Req, true);
                }
                else if (FPName.Length == 0)
                {
                    SetBodyParameters(Req, false);
                }
                else
                {
                    try
                    {
                        FormatPlugin FP = FormatPlugin.Get(FPName);
                        CurrentFormatXml = FP.ToXmlFromRequest(Req);
                        CurrentXmlNameValueArray = FormatPlugin.XmlToArray(CurrentFormatXml);
                        SetDeserializedDataInUi(FP.Name, CurrentFormatXml, CurrentXmlNameValueArray);
                    }
                    catch
                    {
                        SetBodyParameters(Req, false);
                    }
                }
                this.ResetBodyParametersChangedStatus();
            }
            catch (ThreadAbortException) { }
            finally
            {
                ShowStatusMsg("");
                ShowProgressBar(false);
            }
        }

        delegate void SetHeader_d(Request Req);
        void SetHeader(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetHeader_d InvokeDelegate_d = new SetHeader_d(SetHeader);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                this.HeadersTBP.SetText(Req.GetHeadersAsStringWithoutFullURL());
                this.UseSSLCB.Checked = Req.SSL;
                this.ResetHeadersChangedStatus();
                this.ResetSslChangedStatus();
            }
        }
        
        delegate void SetBody_d(Request Req);
        void SetBody(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetBody_d InvokeDelegate_d = new SetBody_d(SetBody);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                if (Req.HasBody)
                {
                    if (Req.IsBinary)
                        this.BodyTBP.SetBytes(Req.BodyArray);
                    else
                        this.BodyTBP.SetText(Req.BodyString);
                }
                this.ResetBodyChangedStatus();
            }
        }
        
        delegate void SetUrlPathPartsParameters_d(Request Req);
        void SetUrlPathPartsParameters(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetUrlPathPartsParameters_d InvokeDelegate_d = new SetUrlPathPartsParameters_d(SetUrlPathPartsParameters);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                List<string> UrlPathParts = Req.UrlPathParts;
                UrlPathPartsParametersGrid.Rows.Clear();
                for (int i = 0; i < UrlPathParts.Count; i++)
                {
                    int RowId = UrlPathPartsParametersGrid.Rows.Add(new object[] { i, UrlPathParts[i], Properties.Resources.Glass });
                    UrlPathPartsParametersGrid.Rows[RowId].Cells[1].ReadOnly = this.ReadOnly;
                }
                this.ResetUrlPathPartsChangedStatus();
            }
        }

        delegate void SetQueryParameters_d(Request Req);
        void SetQueryParameters(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetQueryParameters_d InvokeDelegate_d = new SetQueryParameters_d(SetQueryParameters);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                QueryParametersGrid.Rows.Clear();
                foreach (string Name in Req.Query.GetNames())
                {
                    foreach (string Value in Req.Query.GetAll(Name))
                    {
                        int RowId = QueryParametersGrid.Rows.Add(new object[] { Name, Value, Properties.Resources.Glass });
                        QueryParametersGrid.Rows[RowId].Cells[1].ReadOnly = this.ReadOnly;
                    }
                }
                this.ResetQueryParametersChangedStatus();
            }
        }

        delegate void SetBodyParameters_d(Request Req, bool HideFormatPluginsGrid);
        void SetBodyParameters(Request Req, bool HideFormatPluginsGrid)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetBodyParameters_d InvokeDelegate_d = new SetBodyParameters_d(SetBodyParameters);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req, HideFormatPluginsGrid });
            }
            else
            {
                BodyParametersGrid.Rows.Clear();
                foreach (string Name in Req.Body.GetNames())
                {
                    foreach (string Value in Req.Body.GetAll(Name))
                    {
                        int RowId = BodyParametersGrid.Rows.Add(new object[] { Name, Value, Properties.Resources.Glass });
                        BodyParametersGrid.Rows[RowId].Cells[1].ReadOnly = this.ReadOnly;
                    }
                }
                FormatPluginsGrid.Rows[0].Cells[0].Value = true;
                if (HideFormatPluginsGrid)
                {
                    HideBodyFormatOptions();
                }
                else
                {
                    ShowBodyFormatOptions();
                }
                this.ResetBodyParametersChangedStatus();
            }
        }

        delegate void SetCookieParameters_d(Request Req);
        void SetCookieParameters(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetCookieParameters_d InvokeDelegate_d = new SetCookieParameters_d(SetCookieParameters);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                CookieParametersGrid.Rows.Clear();
                foreach (string Name in Req.Cookie.GetNames())
                {
                    foreach (string Value in Req.Cookie.GetAll(Name))
                    {
                        int RowId = CookieParametersGrid.Rows.Add(new object[] { Name, Value, Properties.Resources.Glass });
                        CookieParametersGrid.Rows[RowId].Cells[1].ReadOnly = this.ReadOnly;
                    }
                }
                this.ResetCookieParametersChangedStatus();
            }
        }

        delegate void SetHeadersParameters_d(Request Req);
        void SetHeadersParameters(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetHeadersParameters_d InvokeDelegate_d = new SetHeadersParameters_d(SetHeadersParameters);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                HeadersParametersGrid.Rows.Clear();
                foreach (string Name in Req.Headers.GetNames())
                {
                    if (!Name.Equals("Host", StringComparison.OrdinalIgnoreCase) && !Name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (string Value in Req.Headers.GetAll(Name))
                        {
                            int RowId = HeadersParametersGrid.Rows.Add(new object[] { Name, Value, Properties.Resources.Glass });
                            HeadersParametersGrid.Rows[RowId].Cells[1].ReadOnly = this.ReadOnly;
                        }
                    }
                }
                this.ResetHeadersParametersChangedStatus();
            }
        }

        public Request GetRequest()
        {
            try
            {
                return this.GetRequestOrException();
            }
            catch
            {
                return null;
            }
        }
        public Request GetRequestOrException()
        {
            this.HandleAllDataChanges();
            if (FormatPluginCallingThread != null)
            {
                try
                {
                    while (FormatPluginCallingThread.ThreadState == ThreadState.Running)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch { }
            }
            return DisplayedRequest;
        }

        delegate void UpdateRequest_d();
        public void UpdateRequest()
        {
            if (this.BaseTabs.InvokeRequired)
            {
                UpdateRequest_d InvokeDelegate_d = new UpdateRequest_d(UpdateRequest);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { });
            }
            else
            {
                try
                {
                    this.HandleAllDataChanges();
                }
                catch(Exception Exp)
                {
                    ShowErrorMsg(Exp.Message);
                }
            }
        }

        void HandleAllDataChanges()
        {
            if (this.ReadOnly) return;
            if (HeadersChanged || BodyChanged || UrlPathPartsChanged || QueryParametersChanged || BodyParametersChanged || CookieParametersChanged || HeadersParametersChanged)
            {
                ShowStatusMsg("");
            }
            if (HeadersChanged)
            {
                Request NewRequest = new Request(HeadersTBP.GetText(), UseSSLCB.Checked);
                if(DisplayedRequest.HasBody)
                {
                    if (DisplayedRequest.IsBinary)
                        NewRequest.BodyArray = BodyTBP.GetBytes();
                    else
                        NewRequest.BodyString = BodyTBP.GetText();
                }
                this.DisplayedRequest = NewRequest;

                SetUrlPathPartsParameters(this.DisplayedRequest);
                SetQueryParameters(this.DisplayedRequest);
                SetCookieParameters(this.DisplayedRequest);
                SetHeadersParameters(this.DisplayedRequest);
                
                ResetHeadersChangedStatus();
                ResetUrlPathPartsChangedStatus();
                ResetQueryParametersChangedStatus();
                ResetCookieParametersChangedStatus();
                ResetHeadersParametersChangedStatus();
            }
            if (BodyChanged && this.DisplayedRequest != null)
            {
                if (BodyTBP.IsBinary)
                    this.DisplayedRequest.BodyArray = BodyTBP.GetBytes();
                else
                    this.DisplayedRequest.BodyString = BodyTBP.GetText();
                AutoDetectFormatAndSetBodyParameters(this.DisplayedRequest);
                //SetBodyParameters(this.DisplayedRequest);
                ClearBodyTypeFormatPluginsUi();
                ResetBodyChangedStatus();
                ResetBodyParametersChangedStatus();
            }
            if (UrlPathPartsChanged && this.DisplayedRequest != null)
            {
                List<string> UrlPathParts = new List<string>();
                foreach (DataGridViewRow Row in UrlPathPartsParametersGrid.Rows)
                {
                    if (Row.Cells[1].Value == null)
                        UrlPathParts.Add("");
                    else
                        UrlPathParts.Add(Row.Cells[1].Value.ToString());
                }
                this.DisplayedRequest.UrlPathParts = UrlPathParts;
                HeadersTBP.SetText(this.DisplayedRequest.GetHeadersAsStringWithoutFullURL());
                ResetHeadersChangedStatus();
                ResetUrlPathPartsChangedStatus();
            }
            if (QueryParametersChanged && this.DisplayedRequest != null)
            {
                this.DisplayedRequest.Query.RemoveAll();
                foreach (DataGridViewRow Row in QueryParametersGrid.Rows)
                {
                    if (Row.Cells[1].Value == null)
                        this.DisplayedRequest.Query.Add(Row.Cells[0].Value.ToString(), "");
                    else
                        this.DisplayedRequest.Query.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
                }
                this.HeadersTBP.SetText(this.DisplayedRequest.GetHeadersAsStringWithoutFullURL());
                ResetHeadersChangedStatus();
                ResetQueryParametersChangedStatus();
            }
            if (CookieParametersChanged && this.DisplayedRequest != null)
            {
                this.DisplayedRequest.Cookie.RemoveAll();
                foreach (DataGridViewRow Row in CookieParametersGrid.Rows)
                {
                    if (Row.Cells[1].Value == null)
                        this.DisplayedRequest.Cookie.Add(Row.Cells[0].Value.ToString(), "");
                    else
                        this.DisplayedRequest.Cookie.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
                }
                this.HeadersTBP.SetText(this.DisplayedRequest.GetHeadersAsStringWithoutFullURL());
                ResetHeadersChangedStatus();
                ResetCookieParametersChangedStatus();
            }
            if (HeadersParametersChanged && this.DisplayedRequest != null)
            {
                foreach (string Name in this.DisplayedRequest.Headers.GetNames())
                {
                    if (Name != "Cookie")
                    {
                        this.DisplayedRequest.Headers.Remove(Name);
                    }
                }                
                foreach (DataGridViewRow Row in HeadersParametersGrid.Rows)
                {
                    if (Row.Cells[1].Value == null)
                        this.DisplayedRequest.Headers.Add(Row.Cells[0].Value.ToString(), "");
                    else
                        this.DisplayedRequest.Headers.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
                }
                this.HeadersTBP.SetText(this.DisplayedRequest.GetHeadersAsStringWithoutFullURL());
                ResetHeadersChangedStatus();
                ResetHeadersParametersChangedStatus();
            }
            if (BodyParametersChanged && this.DisplayedRequest != null)
            {
                if (GetSelectedFormatPluginName() == "Normal")
                {
                    this.DisplayedRequest.Body.RemoveAll();
                    foreach (DataGridViewRow Row in BodyParametersGrid.Rows)
                    {
                        if (Row.Cells[1].Value == null)
                            this.DisplayedRequest.Body.Add(Row.Cells[0].Value.ToString(), "");
                        else
                            this.DisplayedRequest.Body.Add(Row.Cells[0].Value.ToString(), Row.Cells[1].Value.ToString());
                    }
                    this.BodyTBP.SetText(this.DisplayedRequest.BodyString);
                    ResetBodyChangedStatus();
                    ResetBodyParametersChangedStatus();
                    //ClearBodyTypeFormatPluginsUi();
                }
                else
                {
                    string[,] EditedNameValuePairs = new string[BodyParametersGrid.Rows.Count, 2];
                    foreach (DataGridViewRow Row in BodyParametersGrid.Rows)
                    {
                        EditedNameValuePairs[Row.Index, 0] = Row.Cells[0].Value.ToString();
                        if (Row.Cells[1].Value == null)
                            EditedNameValuePairs[Row.Index, 1] = "";
                        else
                            EditedNameValuePairs[Row.Index, 1] = Row.Cells[1].Value.ToString();
                    }
                    string PluginName = GetSelectedFormatPluginName();
                    if (PluginName.Length > 0)
                        SerializeNewParametersWithFormatPlugin(EditedNameValuePairs, PluginName);
                    ResetBodyParametersChangedStatus();
                }
            }
            //if (BodyTypeFormatPluginsParametersChanged && this.DisplayedRequest != null)
            //{
            //    string[,] EditedNameValuePairs = new string[BodyParametersGrid.Rows.Count, 2];
            //    foreach (DataGridViewRow Row in BodyParametersGrid.Rows)
            //    {
            //        EditedNameValuePairs[Row.Index, 0] = Row.Cells[0].Value.ToString();
            //        if (Row.Cells[1].Value == null)
            //            EditedNameValuePairs[Row.Index, 1] = "";
            //        else
            //            EditedNameValuePairs[Row.Index, 1] = Row.Cells[1].Value.ToString();
            //    }
            //    string PluginName = GetSelectedFormatPluginName();
            //    if(PluginName.Length > 0)
            //        SerializeNewParametersWithFormatPlugin(EditedNameValuePairs, PluginName);
            //    ResetBodyTypeFormatPluginsParametersChangedStatus();
            //}
            if (UseSslChanged && this.DisplayedRequest != null)
            {
                this.DisplayedRequest.SSL = UseSSLCB.Checked;
                ResetSslChangedStatus();
            }
        }

        void ClearBodyTypeFormatPluginsUi()
        {
            BodyParametersGrid.Rows.Clear();
            ConvertXmlToObjectBtn.Text = "Convert this XML to Object";
            FormatXmlTBP.ClearData();
            foreach (DataGridViewRow Row in FormatPluginsGrid.Rows)
            {
                Row.Cells[0].Value = false;
            }
            ResetBodyFormatXmlChangedStatus();
            ResetBodyParametersChangedStatus();
        }

        void ResetAllChangedValueStatus()
        {
            ResetSslChangedStatus();
            ResetHeadersChangedStatus();
            ResetBodyChangedStatus();
            ResetUrlPathPartsChangedStatus();
            ResetQueryParametersChangedStatus();
            ResetBodyParametersChangedStatus();
            ResetBodyFormatXmlChangedStatus();
            ResetCookieParametersChangedStatus();
            ResetHeadersParametersChangedStatus();
        }
        void ResetSslChangedStatus()
        {
            UseSslChanged = false;
        }
        void ResetHeadersChangedStatus()
        {
            HeadersChanged = false;
        }
        void ResetBodyChangedStatus()
        {
            BodyChanged = false;
        }
        void ResetUrlPathPartsChangedStatus()
        {
            UrlPathPartsChanged = false;
        }
        void ResetQueryParametersChangedStatus()
        {
            QueryParametersChanged = false;
        }
        void ResetBodyParametersChangedStatus()
        {
            BodyParametersChanged = false;
        }
       void ResetBodyFormatXmlChangedStatus()
        {
            BodyFormatXmlChanged = false;
        }
        void ResetCookieParametersChangedStatus()
        {
            CookieParametersChanged = false;
        }
        void ResetHeadersParametersChangedStatus()
        {
            HeadersParametersChanged = false;
        }

        private void UseSSLCB_CheckedChanged(object sender, EventArgs e)
        {
            UseSslChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void HeadersTBP_ValueChanged()
        {
            HeadersChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void BodyTBP_ValueChanged()
        {
            BodyChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void UrlPathPartsParametersGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            UrlPathPartsChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void QueryParametersGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            QueryParametersChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void CookieParametersGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            CookieParametersChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void HeadersParametersGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            HeadersParametersChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void BodyParametersGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            BodyParametersChanged = true;
            if (DisplayedRequest != null && RequestChanged != null)
                RequestChanged();
        }

        private void FormatXmlTBP_ValueChanged()
        {
            BodyFormatXmlChanged = true;
        }

        private void ConvertXmlToObjectBtn_Click(object sender, EventArgs e)
        {
            if (this.ReadOnly) return;
            if (BodyFormatXmlChanged)
            {
                string XML = FormatXmlTBP.GetText();
                string PluginName = this.GetSelectedFormatPluginName();
                if (PluginName != "Normal" && PluginName.Length > 0 && XML.Length > 0)
                    this.SerializeNewXmlWithFormatPlugin(XML, PluginName);
            }
            ResetBodyFormatXmlChangedStatus();
        }

        private void FormatPluginsGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (this.DisplayedRequest == null) return;
            string PluginName = "";
            CurrentFormatXml = "";
            CurrentXmlNameValueArray = new string[,]{};

            bool Normal = false;
            if (e.RowIndex == 0)
            {
                Normal = true;
                this.SetBodyParameters(this.DisplayedRequest, false);
            }

            foreach (DataGridViewRow Row in FormatPluginsGrid.Rows)
            {
                if (e.RowIndex == Row.Index)
                {
                    PluginName = Row.Cells[1].Value.ToString();
                }
                if (Normal && (e.RowIndex == Row.Index))
                {
                    Row.Cells[0].Value = true;
                }
                else
                {
                    Row.Cells[0].Value = false;
                }
            }
            if (Normal) return;

            BodyParametersGrid.Rows.Clear();
            FormatXmlTBP.ClearData();
            ConvertXmlToObjectBtn.Text = "Convert this XML to Object";
            if (FormatPluginCallingThread != null)
            {
                try
                {
                    FormatPluginCallingThread.Abort();
                }
                catch { }
            }
            if (PluginName.Length == 0) return;
            ShowStatusMsg(string.Format("Parsing Request body as {0}", PluginName));
            ShowProgressBar(true);
            FormatPluginCallingThread = new Thread(DeserializeWithFormatPlugin);
            FormatPluginCallingThread.Start(PluginName);
        }

        void DeserializeWithFormatPlugin(object PluginNameObject)
        {
            string PluginName = PluginNameObject.ToString();
            try
            {
                Request Req = DisplayedRequest.GetClone(true);
                FormatPlugin FP = FormatPlugin.Get(PluginName);
                CurrentFormatXml = FP.ToXmlFromRequest(Req);
                CurrentXmlNameValueArray = FormatPlugin.XmlToArray(CurrentFormatXml);
                ShowStatusMsg("");
                SetDeserializedDataInUi(PluginName, CurrentFormatXml, CurrentXmlNameValueArray);
                this.ResetBodyParametersChangedStatus();
                ShowProgressBar(false);
            }
            catch (ThreadAbortException)
            {
                ShowStatusMsg("");
            }
            catch (Exception Exp)
            {
                IronException.Report(string.Format("Error converting Request to {0}", PluginName), Exp);
                ShowErrorMsg(string.Format("Unable to parse Request body as {0}", PluginName));
                ShowProgressBar(false);
            }
        }

        delegate void SetDeserializedDataInUi_d(string PluginName, string XML, string[,] XmlNameValueArray);
        void SetDeserializedDataInUi(string PluginName, string XML, string[,] XmlNameValueArray)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetDeserializedDataInUi_d InvokeDelegate_d = new SetDeserializedDataInUi_d(SetDeserializedDataInUi);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { PluginName, XML, XmlNameValueArray });
            }
            else
            {
                foreach (DataGridViewRow Row in FormatPluginsGrid.Rows)
                {
                    if (Row.Cells[1].Value.ToString().Equals(PluginName))
                        Row.Cells[0].Value = true;
                    else
                        Row.Cells[0].Value = false;
                }
                FormatXmlTBP.SetText(XML);
                ConvertXmlToObjectBtn.Text = string.Format("Convert this XML to {0}", PluginName);
                BodyParametersGrid.Rows.Clear();
                for (int i = 0; i < XmlNameValueArray.GetLength(0); i++)
                {
                    int RowId = BodyParametersGrid.Rows.Add(new object[] { XmlNameValueArray[i, 0], XmlNameValueArray[i, 1], Properties.Resources.Glass });
                    BodyParametersGrid.Rows[RowId].Cells[1].ReadOnly = this.ReadOnly;
                }
                ShowBodyFormatOptions();
            }
        }

        delegate void SetUpdatedDeserializedXmlInUi_d(string XML);
        void SetUpdatedDeserializedXmlInUi(string XML)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetUpdatedDeserializedXmlInUi_d InvokeDelegate_d = new SetUpdatedDeserializedXmlInUi_d(SetUpdatedDeserializedXmlInUi);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { XML });
            }
            else
            {
                FormatXmlTBP.SetText(XML);
            }
        }

        void SerializeNewParametersWithFormatPlugin(string[,] EditedXmlNameValueArray, string PluginName)
        {
            for (int i = 0; i < this.CurrentXmlNameValueArray.GetLength(0); i++)
            {
                if(this.CurrentXmlNameValueArray[i,0].Equals(EditedXmlNameValueArray[i,0]))
                {
                    if (!this.CurrentXmlNameValueArray[i, 1].Equals(EditedXmlNameValueArray[i, 1]))
                    {
                        this.CurrentFormatXml = FormatPlugin.InjectInXml(this.CurrentFormatXml, i, EditedXmlNameValueArray[i, 1]);
                    }
                }
            }
            this.CurrentXmlNameValueArray = EditedXmlNameValueArray;
            this.SetUpdatedDeserializedXmlInUi(this.CurrentFormatXml);
            this.SerializeNewXmlWithFormatPlugin(this.CurrentFormatXml, PluginName);
        }
        void SerializeNewXmlWithFormatPlugin(string XML, string PluginName)
        {
            this.CurrentFormatXml = XML;
            this.CurrentXmlNameValueArray = FormatPlugin.XmlToArray(this.CurrentFormatXml);
            if (FormatPluginCallingThread != null)
            {
                try
                {
                    FormatPluginCallingThread.Abort();
                }
                catch { }
            }
            ShowProgressBar(true);
            ShowStatusMsg(string.Format("Updating edited values in {0}", PluginName));
            FormatPluginCallingThread = new Thread(SerializeNewXmlWithFormatPlugin);
            FormatPluginCallingThread.Start(PluginName);
        }

        void SerializeNewXmlWithFormatPlugin(object PluginNameObject)
        {
            string PluginName = PluginNameObject.ToString();
            try
            {
                Request Req = DisplayedRequest.GetClone(true);
                FormatPlugin FP = FormatPlugin.Get(PluginName);
                Request NewRequest = FP.ToRequestFromXml(Req, CurrentFormatXml);
                this.DisplayedRequest = NewRequest;
                ShowStatusMsg("");
                this.SetNonFormatPluginRequestFields(NewRequest);
                ShowProgressBar(false);
            }
            catch (ThreadAbortException)
            {
                ShowStatusMsg("");
            }
            catch (Exception Exp)
            {
                IronException.Report(string.Format("Error converting {0} to Request", PluginName), Exp);
                ShowErrorMsg(string.Format("Unable to update edited values in {0}", PluginName));
                ShowProgressBar(false);
            }
        }
        delegate void SetNonFormatPluginRequestFields_d(Request Req);
        void SetNonFormatPluginRequestFields(Request Req)
        {
            if (this.BaseTabs.InvokeRequired)
            {
                SetNonFormatPluginRequestFields_d InvokeDelegate_d = new SetNonFormatPluginRequestFields_d(SetNonFormatPluginRequestFields);
                this.BaseTabs.Invoke(InvokeDelegate_d, new object[] { Req });
            }
            else
            {
                this.SetHeader(Req);
                this.SetBody(Req);
                this.SetUrlPathPartsParameters(Req);
                this.SetQueryParameters(Req);
                ///this.SetBodyParameters(Req);
                this.SetCookieParameters(Req);
                this.SetHeadersParameters(Req);
            }
        }

        string GetSelectedFormatPluginName()
        {
            foreach (DataGridViewRow Row in FormatPluginsGrid.Rows)
            {
                if ((bool)Row.Cells[0].Value)
                {
                    if (Row.Index == 0)
                        return "Normal";
                    else
                        return Row.Cells[1].Value.ToString();
                }
            }
            return "";
        }

        private void BaseTabs_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            try
            {
                this.HandleAllDataChanges();
            }
            catch (Exception Exp) { ShowErrorMsg(Exp.Message); }
        }

        private void BodyParametersFormatTypeTabs_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            try
            {
                this.HandleAllDataChanges();
            }
            catch (Exception Exp) { ShowErrorMsg(Exp.Message); }
        }

        private void RequestView_Load(object sender, EventArgs e)
        {
            FormatPluginsGrid.Rows.Clear();
            FormatPluginsGrid.Rows.Add(new object[] { false, "   --   " });
            foreach (string Name in FormatPlugin.List())
            {
                FormatPluginsGrid.Rows.Add(new object[]{false, Name});
            }
            HideBodyFormatOptions();
        }

        private void ScanBodyTabs_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (ScanBodyTabs.SelectedIndex == 1 && BodyTabSplit.Panel1Collapsed)
            {
                ScanBodyTabs.SelectTab(0);
            }
        }

        private void BodyFormatLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (BodyFormatLbl.Text.StartsWith("Hide"))
            {
                HideBodyFormatOptions();
            }
            else
            {
                ShowBodyFormatOptions();
            }
        }

        void ShowBodyFormatOptions()
        {
            BodyFormatLbl.Text = "Hide Body Format Options";
            BodyTabSplit.Panel1Collapsed = false;
            ScanBodyTabs.TabPages[1].Text = "  Format Plugin XML (For Format Plugin Developers)  ";
        }
        void HideBodyFormatOptions()
        {
            BodyFormatLbl.Text = "Show Body Format Options";
            BodyTabSplit.Panel1Collapsed = true;
            ScanBodyTabs.TabPages[1].Text = "";
            ScanBodyTabs.SelectTab(0);
        }

        private void BodyParametersGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                ExpandedParameterSection = "Body";
                ExpandedParameterIndex = e.RowIndex;
                SetEditTab(BodyParametersGrid.Rows[e.RowIndex].Cells[1].Value.ToString());
            }
        }

        void SetEditTab(string Value)
        {
            EditTBP.SetText(Value);
            BaseTabs.TabPages["EditingTab"].Text = "  Selected Parameter Value  ";
            BaseTabs.SelectTab("EditingTab");
        }

        void ClearEditTab()
        {
            EditTBP.SetText("");
            BaseTabs.TabPages["EditingTab"].Text = "  ";
            if (BaseTabs.SelectedTab.Name == "EditingTab")
            {
                BaseTabs.SelectTab("HeadersTab");
            }
        }

        private void SaveEditsLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            switch (ExpandedParameterSection)
            {
                case ("UrlPathParts"):
                    UrlPathPartsParametersGrid.Rows[ExpandedParameterIndex].Cells[1].Value = EditTBP.GetText();
                    UrlPathPartsChanged = true;
                    BaseTabs.SelectTab("UrlPathPartsParametersTab");
                    break;
                case ("Query"):
                    QueryParametersGrid.Rows[ExpandedParameterIndex].Cells[1].Value = EditTBP.GetText();
                    QueryParametersChanged = true;
                    BaseTabs.SelectTab("QueryParametersTab");
                    break;
                case("Body"):
                    BodyParametersGrid.Rows[ExpandedParameterIndex].Cells[1].Value = EditTBP.GetText();
                    BodyParametersChanged = true;
                    BaseTabs.SelectTab("BodyParametersTab");
                    break;
                case ("Cookie"):
                    CookieParametersGrid.Rows[ExpandedParameterIndex].Cells[1].Value = EditTBP.GetText();
                    CookieParametersChanged = true;
                    BaseTabs.SelectTab("CookieParametersTab");
                    break;
                case ("Headers"):
                    HeadersParametersGrid.Rows[ExpandedParameterIndex].Cells[1].Value = EditTBP.GetText();
                    HeadersParametersChanged = true;
                    BaseTabs.SelectTab("HeadersParametersTab");
                    break;
            }
            ClearEditTab();
        }

        private void QueryParametersGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                ExpandedParameterSection = "Query";
                ExpandedParameterIndex = e.RowIndex;
                SetEditTab(QueryParametersGrid.Rows[e.RowIndex].Cells[1].Value.ToString());
            }
        }

        private void UrlPathPartsParametersGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                ExpandedParameterSection = "UrlPathParts";
                ExpandedParameterIndex = e.RowIndex;
                SetEditTab(UrlPathPartsParametersGrid.Rows[e.RowIndex].Cells[1].Value.ToString());
            }
        }

        private void CookieParametersGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                ExpandedParameterSection = "Cookie";
                ExpandedParameterIndex = e.RowIndex;
                SetEditTab(CookieParametersGrid.Rows[e.RowIndex].Cells[1].Value.ToString());
            }
        }

        private void HeadersParametersGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                ExpandedParameterSection = "Headers";
                ExpandedParameterIndex = e.RowIndex;
                SetEditTab(HeadersParametersGrid.Rows[e.RowIndex].Cells[1].Value.ToString());
            }
        }

        private void BaseTabs_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Text.Trim().Length == 0)
            {
                e.Cancel = true;
                return;
            }
        }
    }
}
