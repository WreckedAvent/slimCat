#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StatusCommand.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    #region Usings

    using System.Linq;
    using System.Text;
    using Services;
    using Utilities;

    #endregion

    public partial class CharacterUpdateModel
    {
        public class StatusChangedEventArgs : CharacterUpdateEventArgs
        {
            public bool IsStatusMessageChanged
            {
                get { return NewStatusMessage != null; }
            }

            public bool IsStatusTypeChanged
            {
                get { return NewStatusType != StatusType.Offline; }
            }

            public string NewStatusMessage { get; set; }

            public StatusType NewStatusType { get; set; }

            public override string ToString()
            {
                var toReturn = new StringBuilder();

                if (IsStatusTypeChanged)
                    toReturn.Append("is now " + NewStatusType);

                if (IsStatusMessageChanged)
                {
                    if (IsStatusTypeChanged && NewStatusMessage.Length > 0)
                        toReturn.Append(": " + NewStatusMessage);
                    else if (NewStatusMessage.Length > 0)
                        toReturn.Append("has updated their status: " + NewStatusMessage);
                    else if (toReturn.Length > 0)
                        toReturn.Append(" and has blanked their status");
                    else
                        toReturn.Append("has blanked their status");
                }

                if (!char.IsPunctuation(toReturn.ToString().Trim().Last()))
                    toReturn.Append('.'); // if the last non-whitespace character is not punctuation, add a period

                return toReturn.ToString();
            }

            public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
            {
                if (!ApplicationSettings.ShowStatusToasts
                    || !chatState.IsInteresting(Model.TargetCharacter.Name))
                { return; }

                DoNormalToast(toastsManager);
            }
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void StatusChangedCommand(IDictionary<string, object> command)
        {
            var target = command.Get(Constants.Arguments.Character);
            var status = command.Get(Constants.Arguments.Status).ToEnum<StatusType>();
            var statusMessage = command.Get(Constants.Arguments.StatusMessage);

            var character = CharacterManager.Find(target);
            var statusChanged = false;
            var statusMessageChanged = false;
            var oldStatus = character.Status;

            if (character.Status != status)
            {
                statusChanged = true;
                character.Status = status;
            }

            if (character.StatusMessage != statusMessage)
            {
                statusMessageChanged = true;
                character.StatusMessage = statusMessage;
            }

            if (!statusChanged && !statusMessageChanged)
                return;

            if (status == StatusType.Idle)
                return;

            if (oldStatus == StatusType.Idle && status == StatusType.Online)
                return;

            var updateArgs = new CharacterUpdateModel.StatusChangedEventArgs
            {
                NewStatusType = statusChanged
                    ? status
                    : StatusType.Offline,
                NewStatusMessage = statusMessageChanged
                    ? statusMessage
                    : null
            };

            Events.NewCharacterUpdate(character, updateArgs);
        }
    }

    public partial class UserCommandService
    {
        private void OnStatusChangeRequested(IDictionary<string, object> command)
        {
            object statusmsg;
            StatusType status;
            try
            {
                status = command.Get(Constants.Arguments.Status).ToEnum<StatusType>();
            }
            catch (ArgumentException)
            {
                events.GetEvent<ErrorEvent>()
                    .Publish("'{0}' is not a valid status type!".FormatWith(command.Get(Constants.Arguments.Status)));
                return;
            }

            command.TryGetValue(Constants.Arguments.StatusMessage, out statusmsg);

            model.CurrentCharacter.Status = status;
            model.CurrentCharacter.StatusMessage = statusmsg as string ?? string.Empty;
            connection.SendMessage(command);
        }
    }
}