using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Utils.Core.Auth
{
    public interface IUserProvider
    {
        string Username { get; }
    }
}
