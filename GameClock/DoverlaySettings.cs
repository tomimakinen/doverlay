using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameClock
{
    [XmlRoot("Context")]
    //[XmlArray("ProcessSettings")]
    public class Settings : List<ProcessSettings> { }

    public class ProcessSettings
    {
        private ProcessSettings() { }
        public ProcessSettings(string name) { ProcessName = name; }
        public string ProcessName { get; set; }
        public bool Enabled { get; set; }
        public bool ShowTime {get;set;}
        public bool EnableCaptureProcess { get; set; }
        public string CaptureProcessName {get;set;}
        public int CaptureSpeed {get;set;}
        public Point CapturePoint { get; set; }
        public Size CaptureSize { get; set; }
        public Point OverlayPoint { get; set; }
        public Size OverlaySize { get; set; }
        public string OverlayTransparentColor { get; set; }
    }
}
