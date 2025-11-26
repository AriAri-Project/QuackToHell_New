using UnityEngine;

public class SoundPlay : MonoBehaviour
{
    public AudioSource buttonClickSFX;
    
    public void SFXPlay()
    {
        SoundManager.Instance.SFXPlay(buttonClickSFX.name, buttonClickSFX.clip);
    }
}
