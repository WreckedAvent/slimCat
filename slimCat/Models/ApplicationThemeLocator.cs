namespace slimCat.Models
{
    using System.Windows;

    public class ApplicationThemeLocator : IThemeLocator
    {
        private readonly Application app;

        public ApplicationThemeLocator(Application app)
        {
            this.app = app;
        }

        public Style FindStyle(string styleName)
        {
            return app.TryFindResource(styleName) as Style;
        }

        public T Find<T>(string resourceName) where T : class 
        {
            return app.TryFindResource(resourceName) as T;
        }
    }
}