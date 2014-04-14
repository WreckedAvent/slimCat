#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationModels.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Linq;
    using System.Text;
    using System.Windows.Documents;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     The notification model.
    /// </summary>
    public abstract class NotificationModel : MessageBase, IViewableObject
    {
        public abstract Block View { get; }
    }

    /// <summary>
    ///     Used to represent an update about a character
    /// </summary>
    public class CharacterUpdateModel : NotificationModel
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterUpdateModel" /> class.
        /// </summary>
        public CharacterUpdateModel(ICharacter target, CharacterUpdateEventArgs e)
        {
            TargetCharacter = target;
            Arguments = e;
        }

        public CharacterUpdateModel()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        public CharacterUpdateEventArgs Arguments { get; private set; }

        /// <summary>
        ///     Gets the target character.
        /// </summary>
        public ICharacter TargetCharacter { get; private set; }

        public override Block View
        {
            get { return new CharacterUpdateView {DataContext = this}; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The to string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string ToString()
        {
            return TargetCharacter.Name + " " + Arguments;
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            TargetCharacter = null;
            Arguments = null;
        }

        #endregion

        /// <summary>
        ///     The broadcast event args.
        /// </summary>
        public class BroadcastEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets the message.
            /// </summary>
            public string Message { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "broadcasted " + Message + (char.IsPunctuation(Message.Last()) ? string.Empty : ".");
            }

            #endregion
        }

        /// <summary>
        ///     Represents updates which have a character as their direct object
        /// </summary>
        public abstract class CharacterUpdateEventArgs : EventArgs
        {
        }

        /// <summary>
        ///     The comment event args.
        /// </summary>
        public class CommentEventArgs : CharacterUpdateEventArgs
        {
            #region Enums

            /// <summary>
            ///     The comment types.
            /// </summary>
            public enum CommentTypes
            {
                Comment,

                Newspost,

                BugReport,

                ChangeLog,

                Feature
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the comment id.
            /// </summary>
            public long CommentId { get; set; }

            /// <summary>
            ///     Gets or sets the comment type.
            /// </summary>
            public CommentTypes CommentType { get; set; }

            /// <summary>
            ///     Gets the link.
            /// </summary>
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

            /// <summary>
            ///     Gets or sets the parent id.
            /// </summary>
            public long ParentId { get; set; }

            /// <summary>
            ///     Gets or sets the target id. This is the parent of whatever we were replied to.
            /// </summary>
            public long TargetId { get; set; }

            /// <summary>
            ///     Gets or sets the title.
            /// </summary>
            public string Title { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
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

            #endregion

            #region Methods

            private static string CommentTypeToString(CommentTypes argument)
            {
                if (argument == CommentTypes.BugReport)
                    return "bug report";

                return argument == CommentTypes.Feature ? "feature suggestion" : argument.ToString();
            }

            #endregion
        }

        /// <summary>
        ///     The join leave event args.
        /// </summary>
        public class JoinLeaveEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether joined.
            /// </summary>
            public bool Joined { get; set; }

            /// <summary>
            ///     Gets or sets the target channel.
            /// </summary>
            public string TargetChannel { get; set; }

            /// <summary>
            ///     Gets or sets the target channel id.
            /// </summary>
            public string TargetChannelId { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "has " + (Joined ? "joined" : "left") + string.Format(" {0}.", TargetChannel);
            }

            #endregion

            // used for other parts of the code to understand what channel
        }

        /// <summary>
        ///     The list changed event args.
        /// </summary>
        public class ListChangedEventArgs : CharacterUpdateEventArgs
        {

            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether is added.
            /// </summary>
            public bool IsAdded { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating whether is temporary.
            /// </summary>
            public bool IsTemporary { get; set; }

            /// <summary>
            ///     Gets or sets the list argument.
            /// </summary>
            public ListKind ListArgument { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                var listKind = ListArgument != ListKind.NotInterested
                    ? ListArgument.ToString()
                    : "not interested";

                return "has been " + (IsAdded ? "added to" : "removed from") + " your " + listKind + " list"
                       + (IsTemporary ? " until this character logs out" : string.Empty) + '.';
            }

            #endregion
        }

        /// <summary>
        ///     The login state changed event args.
        /// </summary>
        public class LoginStateChangedEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether is log in.
            /// </summary>
            public bool IsLogIn { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "has logged " + (IsLogIn ? "in." : "out.");
            }

            #endregion
        }

        /// <summary>
        ///     The note event args.
        /// </summary>
        public class NoteEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets the link.
            /// </summary>
            public string Link
            {
                get { return Constants.UrlConstants.ViewNote + NoteId; }
            }

            /// <summary>
            ///     Gets or sets the note id.
            /// </summary>
            public long NoteId { get; set; }

            /// <summary>
            ///     Gets or sets the subject.
            /// </summary>
            public string Subject { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return string.Format(@"has sent you a note: [url={0}]{1}[/url]", Link, Subject);
            }

            #endregion
        }

        /// <summary>
        ///     The promote demote event args.
        /// </summary>
        public class PromoteDemoteEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether is promote.
            /// </summary>
            public bool IsPromote { get; set; }

            /// <summary>
            ///     Gets or sets the target channel.
            /// </summary>
            public string TargetChannel { get; set; }

            /// <summary>
            ///     Gets or sets the target channel id.
            /// </summary>
            public string TargetChannelId { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                if (TargetChannel == null)
                    return "has been " + (IsPromote ? "promoted to" : "demoted from") + " global moderator.";

                return "has been " + (IsPromote ? "promoted to" : "demoted from") + " channel moderator in "
                       + TargetChannel + ".";
            }

            #endregion

            // used for other parts of the code to understand what channel
        }

        /// <summary>
        ///     The report filed event args.
        /// </summary>
        public class ReportFiledEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets the call id.
            /// </summary>
            public string CallId { get; set; }

            /// <summary>
            ///     Gets or sets the complaint.
            /// </summary>
            public string Complaint { get; set; }

            /// <summary>
            ///     Gets or sets the log id.
            /// </summary>
            public int? LogId { get; set; }

            /// <summary>
            ///     Gets the log link.
            /// </summary>
            public string LogLink
            {
                get
                {
                    var logId = LogId;
                    if (logId != null)
                        return Constants.UrlConstants.ReadLog + logId.Value;

                    return string.Empty;
                }
            }

            /// <summary>
            ///     Gets or sets the reported.
            /// </summary>
            public string Reported { get; set; }

            /// <summary>
            ///     Gets or sets the tab.
            /// </summary>
            public string Tab { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                string toReturn;
                if (Reported == null && Tab == null)
                    toReturn = "has requested staff assistance";
                else if (Reported != Tab)
                    toReturn = "has reported " + Reported + " in " + Tab;
                else
                    toReturn = "has reported" + Reported;

                if (LogId != null)
                    toReturn += string.Format(" [url={0}]view log[/url]", LogLink);

                return toReturn;
            }

            #endregion
        }

        /// <summary>
        ///     The report handled event args.
        /// </summary>
        public class ReportHandledEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets the handled.
            /// </summary>
            public string Handled { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "has handled a report filed by " + Handled;
            }

            #endregion
        }

        /// <summary>
        ///     The status changed event args.
        /// </summary>
        public class StatusChangedEventArgs : CharacterUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets a value indicating whether is status message changed.
            /// </summary>
            public bool IsStatusMessageChanged
            {
                get { return NewStatusMessage != null; }
            }

            /// <summary>
            ///     Gets a value indicating whether is status type changed.
            /// </summary>
            public bool IsStatusTypeChanged
            {
                get { return NewStatusType != StatusType.Offline; }
            }

            /// <summary>
            ///     Gets or sets the new status message.
            /// </summary>
            public string NewStatusMessage { get; set; }

            /// <summary>
            ///     Gets or sets the new status type.
            /// </summary>
            public StatusType NewStatusType { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
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

            #endregion
        }
    }

    /// <summary>
    ///     Used to represent an update about a channel
    /// </summary>
    public class ChannelUpdateModel : NotificationModel
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelUpdateModel" /> class.
        /// </summary>
        public ChannelUpdateModel(ChannelModel model, ChannelUpdateEventArgs e)
        {
            TargetChannel = model;
            Arguments = e;
        }

        public ChannelUpdateModel()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        public ChannelUpdateEventArgs Arguments { get; private set; }

        public ChannelModel TargetChannel { get; set; }

        public override Block View
        {
            get { return new ChannelUpdateView {DataContext = this}; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The to string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string ToString()
        {
            return Arguments.ToString();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            TargetChannel = null;
            Arguments = null;
        }

        #endregion

        /// <summary>
        ///     The channel description changed event args.
        /// </summary>
        public class ChannelDescriptionChangedEventArgs : ChannelUpdateEventArgs
        {
            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "has a new description.";
            }

            #endregion
        }

        /// <summary>
        ///     The channel discipline event args.
        /// </summary>
        public class ChannelDisciplineEventArgs : ChannelUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether is ban.
            /// </summary>
            public bool IsBan { get; set; }

            /// <summary>
            ///     Gets or sets the kicked.
            /// </summary>
            public string Kicked { get; set; }

            /// <summary>
            ///     Gets or sets the kicker.
            /// </summary>
            public string Kicker { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "(" + Kicker + ") has " + (IsBan ? "banned " : "kicked ") + Kicked + '.';
            }

            #endregion
        }

        /// <summary>
        ///     The channel invite event args.
        /// </summary>
        public class ChannelInviteEventArgs : ChannelUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets the inviter.
            /// </summary>
            public string Inviter { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "(" + Inviter + ") has invited you to join their room.";
            }

            #endregion
        }

        /// <summary>
        ///     The channel mode update event args.
        /// </summary>
        public class ChannelModeUpdateEventArgs : ChannelUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets the new mode.
            /// </summary>
            public ChannelMode NewMode { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                if (NewMode != ChannelMode.Both)
                    return "now only allows " + NewMode + '.';

                return "now allows Ads and chatting.";
            }

            #endregion
        }

        /// <summary>
        ///     The channel type banned list event args.
        /// </summary>
        public class ChannelTypeBannedListEventArgs : ChannelUpdateEventArgs
        {
            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "'s ban list has been updated";
            }

            #endregion
        }

        /// <summary>
        ///     The channel type changed event args.
        /// </summary>
        public class ChannelTypeChangedEventArgs : ChannelUpdateEventArgs
        {
            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether is open.
            /// </summary>
            public bool IsOpen { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "is now " + (IsOpen ? "open" : "InviteOnly") + '.';
            }

            #endregion
        }

        /// <summary>
        ///     Represents arguments which have a channel as their direct object
        /// </summary>
        public abstract class ChannelUpdateEventArgs : EventArgs
        {
        }
    }
}