using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Shared
{
    public class UserRegisteredMessage
    {
        public string UserId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime RegisteredAt { get; set; }
    }
}
