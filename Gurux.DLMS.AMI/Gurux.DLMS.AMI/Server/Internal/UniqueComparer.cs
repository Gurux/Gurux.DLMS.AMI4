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
// This file is a part of Gurux Workflow Framework.
//
// Gurux Workflow Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Workflow Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Gurux.Common.Db;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// UniqueComparer is used to find objects using IUnique.
    /// </summary>
    /// <typeparam name="T">Class type.</typeparam>
    /// <typeparam name="TYPE">Data type of the IUnique.</typeparam>
    internal class UniqueComparer<T, TYPE> : IEqualityComparer<T>
    {
        /// <summary>
        /// Check are values equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(T x, T y)
        {
            if ((x is IUnique<TYPE> X) && (y is IUnique<TYPE> Y))
            {
                return X.Id.Equals(Y.Id);
            }
            return false;
        }

        /// <summary>
        /// Get hash code for the object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode([DisallowNull] T obj)
        {
            if (obj is IUnique<TYPE> o)
            {
                return o.Id.GetHashCode();
            }
            return base.GetHashCode();
        }       
    }
}