#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HomeHelpViewModel.cs">
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

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using Models;
    using Services;
    using Utilities;

    #endregion

    public class HomeHelpViewModel : ViewModelBase, IHasTabs
    {
        #region Fields

        private string selectedTab = "HowTo";

        #endregion

        #region Constructors and Destructors

        public HomeHelpViewModel(IChatState chatState)
            : base(chatState)
        {
            var commands = CommandDefinitions.Commands.Select(x =>
            {
                var command = x.Value;
                var aliases = Aggregate(CommandDefinitions.CommandAliases.Where(y => y.Value.Equals(x.Key)).Select(y => y.Key).ToList());
                var overrides = CommandDefinitions.CommandOverrides.FirstOrDefault(y => y.Key.Equals(x.Key)).Value;

                var argumentNames = command.ArgumentNames;
                if (overrides != null)
                {
                    argumentNames.Remove(overrides.ArgumentName);
                }

                var arguments = Aggregate(argumentNames);
                if (arguments != null)
                {
                    if (arguments.EndsWith(", channel"))
                        arguments = arguments.Replace(", channel", "");
                    if (arguments.EndsWith("channel"))
                        arguments = arguments.Replace("channel", "channel?");
                    if (x.Key.Equals("status"))
                        arguments = arguments.Replace(", ", " ");
                }

                return new CommandReference
                {
                    CommandName = x.Key,
                    Aliases = aliases,
                    Arumgents = arguments,
                    Permissions = command.PermissionsLevel
                };
            })
                .Where(x => x.CommandName[0] != '_')
                .ToList();

            CommandReferences = new ListCollectionView(commands);
            CommandReferences.GroupDescriptions.Add(new PropertyGroupDescription("Permissions", new PermissionToTextConverter()));
            CommandReferences.SortDescriptions.Add(new SortDescription("CommandName", ListSortDirection.Ascending));
        }

        #endregion

        #region Public Properties

        public ListCollectionView CommandReferences { get; set; }

        public ICharacter slimCat
        {
            get { return CharacterManager.Find("slimCat"); }
        }

        public ChannelModel slimCatChannel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ApplicationSettings.SlimCatChannelId))
                    return null;

                return new GeneralChannelModel(ApplicationSettings.SlimCatChannelId, "slimCat", ChannelType.Private);
            }
        }

        public string SelectedTab
        {
            get { return selectedTab; }
            set
            {
                selectedTab = value;
                OnPropertyChanged("SelectedTab");
            }
        }

        #endregion

        public override void Initialize()
        {
        }

        private string Aggregate(IList<string> list)
        {
            return list != null && list.Any() ? list.Aggregate((current, next) => current + ", {0}".FormatWith(next)) : null;
        }
    }

    public class CommandReference
    {
        public string CommandName { get; set; }

        public string Arumgents { get; set; }

        public string Aliases { get; set; }

        public string Examples { get; set; }

        public CommandModel.PermissionLevel Permissions { get; set; }
    }

    public class PermissionToTextConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (CommandModel.PermissionLevel) value;

            if (level == CommandModel.PermissionLevel.User) return "User commands";
            if (level == CommandModel.PermissionLevel.Moderator) return "Moderator commands";
            if (level == CommandModel.PermissionLevel.GlobalMod) return "Global moderator commands";
            if (level == CommandModel.PermissionLevel.Admin) return "Admin commands";
            return string.Empty;
        }
    }
}