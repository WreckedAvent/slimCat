using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Slimcat.Models
{
    using System.Windows.Documents;

    public interface IViewableObject
    {
        Block View { get; }
    }
}
