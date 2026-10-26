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

using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Services.Database;

/// <summary>
/// Describes a left or inner join between two entity types.
/// </summary>
/// <param name="From">The entity type that owns the foreign key.</param>
/// <param name="To">The entity type that owns the referenced key.</param>
/// <param name="ForeignKey">The foreign-key column selector.</param>
/// <param name="Key">The referenced-key column selector.</param>
/// <param name="Inner">Whether to use an inner join instead of a left join.</param>
public sealed record AmiDatabaseJoin(Type From, Type To, LambdaExpression ForeignKey, LambdaExpression Key, bool Inner = false);
