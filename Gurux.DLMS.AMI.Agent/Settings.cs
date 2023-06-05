//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Common;

namespace Gurux.DLMS.AMI.Agent
{
    public class Settings
    {
        /// <summary>
        /// Host name.
        /// </summary>
        public string? Host
        {
            get;
            set;
        }
        public static int GetParameters(string[] args, Settings settings)
        {
            List<GXCmdParameter> parameters = GXCommon.GetParameters(args, "h:");
            foreach (GXCmdParameter it in parameters)
            {
                switch (it.Tag)
                {
                    case 'h':
                        settings.Host = it.Value;
                        break;
                    default:
                        ShowHelp();
                        return 1;
                }
            }
            return 0;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Gurux.DLMS.AMI.Agent is used to read DLMS devices.");
            Console.WriteLine("GuruxDlmsSample -h [Gurux.DLMS.AMI server address]");
            Console.WriteLine(" -h \t host name or IP address.");
        }
    }
}
