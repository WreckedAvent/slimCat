using slimCat.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace slimCat.Services
{
    public partial class UserCommandService
    {
        private void OnRawRequested(IDictionary<string, object> command)
        {
            connection.SendMessage(command);
        }
    }
}
