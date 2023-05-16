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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Client.Helpers.Validation
{
    public class GXValidator : ComponentBase
    {
        /// <summary>
        /// Are there any errors.
        /// </summary>
        private bool _errors;
        private ValidationMessageStore? _messageStore;

        [CascadingParameter]
        private EditContext? CurrentEditContext { get; set; }

        /// <summary>
        /// Validate.
        /// </summary>
        [Parameter] 
        public Action<GXValidator>? OnValidate { get; set; }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException(
                                $"{nameof(GXValidator)} requires a cascading " +
                                $"parameter of type {nameof(EditContext)}.");
            }

            _messageStore = new ValidationMessageStore(CurrentEditContext);

            CurrentEditContext.OnValidationRequested += (s, e) =>
                _messageStore.Clear();
            CurrentEditContext.OnFieldChanged += (s, e) =>
                _messageStore.Clear(e.FieldIdentifier);
        }

        /// <summary>
        /// Add validation error.
        /// </summary>
        /// <param name="accessor">Field identifier.</param>
        /// <param name="message">Error message.</param>
        public void AddError(Expression<Func<object?>> accessor, string message)
        {
            _errors = true;
            _messageStore?.Add(FieldIdentifier.Create(accessor), message);
        }

        /// <summary>
        /// Add validation error.
        /// </summary>
        /// <param name="identier">Field identifier.</param>
        /// <param name="message">Error message.</param>
        public void AddError(string identier, string message)
        {
            _errors = true;
            if (CurrentEditContext != null)
            {
                _messageStore?.Add(CurrentEditContext.Field(identier), message);
            }
        }

        /// <summary>
        /// Validate content. 
        /// </summary>
        /// <returns>
        /// True, if content is valid.
        /// </returns>
        public bool Validate()
        {
            if (CurrentEditContext != null && _messageStore != null)
            {
                OnValidate?.Invoke(this);
                CurrentEditContext.NotifyValidationStateChanged();
                return !_errors;
            }
            return true;
        }

        /// <summary>
        /// Display errors.
        /// </summary>
        /// <param name="errors">List of added errros.</param>
        public void DisplayErrors(Dictionary<string, List<string>> errors)
        {
            if (CurrentEditContext != null && _messageStore != null)
            {
                _errors = errors.Any();
                foreach (var err in errors)
                {
                    _messageStore.Add(CurrentEditContext.Field(err.Key), err.Value);
                }
                CurrentEditContext.NotifyValidationStateChanged();
            }
        }

        /// <summary>
        /// Clear form errors.
        /// </summary>
        public void ClearErrors()
        {
            _errors = false;
            _messageStore?.Clear();
            CurrentEditContext?.NotifyValidationStateChanged();
        }
    }
}
