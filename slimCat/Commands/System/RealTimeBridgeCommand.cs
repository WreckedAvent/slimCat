#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RealTimeBridgeCommand.cs">
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

    using System.Diagnostics;
    using Services;
    using Utilities;

    #endregion

    public class CommentEventArgs : CharacterUpdateEventArgs
    {
        public enum CommentTypes
        {
            Comment,

            Newspost,

            BugReport,

            ChangeLog,

            Feature
        }

        public long CommentId { get; set; }

        public CommentTypes CommentType { get; set; }

        public string Link
        {
            get
            {
                switch (CommentType)
                {
                    case CommentTypes.Newspost:
                        return string.Format(
                            "{0}/newspost/{1}/#Comment{2}",
                            Constants.UrlConstants.Domain,
                            TargetId,
                            CommentId);
                    case CommentTypes.BugReport:
                        return string.Format(
                            "{0}/view_bugreport.php?id={1}#Comment{2}",
                            Constants.UrlConstants.Domain,
                            TargetId,
                            CommentId);
                    case CommentTypes.ChangeLog:
                        return string.Format(
                            "{0}/log.php?id={1}#Comment{2}",
                            Constants.UrlConstants.Domain,
                            TargetId,
                            CommentId);
                    case CommentTypes.Feature:
                        return string.Format(
                            "{0}/vote.php?fid={1}#Comment{2}",
                            Constants.UrlConstants.Domain,
                            TargetId,
                            CommentId);
                    default:
                        return string.Empty;
                }
            }
        }

        public long ParentId { get; set; }

        /// <summary>
        ///     Gets or sets the target id. This is the parent of whatever we were replied to.
        /// </summary>
        public long TargetId { get; set; }

        public string Title { get; set; }

        public override string ToString()
        {
            if (ParentId == 0)
            {
                // not a comment, but a suggestion, newspost, etc.
                return "has replied to your " + CommentTypeToString(CommentType) + ", "
                       + string.Format("[url={0}]{1}[/url]", Link, Title);
            }

            // our comment *on* a suggestion, newspost, comment, etc.
            return "has replied to your comment on the " + CommentTypeToString(CommentType) + ' '
                   + string.Format("[url={0}]{1}[/url]", Link, Title);
        }

        private static string CommentTypeToString(CommentTypes argument)
        {
            if (argument == CommentTypes.BugReport)
                return "bug report";

            return argument == CommentTypes.Feature ? "feature suggestion" : argument.ToString();
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            DoNormalToast(toastsManager);
        }

        public override void NavigateTo(IChatState chatState)
        {
            Process.Start(Link);
        }
    }

    public class NoteEventArgs : CharacterUpdateEventArgs
    {
        public string Target { get; set; }

        public long NoteId { get; set; }

        public string Subject { get; set; }

        public override string ToString()
        {
            return string.Format(@"has sent you a note: [url={0}]{1}[/url]", Target, Subject);
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            DoLoudToast(toastsManager);
        }

        public override void NavigateTo(IChatState chatState)
        {
            chatState.EventAggregator.SendUserCommand("priv", new[] { Model.TargetCharacter.Name + "/notes" });

            NotificationService.ShowWindow();
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Web;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void RealTimeBridgeCommand(IDictionary<string, object> command)
        {
            var type = command.Get(Constants.Arguments.Type);

            if (type == null) return;

            var doListAction = new Action<string, ListKind, bool, bool>((name, list, isAdd, giveUpdate) =>
                Dispatcher.Invoke((Action) delegate
                {
                    var result = isAdd
                        ? CharacterManager.Add(name, list)
                        : CharacterManager.Remove(name, list);

                    var character = CharacterManager.Find(name);
                    if (isAdd && character.Status == StatusType.Offline)
                    {
                        CharacterManager.SignOff(name);
                    }
                    character.IsInteresting = CharacterManager.IsOfInterest(name);

                    if (!giveUpdate || !result) return;

                    var eventargs = new CharacterListChangedEventArgs
                    {
                        IsAdded = isAdd,
                        ListArgument = list
                    };

                    Events.NewCharacterUpdate(character, eventargs);
                }));

            if (type.Equals("note"))
            {
                var senderName = command.Get(Constants.Arguments.Sender);
                var subject = command.Get("subject");

                var update = new CharacterUpdateModel(
                    CharacterManager.Find(senderName),
                    new NoteEventArgs
                    {
                        Subject = subject,
                        Target = senderName + "/notes"
                    });

                notes.UpdateNotesAsync(senderName);
                Events.GetEvent<NewUpdateEvent>().Publish(update);
            }
            else if (type.Equals("comment"))
            {
                var name = command.Get(Constants.Arguments.Name);

                // sometimes ID is sent as a string. Sometimes it is sent as a number.
                // so even though it's THE SAME COMMAND we have to treat *each* number differently
                var commentId = long.Parse(command.Get("id"));
                var parentId = (long) command["parent_id"];
                var targetId = long.Parse(command.Get("target_id"));

                var title = HttpUtility.HtmlDecode(command.Get("target"));

                var commentType =
                    command.Get("target_type").ToEnum<CommentEventArgs.CommentTypes>();

                var update = new CharacterUpdateModel(
                    CharacterManager.Find(name),
                    new CommentEventArgs
                    {
                        CommentId = commentId,
                        CommentType = commentType,
                        ParentId = parentId,
                        TargetId = targetId,
                        Title = title
                    });

                Events.GetEvent<NewUpdateEvent>().Publish(update);
            }
            else if (type.Equals("trackadd"))
            {
                var name = command.Get(Constants.Arguments.Name);
                doListAction(name, ListKind.Bookmark, true, true);
            }
            else if (type.Equals("trackrem"))
            {
                var name = command.Get(Constants.Arguments.Name);
                doListAction(name, ListKind.Bookmark, false, true);
            }
            else if (type.Equals("friendadd"))
            {
                var name = command.Get(Constants.Arguments.Name);
                doListAction(name, ListKind.Friend, true, true);
                friendRequestService.UpdatePendingRequests();
                friendRequestService.UpdateOutgoingRequests();
            }
            else if (type.Equals("friendrequest"))
            {
                var name = command.Get(Constants.Arguments.Name);
                doListAction(name, ListKind.FriendRequestReceived, true, true);
                friendRequestService.UpdatePendingRequests();
            }
            else if (type.Equals("friendremove"))
            {
                var name = command.Get(Constants.Arguments.Name);
                doListAction(name, ListKind.Friend, false, false);
            }
        }
    }
}