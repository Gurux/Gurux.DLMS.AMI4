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

using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Extended UI setting interface.
    /// </summary>
    /// <remarks>
    /// This interface is used to customize UI for the wanted object.
    /// </remarks>
    public interface IAmiExtendedSettingsUI
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        /// <remarks>
        /// Name must be unique for the module parameter settings.
        /// </remarks>
        string Name
        {
            get;
        }

        /// <summary>
        /// Description of the parameter settings.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// Returns true, if extended UI has been modified.
        /// </summary>
        bool Dirty
        {
            get;
        }

        /// <summary>
        /// Save settings.
        /// </summary>
        void Save();

        /// <summary>
        /// Cancel changes.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Page is changing.
        /// </summary>
        /// <param name="location">New location.</param>
        /// <returns>Returns true, if page can change to the target location.</returns>
        bool CanChange(string location);

        /// <summary>
        /// Are settings shown for the target object.
        /// Object can be device group, device, object or attribute.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <returns>Visible type.</returns>
        ExtendedlUIType ExtendedUI(object target);
    }
}