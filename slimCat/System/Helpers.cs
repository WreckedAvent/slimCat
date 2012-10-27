using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace System
{
    public static class ExtensionMethods
    {
        public static T FirstByIdOrDefault<T>(this ICollection<T> model, string ID)
            where T : ChannelModel
        {
            return model.FirstOrDefault(param => param.ID.Equals(ID, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class StaticFunctions
    {
        #region ICharacter
        public static bool CharacterIsInList(this ICollection<ICharacter> collection, ICharacter toFind)
        {
            return (collection.Any(character => character.Name.Equals(toFind.Name, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool NameContains(this ICharacter character, string searchString)
        {
            return character.Name.ToLower().Contains(searchString);
        }
        #endregion

        #region Settings Functions

        public static bool MeetsFilters(this ICharacter character, GenderSettingsModel genders, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            if (!character.NameContains(search.SearchString))
                return false;
            if (!genders.MeetsGenderFilter(character))
                return false;

            return character.MeetsChatModelLists(search, cm, channel);
        }

        public static bool MeetsChatModelLists(this ICharacter character, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            if (search.ShowIgnored && cm.Ignored.Contains(character.Name)) return true;
            if (search.ShowMods && cm.Mods.Contains(character.Name)) return true;
            if (channel != null)
                if (search.ShowMods && channel.Moderators.Contains(character.Name)) return true;
            if (search.ShowFriends && cm.Friends.Contains(character.Name)) return true;
            if (search.ShowBookmarks && cm.Bookmarks.Contains(character.Name)) return true;
            
            if (search.MeetsStatusFilter(character)) return true;
            return false;
        }

        public static string RelationshipToUser(this ICharacter character, IChatModel cm, GeneralChannelModel channel)
        {
            // first, push friends, bookmarks, and moderators to the top of the list
            if (cm.OnlineFriends.Contains(character))
                return "a"; // Really important people!
            else if (cm.OnlineBookmarks.Contains(character))
                return "b"; // Important people!
            else if (cm.OnlineGlobalMods.Contains(character))
                return "c"; // Useful people!
            else if (channel != null && channel.Moderators.Contains(character.Name))
                return "c";
            else if (cm.Ignored.Contains(character.Name))
                return "z"; // "I don't want to see this person"

            // then sort then by status
            else if (character.Status == StatusType.looking)
                return "d"; // People we want to bone!
            else if (character.Status == StatusType.busy)
                return "f"; // Not the most available, but still possible to play with
            else if (character.Status == StatusType.away || character.Status == StatusType.idle)
                return "g"; // probably not going to play with, lower on list
            else if (character.Status == StatusType.dnd)
                return "h"; // most likely not going to play with, lowest aside ignored
            else
                return "e"; // just normal joe user.
        }
        #endregion
    }

    /// <summary>
    /// Keeps the user on the most recent page if things change and if they were on the latest page
    /// </summary>
    public class KeepToCurrentFlowDocument : IDisposable
    {
        #region fields
        bool _couldChangeBefore;
        bool _canChangeNow;
        FlowDocumentPageViewer _reader;
        #endregion

        #region constructor
        public KeepToCurrentFlowDocument(FlowDocumentPageViewer reader)
        {
            _reader = reader;

            _couldChangeBefore = _reader.CanGoToNextPage;

            _reader.Document.DocumentPaginator.PagesChanged += KeepToBottom;
        }
        #endregion

        #region methods
        public void KeepToBottom(object sender, PagesChangedEventArgs e)
        {
            _canChangeNow = _reader.CanGoToNextPage;
            if (_canChangeNow && !_couldChangeBefore)
                _reader.NextPage();

            _couldChangeBefore = _canChangeNow;
        }
        #endregion

        #region IDispose
        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool managed)
        {
            if (managed)
            {
                _reader.Document.DocumentPaginator.PagesChanged -= KeepToBottom;
                _reader = null;
            }
        }
        #endregion
    }
}
