using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FixData
{
    public class GoogleJsonData
    {
        public string title { get; set; }
        public Phototakentime photoTakenTime { get; set; }
    }

    public class Phototakentime
    {
        public string timestamp { get; set; }
    }

}
