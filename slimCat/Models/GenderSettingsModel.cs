#region Copyright

// <copyright file="GenderSettingsModel.cs">
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
        #region Properties

        /// <summary>
        ///     Gets the gender filter.
        /// </summary>
        private IDictionary<Gender, bool> GenderFilter { get; } = new Dictionary<Gender, bool>
        {
            {Gender.Male, true},
            {Gender.Female, true},
            {Gender.HermF, true},
            {Gender.HermM, true},
            {Gender.Cuntboy, true},
            {Gender.Shemale, true},
            {Gender.None, true},
            {Gender.Transgender, true}
        };

        #endregion

        #region Public Events

        /// <summary>
        ///     Called whenever the UI updates one of the genders
        /// </summary>
        public event EventHandler Updated;

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
            return GenderFilter[character.Gender];
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            Updated?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Fields

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
            get { return GenderFilter[Gender.Cuntboy]; }

            set
            {
                if (GenderFilter[Gender.Cuntboy] == value)
                    return;

                GenderFilter[Gender.Cuntboy] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show female herms.
        /// </summary>
        public bool ShowFemaleHerms
        {
            get { return GenderFilter[Gender.HermF]; }

            set
            {
                if (GenderFilter[Gender.HermF] == value)
                    return;

                GenderFilter[Gender.HermF] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show females.
        /// </summary>
        public bool ShowFemales
        {
            get { return GenderFilter[Gender.Female]; }

            set
            {
                if (GenderFilter[Gender.Female] == value)
                    return;

                GenderFilter[Gender.Female] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show male herms.
        /// </summary>
        public bool ShowMaleHerms
        {
            get { return GenderFilter[Gender.HermM]; }

            set
            {
                if (GenderFilter[Gender.HermM] == value)
                    return;

                GenderFilter[Gender.HermM] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show males.
        /// </summary>
        public bool ShowMales
        {
            get { return GenderFilter[Gender.Male]; }

            set
            {
                if (GenderFilter[Gender.Male] == value)
                    return;

                GenderFilter[Gender.Male] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show no genders.
        /// </summary>
        public bool ShowNoGenders
        {
            get { return GenderFilter[Gender.None]; }

            set
            {
                if (GenderFilter[Gender.None] == value)
                    return;

                GenderFilter[Gender.None] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show shemales.
        /// </summary>
        public bool ShowShemales
        {
            get { return GenderFilter[Gender.Shemale]; }

            set
            {
                if (GenderFilter[Gender.Shemale] == value)
                    return;

                GenderFilter[Gender.Shemale] = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show transgenders.
        /// </summary>
        public bool ShowTransgenders
        {
            get { return GenderFilter[Gender.Transgender]; }

            set
            {
                if (GenderFilter[Gender.Transgender] == value)
                    return;

                GenderFilter[Gender.Transgender] = value;
                Updated?.Invoke(this, new EventArgs());
            }
        }

        #endregion
    }
}