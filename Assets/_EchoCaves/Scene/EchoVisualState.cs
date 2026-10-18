using UnityEngine;

namespace EchoCaves.Scene
{
    /// <summary>Only switches child artwork; no unlock, trigger, economy, sound, or quest logic.</summary>
    public sealed class EchoVisualState : MonoBehaviour
    {
        public string[] StateNames;
        public GameObject[] StateArt;
        public int Current;
        public void ShowState(int index)
        {
            if(StateArt==null||index<0||index>=StateArt.Length)return;
            Current=index;for(int i=0;i<StateArt.Length;i++)if(StateArt[i])StateArt[i].SetActive(i==index);
        }
    }
}
