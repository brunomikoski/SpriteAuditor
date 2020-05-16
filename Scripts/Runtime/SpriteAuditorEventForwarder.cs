using System;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteAuditorEventForwarder : MonoBehaviour
    {
        private IProjectUpdateLoopListener listener;


        private void Awake()
        {
            listener?.OnProjectAwake();
        }

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
