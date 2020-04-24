using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.Utils.Auth
{
    public interface IUserProvider
    {
        string Username { get; }
    }
}
