using UnityEngine;

namespace POPO.Dialogue
{
    [System.Serializable]
    public class DLine
    {
        public string speakerName;

        [TextArea(2, 5)]
        public string text;

        // If empty, dialogue continues normally
        public DChoice[] choices;
    }
}