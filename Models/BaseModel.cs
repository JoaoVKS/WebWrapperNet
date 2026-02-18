using System;
using System.Collections.Generic;
using System.Text;

namespace WebWrap.Models
{
    public class BaseModel
    {
        public required string Type { get; set; }
        public string? RequestId { get; set; }
    }
}
