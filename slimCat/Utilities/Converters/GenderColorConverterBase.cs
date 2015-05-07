#region Copyright

// <copyright file="GenderColorConverterBase.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using Models;
    using Services;

    #endregion

    /// <summary>
    ///     Contains common logic for turning values into gender colors.
    /// </summary>
    public abstract class GenderColorConverterBase : OneWayConverter
    {
        private readonly ICharacterManager characters;

        private readonly IDictionary<Gender, Gender> genderFallbacks = new Dictionary<Gender, Gender>
        {
            {Gender.Male, Gender.Male},
            {Gender.HermM, Gender.Male},
            {Gender.Cuntboy, Gender.Male},
            {Gender.Female, Gender.Female},
            {Gender.Shemale, Gender.Female},
            {Gender.HermF, Gender.Female},
            {Gender.Transgender, Gender.None},
            {Gender.None, Gender.None}
        };

        private readonly IDictionary<Gender, string> genderNames = new Dictionary<Gender, string>
        {
            {Gender.HermM, "MaleHerm"},
            {Gender.Cuntboy, "Cunt"},
            {Gender.Male, "Male"},
            {Gender.HermF, "Herm"},
            {Gender.Female, "Female"},
            {Gender.Shemale, "Shemale"},
            {Gender.Transgender, "Transgender"},
            {Gender.None, "Highlight"}
        };

        internal readonly IGetPermissions Permissions;

        protected GenderColorConverterBase(IGetPermissions permissions, ICharacterManager characters)
        {
            Permissions = permissions;
            this.characters = characters;
        }

        protected SolidColorBrush GetBrush(ICharacter character)
        {
            if (Permissions != null && Permissions.IsModerator(character.Name))
                return (SolidColorBrush) Application.Current.FindResource("ModeratorBrush");

            if (characters != null && characters.IsOnList(character.Name, ListKind.NotInterested))
                return (SolidColorBrush) Application.Current.FindResource("NotAvailableBrush");

            if (!ApplicationSettings.AllowStatusDiscolor)
                return (SolidColorBrush) TryGet(GetGenderName(character.Gender), true);

            if (character.Status == StatusType.Crown
                || character.Status == StatusType.Online
                || character.Status == StatusType.Looking)
                return (SolidColorBrush) TryGet(GetGenderName(character.Gender), true);

            return (SolidColorBrush) Application.Current.FindResource("NotAvailableBrush");
        }

        protected Color GetColor(ICharacter character)
        {
            if (Permissions != null && Permissions.IsModerator(character.Name))
                return (Color) Application.Current.FindResource("ModeratorColor");

            if (characters != null && characters.IsOnList(character.Name, ListKind.NotInterested))
                return (Color) Application.Current.FindResource("NotAvailableColor");

            if (!ApplicationSettings.AllowStatusDiscolor)
                return (Color) TryGet(GetGenderName(character.Gender), false);

            if (character.Status == StatusType.Crown
                || character.Status == StatusType.Online
                || character.Status == StatusType.Looking)
                return (Color) TryGet(GetGenderName(character.Gender), false);

            return (Color) Application.Current.FindResource("NotAvailableColor");
        }

        protected static object TryGet(string name, bool isBrush)
        {
            var toReturn = Application.Current.TryFindResource(name + (isBrush ? "Brush" : "Color"));

            if (isBrush)
                return toReturn as SolidColorBrush ?? Application.Current.FindResource("HighlightBrush");

            var color = toReturn as Color?;
            return color ?? Application.Current.FindResource("HighlightColor");
        }

        protected string GetGenderName(Gender? gender)
        {
            if (gender == null || ApplicationSettings.GenderColorSettings == GenderColorSettings.None)
                return "Highlight";

            if (ApplicationSettings.GenderColorSettings == GenderColorSettings.Full)
                return genderNames[gender.Value];

            if (ApplicationSettings.GenderColorSettings == GenderColorSettings.GenderOnly)
                return genderNames[genderFallbacks[gender.Value]];

            if (ApplicationSettings.GenderColorSettings == GenderColorSettings.GenderAndHerm
                && (gender == Gender.HermM || gender == Gender.HermF))
                return genderNames[gender.Value];

            return genderNames[genderFallbacks[gender.Value]];
        }
    }
}