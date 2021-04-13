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
using System.Text;

namespace IronWASP
{
    public class SimilarityCheckerItem
    {
        public string Key;
        public Response Res;
        public string Payload;
        bool isPayloadSet = false;
        public string ProcessedBodyString = "";

        public bool IsPayloadSet
        {
            get
            {
                return isPayloadSet;
            }
        }

        internal SimilarityCheckerItem(string Key, Response Res)
        {
            this.Key = Key;
            this.Res = Res;
            this.ProcessedBodyString = this.Res.BodyString;
        }

        internal SimilarityCheckerItem(string Key, Response Res, string Payload)
        {
            this.Key = Key;
            this.Res = Res;
            this.Payload = Payload;
            this.isPayloadSet = true;
            this.ProcessedBodyString = this.Res.BodyString.Replace(Payload, "").Replace(Tools.UrlEncode(Payload), "").Replace(Tools.HtmlEncode(Payload), "");
        }
    }
}
