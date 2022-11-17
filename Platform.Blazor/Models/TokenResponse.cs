﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Blazor.Models
{
    public class TokenResponse
    {
         public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public bool IsVerified { get; set; }
        public string? JWToken { get; set; } 
        public string? RefreshToken { get; set; }
    }
}
