#region Copyright

// <copyright file="IManageNotes.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
// 
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

namespace slimCat.Services
{
    /// <summary>
    ///     Represents endpoints for sending and retrieving notes.
    /// </summary>
    public interface IManageNotes
    {
        /// <summary>
        ///     Retrieves the notes for the specified character.
        /// </summary>
        void GetNotesAsync(string character);

        /// <summary>
        ///     Updates the note backlog for a given character.
        /// </summary>
        void UpdateNotesAsync(string character);

        /// <summary>
        ///     Sends the specified message to the specified character via note.
        ///     The last message in the conversation is used for the title.
        /// </summary>
        void SendNoteAsync(string message, string characterName, string subject = null);

        /// <summary>
        ///     Gets the subject line of the last conversation with the specified character.
        /// </summary>
        string GetLastSubject(string character);
    }
}