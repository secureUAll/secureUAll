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


namespace IronWASP
{
    public class IronUpdater
    {
        static bool IsOn = true;
        static Queue<Finding> PluginResultQ = new Queue<Finding>();

        static Queue<Request> ScanRequestQ = new Queue<Request>();
        static Queue<Response> ScanResponseQ = new Queue<Response>();
        internal static Dictionary<int, int> ScanGridMap = new Dictionary<int, int>();

        static Queue<Request> ShellRequestQ = new Queue<Request>();
        static Queue<Response> ShellResponseQ = new Queue<Response>();
        internal static Dictionary<int, int> ShellGridMap = new Dictionary<int, int>();

        static Queue<Request> ProbeRequestQ = new Queue<Request>();
        static Queue<Response> ProbeResponseQ = new Queue<Response>();
        internal static Dictionary<int, int> ProbeGridMap = new Dictionary<int, int>();

        static Queue<Request> ProxyRequestQ = new Queue<Request>();
        static Queue<Response> ProxyResponseQ = new Queue<Response>();
        static Queue<Request> ProxyOriginalRequestQ = new Queue<Request>();
        static Queue<Response> ProxyOriginalResponseQ = new Queue<Response>();
        static Queue<Request> ProxyEditedRequestQ = new Queue<Request>();
        static Queue<Response> ProxyEditedResponseQ = new Queue<Response>();

        static Queue<Request[]> ProxyRequestListQ = new Queue<Request[]>();
        static Queue<Response[]> ProxyResponseListQ = new Queue<Response[]>();

        static Queue<Request> OtherSourceRequestQ = new Queue<Request>();
        static Queue<Response> OtherSourceResponseQ = new Queue<Response>();
        internal static Dictionary<int, int> OtherSourceGridMap = new Dictionary<int, int>();

        internal static Queue<IronTrace> Traces = new Queue<IronTrace>();
        internal static Queue<IronTrace> ScanTraces = new Queue<IronTrace>();
        internal static Queue<IronTrace> SessionPluginTraces = new Queue<IronTrace>();
        
        internal static Dictionary<int, int> ProxyGridMap = new Dictionary<int, int>();

        internal static Dictionary<int, int> MTGridMap = new Dictionary<int, int>();

        internal static List<List<string>> Urls = new List<List<string>>();

        static int SleepTime = 2000;
        static Thread T;

