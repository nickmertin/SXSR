using System;
using System.Collections.Generic;
using System.Text;

namespace Project
{
    public class SignaturePart : List<SignaturePoint>
    {
        public TimeSpan Offset { get; set; }
    }
}
