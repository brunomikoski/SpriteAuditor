using UnityEngine;

namespace BrunoMikoski.AtlasAudior
{
    public class AtlasAuditorForwarder : MonoBehaviour
    {
        private IProjectUpdateLoopListener listener;

        private void Update()
        {
            listener?.OnProjectUpdate();
        }

        public void SetListener(IProjectUpdateLoopListener listener)
        {
            this.listener = listener;
        }
    }
}
