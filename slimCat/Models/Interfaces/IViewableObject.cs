namespace Slimcat.Models
{
    using System.Windows.Documents;

    public interface IViewableObject
    {
        Block View { get; }
    }
}
