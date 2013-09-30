namespace Slimcat.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Gender Settings Model provides basic settings for a gender filter
    /// </summary>
    public class GenderSettingsModel
    {
        #region Fields

        private readonly IDictionary<Gender, bool> genderFilter = new Dictionary<Gender, bool>
        {
            { Gender.Male, true }, 
            { Gender.Female, true }, 
            { Gender.HermF, true }, 
            { Gender.HermM, true }, 
            { Gender.Cuntboy, true }, 
            { Gender.Shemale, true }, 
            { Gender.None, true }, 
            { Gender.Transgender, true }, 
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
            get
            {
                return this.GenderFilter.Where(x => x.Value == false).Select(x => x.Key);
            }
        }

        /// <summary>
        ///     Gets the gender filter.
        /// </summary>
        private IDictionary<Gender, bool> GenderFilter
        {
            get
            {
                return this.genderFilter;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show cuntboys.
        /// </summary>
        public bool ShowCuntboys
        {
            get
            {
                return this.genderFilter[Gender.Cuntboy];
            }

            set
            {
                if (this.genderFilter[Gender.Cuntboy] == value)
                {
                    return;
                }

                this.genderFilter[Gender.Cuntboy] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show female herms.
        /// </summary>
        public bool ShowFemaleHerms
        {
            get
            {
                return this.genderFilter[Gender.HermF];
            }

            set
            {
                if (this.genderFilter[Gender.HermF] == value)
                {
                    return;
                }

                this.genderFilter[Gender.HermF] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show females.
        /// </summary>
        public bool ShowFemales
        {
            get
            {
                return this.genderFilter[Gender.Female];
            }

            set
            {
                if (this.genderFilter[Gender.Female] == value)
                {
                    return;
                }

                this.genderFilter[Gender.Female] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show male herms.
        /// </summary>
        public bool ShowMaleHerms
        {
            get
            {
                return this.genderFilter[Gender.HermM];
            }

            set
            {
                if (this.genderFilter[Gender.HermM] == value)
                {
                    return;
                }

                this.genderFilter[Gender.HermM] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show males.
        /// </summary>
        public bool ShowMales
        {
            get
            {
                return this.genderFilter[Gender.Male];
            }

            set
            {
                if (this.genderFilter[Gender.Male] == value)
                {
                    return;
                }

                this.genderFilter[Gender.Male] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show no genders.
        /// </summary>
        public bool ShowNoGenders
        {
            get
            {
                return this.genderFilter[Gender.None];
            }

            set
            {
                if (this.genderFilter[Gender.None] == value)
                {
                    return;
                }

                this.genderFilter[Gender.None] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show shemales.
        /// </summary>
        public bool ShowShemales
        {
            get
            {
                return this.genderFilter[Gender.Shemale];
            }

            set
            {
                if (this.genderFilter[Gender.Shemale] == value)
                {
                    return;
                }

                this.genderFilter[Gender.Shemale] = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show transgenders.
        /// </summary>
        public bool ShowTransgenders
        {
            get
            {
                return this.genderFilter[Gender.Transgender];
            }

            set
            {
                if (this.genderFilter[Gender.Transgender] == value)
                {
                    return;
                }

                this.genderFilter[Gender.Transgender] = value;
                this.Updated(this, new EventArgs());
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The meets gender filter.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool MeetsGenderFilter(ICharacter character)
        {
            return this.genderFilter[character.Gender];
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            if (this.Updated != null)
            {
                this.Updated(this, new EventArgs());
            }
        }

        #endregion
    }
}