/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class Constants
    {
        public const string CLIENT_ID = "slimCat";
        public const string CLIENT_NAME = "Ocelot";
        public const string CLIENT_VER = "rc1.00 prototype";
        public static string FRIENDLY_NAME = CLIENT_ID + ' ' + CLIENT_NAME + ' ' + CLIENT_VER;

        public static class UrlConstants
        {
            public const string DOMAIN = @"http://www.f-list.net";

            // not-so-api
            public const string LOGIN = DOMAIN + @"/action/script_login.php";
            public const string UPLOAD_LOG = DOMAIN + @"/fchat/submitLog.php";
            public const string READ_NOTE = DOMAIN + @"/view_note.php?note_id=";
            public const string READ_LOG = DOMAIN + @"/fchat/getLog.php?log=";

            // api
            public const string GET_TICKET = DOMAIN + @"/json/getApiTicket.php";
            public const string API = DOMAIN + @"/json/api/";
        }
    }
}
