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
    ///     Represents endpoints for sending and retrieving notes.
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        ///     Gets the note conversation of a given character.
        /// </summary>
        void GetNotes(string character);

        /// <summary>
        ///     Updates the note cache for a given character, so they're refreshed next time viewed.
        /// </summary>
        void UpdateNoteCache(string character);

        /// <summary>
        ///     Sends the specified message to the specified character via note.
        ///     The last message in the conversation is used for the title.
        /// </summary>
        void SendNote(string message, string characterName);
    }
}