/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models
{
    public abstract class NotificationModel : MessageBase
    {
        public NotificationModel()
            : base() { }
    }

    /// <summary>
    /// Used to represent an update about a character
    /// </summary>
    public class CharacterUpdateModel : NotificationModel
    {
        /// <summary>
        /// Represents updates which have a character as their direct object
        /// </summary>
        public abstract class CharacterUpdateEventArgs : EventArgs
        { }

        #region Character Update Event Args subclasses
        public class LoginStateChangedEventArgs : CharacterUpdateEventArgs
        {
            public bool IsLogIn { get; set; }

            public override string ToString()
            {
                return "has logged " + (IsLogIn ? "in." : "out.");
            }
        }

        public class StatusChangedEventArgs : CharacterUpdateEventArgs
        {
            public bool IsStatusTypeChanged { get { return NewStatusType != StatusType.offline; } }
            public bool IsStatusMessageChanged { get { return NewStatusMessage != null; } }

            public StatusType NewStatusType { get; set; }
            public string NewStatusMessage { get; set; }

            public override string ToString()
            {
                StringBuilder toReturn = new StringBuilder();

                if (IsStatusTypeChanged)
                    toReturn.Append("is now " + NewStatusType.ToString());

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
        }

        public class PromoteDemoteEventArgs : CharacterUpdateEventArgs
        {
            public bool IsPromote { get; set; }
            public string TargetChannel { get; set; }
            public string TargetChannelID { get; set; } // used for other parts of the code to understand what channel

            public override string ToString()
            {
                if (TargetChannel == null)
                    return "has been " + (IsPromote ? "promoted to" : "demoted from") + " global moderator.";
                else
                    return "has been " + (IsPromote ? "promoted to" : "demoted from") + " channel moderator in " + TargetChannel + ".";
            }
        }

        public class JoinLeaveEventArgs : CharacterUpdateEventArgs
        {
            public string TargetChannel { get; set; }
            public string TargetChannelID { get; set; } // used for other parts of the code to understand what channel
            public bool Joined { get; set; }

            public override string ToString()
            {
                return "has " + (Joined ? "joined" : "left") + string.Format(" {0}.", TargetChannel);
            }
        }

        public class BroadcastEventArgs : CharacterUpdateEventArgs
        {
            public string Message { get; set; }

            public override string ToString()
            {
                return "broadcasted " + Message + (Char.IsPunctuation(Message.Last()) ? "" : ".");
            }
        }

        public class NoteEventArgs : CharacterUpdateEventArgs
        {
            public long NoteID { get; set; }
            public string Subject { get; set; }
            public string Link
            {
                get
                {
                    return Constants.UrlConstants.READ_NOTE + NoteID;
                }
            }

            public override string ToString()
            {
                return string.Format(@"has sent you a note: [url={0}]{1}[/url]", Link, Subject);
            }
        }

        public class CommentEventArgs : CharacterUpdateEventArgs
        {
            public enum CommentTypes
            {
                comment,
                newspost,
                bugreport,
                changelog,
                feature
            }

            private string commentTypeToString(CommentTypes argument)
            {
                if (argument == CommentTypes.bugreport)
                    return "bug report";
                else if (argument == CommentTypes.feature)
                    return "feature suggestion";
                else return argument.ToString();
            }

            public long ParentID { get; set; } // the parent of whatever was replied to
            public long TargetID { get; set; } // the id of the content, such as newspost ID
            public long CommentID { get; set; } // the id of the comment that is new
            public string Title { get; set; } // title of the newpost, suggestion, etc
            public CommentTypes CommentType { get; set; }  // the type of comment we got
            public string Link
            {
                get
                {
                    switch (CommentType)
                    {
                        case CommentTypes.newspost: return string.Format("{0}/newspost/{1}/#Comment{2}", Constants.UrlConstants.DOMAIN, TargetID, CommentID);
                        case CommentTypes.bugreport: return string.Format("{0}/view_bugreport.php?id={1}#Comment{2}", Constants.UrlConstants.DOMAIN, TargetID, CommentID);
                        case CommentTypes.changelog: return string.Format("{0}/log.php?id={1}#Comment{2}", Constants.UrlConstants.DOMAIN, TargetID, CommentID);
                        case CommentTypes.feature: return string.Format("{0}/vote.php?fid={1}#Comment{2}", Constants.UrlConstants.DOMAIN, TargetID, CommentID);
                        default: return string.Empty;
                    }
                }
            }

            public override string ToString()
            {
                if (ParentID == 0) // not a comment, but a suggestion, newspost, etc.
                {
                    return "has replied to your " + commentTypeToString(CommentType) + ", " + string.Format("[url={0}]{1}[/url]", Link, Title);
                }

                else // our comment *on* a suggestion, newspost, comment, etc.
                {
                    return "has replied to your comment on the " + commentTypeToString(CommentType) + ' ' + string.Format("[url={0}]{1}[/url]", Link, Title);
                }
            }
        }

        public class ListChangedEventArgs : CharacterUpdateEventArgs
        {
            public enum ListType
            {
                friends,
                bookmarks,
                ignored,
                interested,
                notinterested
            }

            public bool IsAdded { get; set; }
            public bool IsTemporary { get; set; }
            public ListType ListArgument { get; set; }

            public override string ToString()
            {
                var listKind = (ListArgument != ListType.notinterested ? ListArgument.ToString() : "not interested");
                bool isTemp = (IsTemporary == null ? false : IsTemporary);

                return "has been " + (IsAdded ? "added to" : "removed from") + " your " + listKind + " list"
                    + (IsTemporary ? " until this character logs out" : "") + '.';
            }
        }

        public class ReportFiledEventArgs : CharacterUpdateEventArgs
        {
            public string Reported { get; set; }
            public string Tab { get; set; }
            public string Complaint { get; set; }
            public int? LogId { get; set; }
            public string CallId { get; set; }
            public string LogLink
            {
                get
                {
                    return Constants.UrlConstants.READ_LOG + LogId.Value;
                }
            }

            public override string ToString()
            {
                string toReturn = null;
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
        }

        public class ReportHandledEventArgs : CharacterUpdateEventArgs
        {
            public string Handled { get; set; }

            public override string ToString()
            {
                return "has handled a report filed by " + Handled;
            }
        }
        #endregion

        #region Class Implentation
        private ICharacter _target;
        private CharacterUpdateEventArgs _args;

        public CharacterUpdateModel(ICharacter target, CharacterUpdateEventArgs e)
            : base()
        {
            _target = target;
            _args = e;
        }

        public ICharacter TargetCharacter { get { return _target; } }
        public CharacterUpdateEventArgs Arguments { get { return _args; } }

        public override string ToString()
        {
            return TargetCharacter.Name + " " + Arguments.ToString();
        }

        internal override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _target = null;
                _args = null;
            }
        }
        #endregion
    }

    /// <summary>
    /// Used to represent an update about a channel
    /// </summary>
    public class ChannelUpdateModel : NotificationModel
    {
        /// <summary>
        /// Represents arguments which have a channel as their direct object
        /// </summary>
        public abstract class ChannelUpdateEventArgs : EventArgs
        { }

        #region Channel Update Event Args sub classes
        public class ChannelDescriptionChangedEventArgs : ChannelUpdateEventArgs
        {
            public override string ToString()
            {
                return "has a new description.";
            }
        }

        public class ChannelModeUpdateEventArgs : ChannelUpdateEventArgs
        {
            public ChannelMode NewMode { get; set; }

            public override string ToString()
            {
                if (NewMode != ChannelMode.both)
                    return "now only allows " + NewMode.ToString() + '.';
                return "now allows ads and chatting.";
            }
        }

        public class ChannelDisciplineEventArgs : ChannelUpdateEventArgs
        {
            public string Kicked { get; set; }
            public bool IsBan { get; set; }
            public string Kicker { get; set; }

            public override string ToString()
            {
                return ": " + Kicker + " has " + (IsBan ? "banned " : "kicked ") + Kicked + '.';
            }
        }

        public class ChannelInviteEventArgs : ChannelUpdateEventArgs
        {
            public string Inviter { get; set; }

            public override string ToString()
            {
                return (": " + Inviter + " has invited you to join their room.");
            }
        }

        public class ChannelTypeChangedEventArgs : ChannelUpdateEventArgs
        {
            public bool IsOpen { get; set; }

            public override string ToString()
            {
                return ": is now " + (IsOpen ? "open" : "closed") + '.';
            }
        }

        public class ChannelTypeBannedListEventArgs : ChannelUpdateEventArgs
        {
            public override string ToString()
            {
                return "'s ban list has been updated";
            }
        }
        #endregion

        #region Class Implementation
        private string _channelID;
        private string _channelTitle;
        private ChannelUpdateEventArgs _args;

        public ChannelUpdateModel(string channelID, ChannelUpdateEventArgs e, string channelTitle = null)
            : base()
        {
            _channelID = channelID;
            _args = e;

            if (channelTitle == null)
                _channelTitle = _channelID;
            else
                _channelTitle = channelTitle;
        }

        public string ChannelID { get { return _channelID; } }
        public string ChannelTitle { get { return _channelTitle; } }
        public ChannelUpdateEventArgs Arguments { get { return _args; } }

        public override string ToString()
        {
            return ChannelTitle + " " + Arguments.ToString();
        }

        internal override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _channelID = null;
                _channelTitle = null;
                _args = null;
            }
        }
        #endregion
    }
}
