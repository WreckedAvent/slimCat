namespace slimCat.Models
{
    using System.Windows;

    /// <summary>
    /// Represents theme location for styling WPF elements.
    /// </summary>
    public interface IThemeLocator
    {
        /// <summary>
        /// Finds the style.
        /// </summary>
        /// <param name="styleName">Name of the style.</param>
        Style FindStyle(string styleName);

        /// <summary>
        /// Finds the specified resource name.
        /// </summary>
        /// <typeparam name="T">Type of resource to return.</typeparam>
        /// <param name="resourceName">Name of the resource.</param>
        T Find<T>(string resourceName) where T : class;
    }
}
