#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBrowser.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

using slimCat.Models;
using System.Collections.Generic;

namespace slimCat.Services
{
    /// <summary>
    ///     Represents an endpoint for bi-directional HTTP requests.
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        ///     Gets the note conversation of a given character.
        /// </summary>
        IList<IMessage> GetNotes(string character);

        /// <summary>
        ///     Removes the note cache for a given character, so they're refreshed next time viewed.
        /// </summary>
        void RemoveNoteCache(string character);
    }
}