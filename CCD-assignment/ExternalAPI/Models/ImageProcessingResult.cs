using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalAPI.Models
{
    public sealed record ImageProcessingResult(byte[] Data, Uri Uri);
}