        internal static void Start()
        {
            ThreadStart TS = new ThreadStart(IronUpdater.Run);
            IronUpdater.T = new Thread(TS);
            IronUpdater.T.Start();
        }
        static void Run()
        {
            int Counter = 0;
            int MemoryCounter = 0;

            while(IronUpdater.IsOn)
            {
                if (Counter == 5)
                {
                    Counter = 0;
                    MemoryCounter++;
                    
                }
                if (MemoryCounter == 10)
                {
                    MemoryCounter = 0;
                    if (GC.GetTotalMemory(false) > 200000000)
                    {
                        GC.Collect();
                    }
                }
                Thread.Sleep(IronUpdater.SleepTime);
                Counter++;

                try { UpdateProxyLogAndGrid(); }
                catch (Exception Exp) { IronException.Report("Error Updating Proxy Log & Grid", Exp.Message, Exp.StackTrace); }
                try 
                { 
                    if(Counter == 2 || Counter == 4) UpdatePluginResult(); 
                }
                catch (Exception Exp) { IronException.Report("Error Updating PluginResult", Exp.Message, Exp.StackTrace); }

                try { UpdateShellLogAndGrid(); }
                catch (Exception Exp) { IronException.Report("Error Updating Shell Log & Grid", Exp.Message, Exp.StackTrace); }
                try 
                {
                    if (Counter == 5) UpdateProbeLogAndGrid(); 
                }
                catch (Exception Exp) { IronException.Report("Error Updating Probe Log & Grid", Exp.Message, Exp.StackTrace); }
                try
                {
                    if (Counter == 5) UpdateScanLogAndGrid(); 
                }
                catch (Exception Exp) { IronException.Report("Error Updating Scan Log & Grid", Exp.Message, Exp.StackTrace); }
                try
                {
                    //if (Counter == 2 || Counter == 4) 
                    UpdateOtherSourceLogAndGrid();
                }
                catch (Exception Exp) { IronException.Report("Error Updating Other Source Log & Grid", Exp.Message, Exp.StackTrace); }
                try 
                {
                    if (Counter == 5) UpdateSiteMapTree(); 
                }
                catch (Exception Exp) { IronException.Report("Error Updating SiteMapTree", Exp.Message, Exp.StackTrace); }
                try { UpdateTraceLogAndGrid(); }
                catch (Exception Exp) { IronException.Report("Error Updating Trace Log & Grid", Exp.Message, Exp.StackTrace); }
                try
                {
                    if (Counter == 2 || Counter == 4) UpdateScanTraceLogAndGrid();
                }
                catch (Exception Exp) { IronException.Report("Error Updating ScanTrace Log & Grid", Exp.Message, Exp.StackTrace); }
                try
                {
                    if (Counter == 2 || Counter == 4) UpdateScanTraceLogAndGrid();
                }
                catch (Exception Exp) { IronException.Report("Error Updating ScanTrace Log & Grid", Exp.Message, Exp.StackTrace); }
                try
                {
                    if (Counter == 2 || Counter == 4) UpdateSessionPluginTraceLogAndGrid();
                }
                catch (Exception Exp) { IronException.Report("Error Updating SessionPluginTrace Log & Grid", Exp.Message, Exp.StackTrace); }
                try
                {
                    if (Counter == 5) ThreadStore.CleanUp(); 
                }
                catch (Exception Exp) { IronException.Report("Error cleaning up ThreadStore", Exp.Message, Exp.StackTrace); }
            }
        }
        internal static void Stop()
        {
            IsOn = false;
            T.Abort();
        }
        internal static void AddPluginResult(Finding PR)
        {
            if (PR != null)
            {
                lock (PluginResultQ)
                {
                    PluginResultQ.Enqueue(PR);
                }
            }
        }
        static void UpdatePluginResult()
        {
            Finding[] DequedPluginResult;
            lock (PluginResultQ)
            {
                DequedPluginResult = PluginResultQ.ToArray();
                PluginResultQ.Clear();
            }
            if (DequedPluginResult == null) return;

            List<Finding> PRs = new List<Finding>();
            foreach (Finding PR in DequedPluginResult)
            {
                try
                {
                    foreach (Trigger T in PR.Triggers.GetTriggers())
                    {
                        if (T.Request != null)
                        {
                            T.Request.StoredHeadersString = T.Request.GetHeadersAsString();
                            if (T.Request.IsBinary) T.Request.StoredBinaryBodyString = T.Request.BinaryBodyString;
                        }
                        if (T.Response != null)
                        {
                            T.Response.StoredHeadersString = T.Response.GetHeadersAsString();
                            if (T.Response.IsBinary) T.Response.StoredBinaryBodyString = T.Response.BinaryBodyString;
                        }
                    }
                    if (PR.FromActiveScan)
                    {
                        try
                        {
                            PR.BaseRequest.StoredHeadersString = PR.BaseRequest.GetHeadersAsString();
                            if (PR.BaseRequest.IsBinary) PR.BaseRequest.StoredBinaryBodyString = PR.BaseRequest.BinaryBodyString;
                            PR.BaseResponse.StoredHeadersString = PR.BaseResponse.GetHeadersAsString();
                            if (PR.BaseResponse.IsBinary) PR.BaseResponse.StoredBinaryBodyString = PR.BaseResponse.BinaryBodyString;
                        }
                        catch { }
                    }
                    PR.Id = Interlocked.Increment(ref Config.PluginResultCount);
                    PRs.Add(PR);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }
            if (PRs.Count > 0)
            {
                IronDB.LogPluginResults(PRs);
                IronUI.UpdatePluginResultTree(PRs);
            }
        }

        internal static void AddProxyRequest(Request Request)
        {
            if (Request != null)
            {
                try
                {
                    Request ClonedRequest = Request.GetClone(true);
                    if (ClonedRequest != null)
                    {
                        lock (ProxyRequestQ)
                        {
                            ProxyRequestQ.Enqueue(ClonedRequest);
                        }
                    }
                    else
                    {
                        Tools.Trace("IronUpdater", "Null Proxy Request");
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Proxy Request for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddProxyResponse(Response Response)
        {
            if (Response != null)
            {
                try
                {
                    Response ClonedResponse = Response.GetClone(true);
                    if (ClonedResponse != null)
                    {
                        lock (ProxyResponseQ)
                        {
                            ProxyResponseQ.Enqueue(ClonedResponse);
                        }
                    }
                    else
                        Tools.Trace("IronUpdater", "Null Proxy Response");
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Proxy Response for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddProxyRequestsAfterEdit(Request OriginalRequest, Request EditedRequest)
        {
            if (OriginalRequest != null)
            {
                lock (ProxyOriginalRequestQ)
                {
                    ProxyOriginalRequestQ.Enqueue(OriginalRequest);
                }
            }
            if (ProxyEditedRequestQ != null)
            {
                lock (ProxyEditedRequestQ)
                {
                    ProxyEditedRequestQ.Enqueue(EditedRequest);
                }
            }
        }

        internal static void AddProxyResponsesAfterEdit(Response OriginalResponse, Response EditedResponse)
        {
            if (OriginalResponse != null)
            {
                lock (ProxyOriginalResponseQ)
                {
                    ProxyOriginalResponseQ.Enqueue(OriginalResponse);
                }
            }
            if (EditedResponse != null)
            {
                lock (ProxyEditedResponseQ)
                {
                    ProxyEditedResponseQ.Enqueue(EditedResponse);
                }
            }
        }

        internal static void AddProxyRequests(Request[] Requests)
        {
            if (Requests.Length ==2)
            {
                try
                {
                    lock (ProxyRequestListQ)
                    {
                        ProxyRequestListQ.Enqueue(Requests);
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Proxy Request for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddProxyResponses(Response[] Responses)
        {
            if (Responses.Length == 2)
            {
                try
                {
                    lock (ProxyResponseListQ)
                    {
                        ProxyResponseListQ.Enqueue(Responses);
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Proxy Response for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        static void UpdateProxyLogAndGrid()
        {
            Response[] DequedResponses;
            lock (ProxyResponseQ)
            {
                DequedResponses = ProxyResponseQ.ToArray();
                ProxyResponseQ.Clear();
            }
            
            List<Response> Responses = new List<Response>();            
            foreach(Response Res in DequedResponses)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Proxy Response Q", "Null Response DeQed from Proxy Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if(Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    Responses.Add(Res);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            Response[] DequedOriginalResponses;
            List<Response> OriginalResponses = new List<Response>();
            lock (ProxyOriginalResponseQ)
            {
                DequedOriginalResponses = ProxyOriginalResponseQ.ToArray();
                ProxyOriginalResponseQ.Clear();
            }
            foreach(Response Res in DequedOriginalResponses)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Original Proxy Response Q", "Null Response DeQed from Original Proxy Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if (Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    OriginalResponses.Add(Res);
                }
                catch (Exception Exp)
                { 
                    IronException.Report("Error preparing Original Response for UI & DB Update", Exp.Message, Exp.StackTrace); 
                }
            }
            Response[] DequedEditedResponses;
            List<Response> EditedResponses = new List<Response>();
            lock (ProxyEditedResponseQ)
            {
                DequedEditedResponses = ProxyEditedResponseQ.ToArray();
                ProxyEditedResponseQ.Clear();
            }
            foreach(Response Res in DequedEditedResponses)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Edited Proxy Response Q", "Null Response DeQed from Edited Proxy Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if (Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    EditedResponses.Add(Res);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Edited Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }
            
            Request[] DequedRequests;
            List<Request> Requests = new List<Request>();
            lock (ProxyRequestQ)
            {
                DequedRequests = ProxyRequestQ.ToArray();
                ProxyRequestQ.Clear();
            }
            foreach(Request Req in DequedRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Proxy Request Q", "Null Request DeQed from Proxy Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Urls.Add(GetUrlForList(Req));
                    Requests.Add(Req);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Proxy Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            Request[] DequedOriginalRequests;
            List<Request> OriginalRequests = new List<Request>();
            lock (ProxyOriginalRequestQ)
            {
                DequedOriginalRequests = ProxyOriginalRequestQ.ToArray();
                ProxyOriginalRequestQ.Clear();
            }
            foreach(Request Req in DequedOriginalRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Proxy Original Request Q", "Null Request DeQed from Proxy Original Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Urls.Add(GetUrlForList(Req));
                    OriginalRequests.Add(Req);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Original Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            Request[] DequedEditedRequests;
            List<Request> EditedRequests = new List<Request>();
            lock (ProxyEditedRequestQ)
            {
                DequedEditedRequests = ProxyEditedRequestQ.ToArray();
                ProxyEditedRequestQ.Clear();
            }
            foreach(Request Req in DequedEditedRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Proxy Edited Request Q", "Null Request DeQed from Proxy Edited Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Urls.Add(GetUrlForList(Req));
                    EditedRequests.Add(Req);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Edited Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            List<Session> IronSessions = new List<Session>();
            if (Requests.Count > 0 || Responses.Count > 0 || OriginalRequests.Count > 0 || OriginalResponses.Count > 0 || EditedRequests.Count > 0 || EditedResponses.Count > 0)
            {
                IronDB.LogProxyMessages(IronSessions, Requests, Responses, OriginalRequests, OriginalResponses, EditedRequests, EditedResponses);
            }
            if (Requests.Count > 0 | Responses.Count > 0)
            {
                IronUI.UpdateProxyLogGrid(Requests, Responses);
            }

            Response[][] DequedResponseArrs;
            lock (ProxyResponseListQ)
            {
                DequedResponseArrs = ProxyResponseListQ.ToArray();
                ProxyResponseListQ.Clear();
            }

            List<Response[]> ResponseArrs = new List<Response[]>();
            foreach (Response[] ResArr in DequedResponseArrs)
            {
                try
                {
                    if (ResArr.Length == 2)
                    {
                        if (ResArr[1] == null)
                        {
                            IronException.Report("Null Response DeQed from Proxy Response Q", "Null Response DeQed from Proxy Response Q");
                            continue;
                        }

                        if (ResArr[0] != null)
                        {
                            ResArr[0].StoredHeadersString = ResArr[0].GetHeadersAsString();
                            if (ResArr[0].IsBinary) ResArr[0].StoredBinaryBodyString = ResArr[0].BinaryBodyString;
                        }
                        ResArr[1].StoredHeadersString = ResArr[1].GetHeadersAsString();
                        if (ResArr[1].IsBinary) ResArr[1].StoredBinaryBodyString = ResArr[1].BinaryBodyString;

                        ResponseArrs.Add(ResArr);
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            Request[][] DequedRequestArrs;
            List<Request[]> RequestArrs = new List<Request[]>();
            lock (ProxyRequestListQ)
            {
                DequedRequestArrs = ProxyRequestListQ.ToArray();
                ProxyRequestListQ.Clear();
            }
            foreach (Request[] ReqArr in DequedRequestArrs)
            {
                try
                {
                    if (ReqArr.Length == 2)
                    {
                        if (ReqArr[1] == null)
                        {
                            IronException.Report("Null Request DeQed from Proxy Request Q", "Null Request DeQed from Proxy Request Q");
                            continue;
                        }
                        if (ReqArr[0] != null)
                        {
                            ReqArr[0].StoredFile = ReqArr[0].File;
                            ReqArr[0].StoredParameters = ReqArr[0].GetParametersString();
                            ReqArr[0].StoredHeadersString = ReqArr[0].GetHeadersAsString();
                            if (ReqArr[0].IsBinary) ReqArr[0].StoredBinaryBodyString = ReqArr[0].BinaryBodyString;
                            Urls.Add(GetUrlForList(ReqArr[0]));
                        }
                        ReqArr[1].StoredFile = ReqArr[1].File;
                        ReqArr[1].StoredParameters = ReqArr[1].GetParametersString();
                        ReqArr[1].StoredHeadersString = ReqArr[1].GetHeadersAsString();
                        if (ReqArr[1].IsBinary) ReqArr[1].StoredBinaryBodyString = ReqArr[1].BinaryBodyString;
                        Urls.Add(GetUrlForList(ReqArr[1]));

                        RequestArrs.Add(ReqArr);
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Proxy Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            if (RequestArrs.Count > 0 || ResponseArrs.Count > 0)
            {
                IronDB.LogProxyMessages(RequestArrs, ResponseArrs);
                IronUI.UpdateProxyLogGridWithArrs(RequestArrs, ResponseArrs);
            }
        }

        public static void AddMTRequest(Session Sess)
        {

        }
        public static void AddMTResponse(Session Sess)
        {

        }

        internal static void AddShellRequest(Request Request)
        {
            if (Request != null)
            {
                try
                {
                    Request ClonedRequest = Request.GetClone(true);
                    if (ClonedRequest != null)
                    {
                        lock (ShellRequestQ)
                        {
                            ShellRequestQ.Enqueue(ClonedRequest);
                        }
                    }
                    else
                    {
                        Tools.Trace("IronUpdater", "Null Shell Request");
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Shell Request for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddShellResponse(Response Response)
        {
            if (Response != null)
            {
                try
                {
                    Response ClonedResponse = Response.GetClone(true);
                    if (ClonedResponse != null)
                    {
                        lock (ShellResponseQ)
                        {
                            ShellResponseQ.Enqueue(ClonedResponse);
                        }
                    }
                    else
                        Tools.Trace("IronUpdater", "Null Shell Response");
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Shell Response for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        static void UpdateShellLogAndGrid()
        {
            Response[] DequedResponses;
            lock (ShellResponseQ)
            {
                DequedResponses = ShellResponseQ.ToArray();
                ShellResponseQ.Clear();
            }
            List<Response> Responses = new List<Response>();
            foreach(Response Res in DequedResponses)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Shell Response Q", "Null Response DeQed from Shell Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if (Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    Responses.Add(Res);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Shell Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            Request[] DequedRequests;
            lock (ShellRequestQ)
            {
                DequedRequests = ShellRequestQ.ToArray();
                ShellRequestQ.Clear();
            }
            List<Request> Requests = new List<Request>();
            foreach(Request Req in DequedRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Shell Request Q", "Null Request DeQed from Shell Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Requests.Add(Req);

                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Shell Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            List<Session> IronSessions = new List<Session>();
            if (Requests.Count > 0 || Responses.Count > 0)
            {
                IronDB.LogShellMessages(IronSessions, Requests, Responses);
                IronUI.UpdateShellLogGrid(Requests, Responses);
            }
        }

        internal static void AddProbeRequest(Request Request)
        {
            if (Request != null)
            {
                try
                {
                    Request ClonedRequest = Request.GetClone(true);
                    if (ClonedRequest != null)
                    {
                        lock (ProbeRequestQ)
                        {
                            ProbeRequestQ.Enqueue(ClonedRequest);
                        }
                    }
                    else
                    {
                        Tools.Trace("IronUpdater", "Null Probe Request");
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Probe Request for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddProbeResponse(Response Response)
        {
            if (Response != null)
            {
                try
                {
                    Response ClonedResponse = Response.GetClone(true);
                    if (ClonedResponse != null)
                    {
                        lock (ProbeResponseQ)
                        {
                            ProbeResponseQ.Enqueue(ClonedResponse);
                        }
                    }
                    else
                        Tools.Trace("IronUpdater", "Null Probe Response");
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Probe Response for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        static void UpdateProbeLogAndGrid()
        {
            Response[] DequedResponses;
            lock (ProbeResponseQ)
            {
                DequedResponses = ProbeResponseQ.ToArray();
                ProbeResponseQ.Clear();
            }
            List<Response> Responses = new List<Response>();
            foreach(Response Res in DequedResponses)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Probe Response Q", "Null Response DeQed from Probe Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if (Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    Responses.Add(Res);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Probe Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }
            
            Request[] DequedRequests;
            lock (ProbeRequestQ)
            {
                DequedRequests = ProbeRequestQ.ToArray();
                ProbeRequestQ.Clear();
            }
            List<Request> Requests = new List<Request>();
            foreach(Request Req in DequedRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Probe Request Q", "Null Request DeQed from Probe Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Requests.Add(Req);

                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Probe Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            List<Session> IronSessions = new List<Session>();
            if (Requests.Count > 0 || Responses.Count > 0)
            {
                IronDB.LogProbeMessages(IronSessions, Requests, Responses);
                IronUI.UpdateProbeLogGrid(Requests, Responses);
            }
        }

        internal static void AddScanRequest(Request Request)
        {
            if (Request != null)
            {
                try
                {
                    Request ClonedRequest = Request.GetClone(true);
                    if (ClonedRequest != null)
                    {
                        lock (ScanRequestQ)
                        {
                            ScanRequestQ.Enqueue(ClonedRequest);
                        }
                    }
                    else
                    {
                        Tools.Trace("IronUpdater", "Null Scan Request");
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Scan Request for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddScanResponse(Response Response)
        {
            if (Response != null)
            {
                try
                {
                    Response ClonedResponse = Response.GetClone(true);
                    if (ClonedResponse != null)
                    {
                        lock (ScanResponseQ)
                        {
                            ScanResponseQ.Enqueue(ClonedResponse);
                        }
                    }
                    else
                        Tools.Trace("IronUpdater", "Null Scan Response");
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Scan Response for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        static void UpdateScanLogAndGrid()
        {
            Response[] DequedResponse;

            lock (ScanResponseQ)
            {
                DequedResponse = ScanResponseQ.ToArray();
                ScanResponseQ.Clear();
            }
            List<Response> Responses = new List<Response>();
            foreach(Response Res in DequedResponse)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Scan Response Q", "Null Response DeQed from Scan Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if (Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    Responses.Add(Res);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Scan Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }
            
            Request[] DequedRequests;
            lock (ScanRequestQ)
            {
                DequedRequests = ScanRequestQ.ToArray();
                ScanRequestQ.Clear();
            }
            List<Request> Requests = new List<Request>();
            foreach(Request Req in DequedRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Scan Request Q", "Null Request DeQed from Scan Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Requests.Add(Req);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Scan Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            List<Session> IronSessions = new List<Session>();
            if (Requests.Count > 0 || Responses.Count > 0)
            {
                IronDB.LogScanMessages(IronSessions, Requests, Responses);
                IronUI.UpdateScanLogGrid(Requests, Responses);
            }
        }

        internal static void AddOtherSourceRequest(Request Request)
        {
            if (Request != null)
            {
                try
                {
                    Request ClonedRequest = Request.GetClone(true);
                    if (ClonedRequest != null)
                    {
                        lock (OtherSourceRequestQ)
                        {
                            OtherSourceRequestQ.Enqueue(ClonedRequest);
                        }
                    }
                    else
                    {
                        Tools.Trace("IronUpdater", "Null Other Source Request");
                    }
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Other Source Request for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        internal static void AddOtherSourceResponse(Response Response)
        {
            if (Response != null)
            {
                try
                {
                    Response ClonedResponse = Response.GetClone(true);
                    if (ClonedResponse != null)
                    {
                        lock (OtherSourceResponseQ)
                        {
                            OtherSourceResponseQ.Enqueue(ClonedResponse);
                        }
                    }
                    else
                        Tools.Trace("IronUpdater", "Null Other Source Response");
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error adding Other Source Response for updating", Exp.Message, Exp.StackTrace);
                }
            }
        }

        static void UpdateOtherSourceLogAndGrid()
        {
            Response[] DequedResponses;
            lock (OtherSourceResponseQ)
            {
                DequedResponses = OtherSourceResponseQ.ToArray();
                OtherSourceResponseQ.Clear();
            }
            List<Response> Responses = new List<Response>();
            foreach (Response Res in DequedResponses)
            {
                try
                {
                    if (Res == null)
                    {
                        IronException.Report("Null Response DeQed from Other Source Response Q", "Null Response DeQed from Other Source Response Q");
                        continue;
                    }
                    Res.StoredHeadersString = Res.GetHeadersAsString();
                    if (Res.IsBinary) Res.StoredBinaryBodyString = Res.BinaryBodyString;
                    Responses.Add(Res);
                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Other Source Response for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            Request[] DequedRequests;
            lock (OtherSourceRequestQ)
            {
                DequedRequests = OtherSourceRequestQ.ToArray();
                OtherSourceRequestQ.Clear();
            }
            List<Request> Requests = new List<Request>();
            foreach (Request Req in DequedRequests)
            {
                try
                {
                    if (Req == null)
                    {
                        IronException.Report("Null Request DeQed from Other Source Request Q", "Null Request DeQed from Other Source Request Q");
                        continue;
                    }
                    Req.StoredFile = Req.File;
                    Req.StoredParameters = Req.GetParametersString();
                    Req.StoredHeadersString = Req.GetHeadersAsString();
                    if (Req.IsBinary) Req.StoredBinaryBodyString = Req.BinaryBodyString;
                    Requests.Add(Req);

                }
                catch (Exception Exp)
                {
                    IronException.Report("Error preparing Other Source Request for UI & DB Update", Exp.Message, Exp.StackTrace);
                }
            }

            List<Session> IronSessions = new List<Session>();
            Dictionary<string, List<Request>> SourceSpecificRequestList = new Dictionary<string, List<Request>>();
            Dictionary<string, List<Response>> SourceSpecificResponseList = new Dictionary<string, List<Response>>();
            if (Requests.Count > 0 || Responses.Count > 0)
            {
                foreach (Request Req in Requests)
                {
                    if (!SourceSpecificRequestList.ContainsKey(Req.Source)) SourceSpecificRequestList[Req.Source] = new List<Request>();
                    if (!SourceSpecificResponseList.ContainsKey(Req.Source)) SourceSpecificResponseList[Req.Source] = new List<Response>();
                    SourceSpecificRequestList[Req.Source].Add(Req);
                }
                foreach (Response Res in Responses)
                {
                    if (!SourceSpecificResponseList.ContainsKey(Res.Source)) SourceSpecificResponseList[Res.Source] = new List<Response>();
                    if (!SourceSpecificRequestList.ContainsKey(Res.Source)) SourceSpecificRequestList[Res.Source] = new List<Request>();
                    SourceSpecificResponseList[Res.Source].Add(Res);
                }

                foreach (string Source in SourceSpecificRequestList.Keys)
                {
                    IronDB.LogOtherSourceMessages(IronSessions, SourceSpecificRequestList[Source], SourceSpecificResponseList[Source], Source);
                }
                List<Request> OtherSourceRequests = new List<Request>();
                List<Response> OtherSourceResponses = new List<Response>();
                if (SourceSpecificRequestList.ContainsKey(IronLog.SelectedOtherSource))
                {
                    OtherSourceRequests = SourceSpecificRequestList[IronLog.SelectedOtherSource];
                    OtherSourceResponses = SourceSpecificResponseList[IronLog.SelectedOtherSource];
                }

                IronUI.UpdateOtherSourceLogGrid(OtherSourceRequests, OtherSourceResponses, IronLog.SelectedOtherSource, new List<string>(SourceSpecificRequestList.Keys));
            }
        }


        internal static void AddTrace(IronTrace Trace)
        {
            if (Trace != null)
            {
                lock (Traces)
                {
                    Traces.Enqueue(Trace);
                }
            }
        }

        static void UpdateTraceLogAndGrid()
        {
            IronTrace[] DequedTraces;
            lock (Traces)
            {
                DequedTraces = Traces.ToArray();
                Traces.Clear();
            }
            List<IronTrace> TraceList = new List<IronTrace>(DequedTraces);
            if (TraceList.Count > 0)
            {
                IronDB.LogTraces(TraceList);
                IronUI.UpdateTraceGrid(TraceList);
            }
        }

        internal static void AddScanTrace(IronTrace Trace)
        {
            if (Trace != null)
            {
                lock (ScanTraces)
                {
                    ScanTraces.Enqueue(Trace);
                }
            }
        }

        static void UpdateScanTraceLogAndGrid()
        {
            IronTrace[] DequedTraces;
            lock (ScanTraces)
            {
                DequedTraces = ScanTraces.ToArray();
                ScanTraces.Clear();
            }
            List<IronTrace> TraceList = new List<IronTrace>(DequedTraces);
            if (TraceList.Count > 0)
            {
                IronDB.LogScanTraces(TraceList);
                IronUI.UpdateScanTraceGrid(TraceList);
            }
        }

        internal static void AddSessionPluginTrace(IronTrace Trace)
        {
            if (Trace != null)
            {
                lock (SessionPluginTraces)
                {
                    SessionPluginTraces.Enqueue(Trace);
                }
            }
        }

        static void UpdateSessionPluginTraceLogAndGrid()
        {
            IronTrace[] DequedTraces;
            lock (SessionPluginTraces)
            {
                DequedTraces = SessionPluginTraces.ToArray();
                SessionPluginTraces.Clear();
            }
            List<IronTrace> TraceList = new List<IronTrace>(DequedTraces);
            if (TraceList.Count > 0)
            {
                IronDB.LogSessionPluginTraces(TraceList);
                IronUI.UpdateSessionPluginTraceGrid(TraceList);
            }
        }

        internal static List<string> GetUrlForList(Request Req)
        {
            List<string> UrlParts = new List<string>();
            UrlParts.Add(Req.BaseUrl);
            UrlParts.AddRange(Req.UrlPathParts);
            UrlParts.Add("");
            if (Req.Query.Count > 0)
            {               
                UrlParts.Add("?" + Req.Query.GetQueryStringFromParameters());
            }
            return UrlParts;
        }

        internal static void AddToSiteMap(Request Req)
        {
            List<string> Url = GetUrlForList(Req);
            lock (Urls)
            {
                Urls.Add(Url);
            }
        }

        static void UpdateSiteMapTree()
        {
            if (Urls.Count > 0)
            {
                List<List<string>> UrlsToBeUpdated = new List<List<string>>();
                lock (Urls)
                {
                    UrlsToBeUpdated.AddRange(Urls);
                    Urls.Clear();
                }
                IronUI.UpdateSitemapTree(UrlsToBeUpdated);
            }
        }
    }
}
