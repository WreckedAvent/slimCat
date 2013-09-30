// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationModels.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The notification model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Models
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Windows.Documents;

    using Slimcat.Utilities;
    using Slimcat.Views;

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
        /// Initializes a new instance of the <see cref="CharacterUpdateModel"/> class.
        /// </summary>
        public CharacterUpdateModel(ICharacter target, CharacterUpdateEventArgs e)
        {
            this.TargetCharacter = target;
            this.Arguments = e;
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
            get
            {
                return new CharacterUpdateView { DataContext = this };
            }
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
            return this.TargetCharacter.Name + " " + this.Arguments;
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this.TargetCharacter = null;
                this.Arguments = null;
            }
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
                return "broadcasted " + this.Message + (char.IsPunctuation(this.Message.Last()) ? string.Empty : ".");
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
            public long CommentID { get; set; }

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
                    switch (this.CommentType)
                    {
                        case CommentTypes.Newspost:
                            return string.Format(
                                "{0}/newspost/{1}/#Comment{2}", 
                                Constants.UrlConstants.Domain, 
                                this.TargetID, 
                                this.CommentID);
                        case CommentTypes.BugReport:
                            return string.Format(
                                "{0}/view_bugreport.php?id={1}#Comment{2}", 
                                Constants.UrlConstants.Domain, 
                                this.TargetID, 
                                this.CommentID);
                        case CommentTypes.ChangeLog:
                            return string.Format(
                                "{0}/log.php?id={1}#Comment{2}", 
                                Constants.UrlConstants.Domain, 
                                this.TargetID, 
                                this.CommentID);
                        case CommentTypes.Feature:
                            return string.Format(
                                "{0}/vote.php?fid={1}#Comment{2}", 
                                Constants.UrlConstants.Domain, 
                                this.TargetID, 
                                this.CommentID);
                        default:
                            return string.Empty;
                    }
                }
            }

            /// <summary>
            ///     Gets or sets the parent id.
            /// </summary>
            public long ParentID { get; set; }

            /// <summary>
            ///     Gets or sets the target id. This is the parent of whatever we were replied to.
            /// </summary>
            public long TargetID { get; set; }

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
                if (this.ParentID == 0)
                {
                    // not a comment, but a suggestion, newspost, etc.
                    return "has replied to your " + CommentTypeToString(this.CommentType) + ", "
                           + string.Format("[url={0}]{1}[/url]", this.Link, this.Title);
                }

                // our comment *on* a suggestion, newspost, comment, etc.
                return "has replied to your comment on the " + CommentTypeToString(this.CommentType) + ' '
                       + string.Format("[url={0}]{1}[/url]", this.Link, this.Title);
            }

            #endregion

            #region Methods

            private static string CommentTypeToString(CommentTypes argument)
            {
                if (argument == CommentTypes.BugReport)
                {
                    return "bug report";
                }

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
            public string TargetChannelID { get; set; }

            #endregion

            // used for other parts of the code to understand what channel
            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                return "has " + (this.Joined ? "joined" : "left") + string.Format(" {0}.", this.TargetChannel);
            }

            #endregion
        }

        /// <summary>
        ///     The list changed event args.
        /// </summary>
        public class ListChangedEventArgs : CharacterUpdateEventArgs
        {
            #region Enums

            /// <summary>
            ///     The list type.
            /// </summary>
            public enum ListType
            {
                Friends, 

                Bookmarks, 

                Ignored, 

                Interested, 

                NotInterested
            }

            #endregion

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
            public ListType ListArgument { get; set; }

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
                var listKind = this.ListArgument != ListType.NotInterested
                                      ? this.ListArgument.ToString()
                                      : "not interested";

                return "has been " + (this.IsAdded ? "added to" : "removed from") + " your " + listKind + " list"
                       + (this.IsTemporary ? " until this character logs out" : string.Empty) + '.';
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
                return "has logged " + (this.IsLogIn ? "in." : "out.");
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
                get
                {
                    return Constants.UrlConstants.ViewNote + this.NoteID;
                }
            }

            /// <summary>
            ///     Gets or sets the note id.
            /// </summary>
            public long NoteID { get; set; }

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
                return string.Format(@"has sent you a note: [url={0}]{1}[/url]", this.Link, this.Subject);
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
            public string TargetChannelID { get; set; }

            #endregion

            // used for other parts of the code to understand what channel
            #region Public Methods and Operators

            /// <summary>
            ///     The to string.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public override string ToString()
            {
                if (this.TargetChannel == null)
                {
                    return "has been " + (this.IsPromote ? "promoted to" : "demoted from") + " global moderator.";
                }

                return "has been " + (this.IsPromote ? "promoted to" : "demoted from") + " channel moderator in "
                       + this.TargetChannel + ".";
            }

            #endregion
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
                    var logId = this.LogId;
                    if (logId != null)
                    {
                        return Constants.UrlConstants.ReadLog + logId.Value;
                    }

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
                if (this.Reported == null && this.Tab == null)
                {
                    toReturn = "has requested staff assistance";
                }
                else if (this.Reported != this.Tab)
                {
                    toReturn = "has reported " + this.Reported + " in " + this.Tab;
                }
                else
                {
                    toReturn = "has reported" + this.Reported;
                }

                if (this.LogId != null)
                {
                    toReturn += string.Format(" [url={0}]view log[/url]", this.LogLink);
                }

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
                return "has handled a report filed by " + this.Handled;
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
                get
                {
                    return this.NewStatusMessage != null;
                }
            }

            /// <summary>
            ///     Gets a value indicating whether is status type changed.
            /// </summary>
            public bool IsStatusTypeChanged
            {
                get
                {
                    return this.NewStatusType != StatusType.Offline;
                }
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

                if (this.IsStatusTypeChanged)
                {
                    toReturn.Append("is now " + this.NewStatusType.ToString());
                }

                if (this.IsStatusMessageChanged)
                {
                    if (this.IsStatusTypeChanged && this.NewStatusMessage.Length > 0)
                    {
                        toReturn.Append(": " + this.NewStatusMessage);
                    }
                    else if (this.NewStatusMessage.Length > 0)
                    {
                        toReturn.Append("has updated their status: " + this.NewStatusMessage);
                    }
                    else if (toReturn.Length > 0)
                    {
                        toReturn.Append(" and has blanked their status");
                    }
                    else
                    {
                        toReturn.Append("has blanked their status");
                    }
                }

                if (!char.IsPunctuation(toReturn.ToString().Trim().Last()))
                {
                    toReturn.Append('.'); // if the last non-whitespace character is not punctuation, add a period
                }

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
        /// Initializes a new instance of the <see cref="ChannelUpdateModel"/> class.
        /// </summary>
        public ChannelUpdateModel(ChannelModel model, ChannelUpdateEventArgs e)
        {
            this.TargetChannel = model;
            this.Arguments = e;
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
            get
            {
                return new ChannelUpdateView { DataContext = this };
            }
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
            return this.Arguments.ToString();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                return;
            }

            this.TargetChannel = null;
            this.Arguments = null;
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
                return "(" + this.Kicker + ") has " + (this.IsBan ? "banned " : "kicked ") + this.Kicked + '.';
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
                return "(" + this.Inviter + ") has invited you to join their room.";
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
                if (this.NewMode != ChannelMode.Both)
                {
                    return "now only allows " + this.NewMode.ToString() + '.';
                }

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
                return "is now " + (this.IsOpen ? "open" : "InviteOnly") + '.';
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