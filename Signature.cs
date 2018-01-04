using System;
using System.Collections.Generic;
using System.Text;

namespace Project
{
    public class Signature
    {
        List<SignaturePart> _parts = new List<SignaturePart>();
        public List<SignaturePart> Parts { get { return _parts; } }
        public Signature(IEnumerable<SignaturePart> parts) { _parts = new List<SignaturePart>(parts); }
    }
}
