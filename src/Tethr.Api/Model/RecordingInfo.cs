using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tethr.Api.Model
{
    public class RecordingInfo
    {
        public string SessionId { get; set; }
        public string MasterCallId { get; set; }
        public Audio Audio { get; set; }
        public JObject Metadata { get; set; }

        public List<Contact> Contacts { get; set; }
        public string Direction { get; set; }
        public string NumberDialed { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool SendToTethr { get; set; }
    }

    public class Audio
    {
        public string Format { get; set; }
    }

    public class Contact
    {
        public int Channel { get; set; }
        public string PhoneNumber { get; set; }
        public string Type { get; set; }
    }
}