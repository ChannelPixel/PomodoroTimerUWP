using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;


namespace PomodoroSettingsLibrary
{
    public class PresetSettings : DefaultSettings
    {
        private string presetName;

        public PresetSettings(string PresetName)
        {
            this.presetName = PresetName;
        }

        //GETTER METHODS
        public string GetPresetName()
        {
            return this.presetName;
        }

        //SETTER METHODS
        public void SetPresetName(string aString)
        {
            this.presetName = aString;
        }
    }
}
