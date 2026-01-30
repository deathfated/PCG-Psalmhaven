using UnityEngine;

namespace POPO.Dialogue
{
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Popo/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        public DLine[] lines;
    }

}
