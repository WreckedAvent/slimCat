#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenderSettingsModel.cs">
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

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    /// <summary>
    ///     Gender Settings Model provides basic settings for a gender filter
    /// </summary>
    public class GenderSettingsModel
    {
        #region Fields

        private readonly IDictionary<Gender, bool> genderFilter = new Dictionary<Gender, bool>
        {
            {Gender.Male, true},
            {Gender.Female, true},
            {Gender.HermF, true},
            {Gender.HermM, true},
            {Gender.Cuntboy, true},
            {Gender.Shemale, true},
            {Gender.None, true},
            {Gender.Transgender, true},
        };

        #endregion

        #region Public Events

        /// <summary>
        ///     Called whenever the UI updates one of the genders
        /// </summary>
        public event EventHandler Updated;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the filtered genders.
        /// </summary>
        public IEnumerable<Gender> FilteredGenders
        {
            get { return GenderFilter.Where(x => x.Value == false).Select(x => x.Key); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show cuntboys.
        /// </summary>
        public bool ShowCuntboys
        {
            get { return genderFilter[Gender.Cuntboy]; }

            set
            {
                if (genderFilter[Gender.Cuntboy] == value)
                    return;

                genderFilter[Gender.Cuntboy] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show female herms.
        /// </summary>
        public bool ShowFemaleHerms
        {
            get { return genderFilter[Gender.HermF]; }

            set
            {
                if (genderFilter[Gender.HermF] == value)
                    return;

                genderFilter[Gender.HermF] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show females.
        /// </summary>
        public bool ShowFemales
        {
            get { return genderFilter[Gender.Female]; }

            set
            {
                if (genderFilter[Gender.Female] == value)
                    return;

                genderFilter[Gender.Female] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show male herms.
        /// </summary>
        public bool ShowMaleHerms
        {
            get { return genderFilter[Gender.HermM]; }

            set
            {
                if (genderFilter[Gender.HermM] == value)
                    return;

                genderFilter[Gender.HermM] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show males.
        /// </summary>
        public bool ShowMales
        {
            get { return genderFilter[Gender.Male]; }

            set
            {
                if (genderFilter[Gender.Male] == value)
                    return;

                genderFilter[Gender.Male] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show no genders.
        /// </summary>
        public bool ShowNoGenders
        {
            get { return genderFilter[Gender.None]; }

            set
            {
                if (genderFilter[Gender.None] == value)
                    return;

                genderFilter[Gender.None] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show shemales.
        /// </summary>
        public bool ShowShemales
        {
            get { return genderFilter[Gender.Shemale]; }

            set
            {
                if (genderFilter[Gender.Shemale] == value)
                    return;

                genderFilter[Gender.Shemale] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show transgenders.
        /// </summary>
        public bool ShowTransgenders
        {
            get { return genderFilter[Gender.Transgender]; }

            set
            {
                if (genderFilter[Gender.Transgender] == value)
                    return;

                genderFilter[Gender.Transgender] = value;
                Updated(this, new EventArgs());
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the gender filter.
        /// </summary>
        private IDictionary<Gender, bool> GenderFilter
        {
            get { return genderFilter; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The meets gender filter.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool MeetsGenderFilter(ICharacter character)
        {
            return genderFilter[character.Gender];
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }

        #endregion
    }
}