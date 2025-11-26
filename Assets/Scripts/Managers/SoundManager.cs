using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class SoundManager : MonoBehaviour
{
    public AudioMixer mixer;
    [Header("each bg source have to be same as scene name")]
    public AudioSource[] bgList;
    private AudioSource bgSound;
    #region 싱글톤

    public static SoundManager Instance => SingletonHelper<SoundManager>.Instance;

    private void Awake()
    {
        SingletonHelper<SoundManager>.InitializeSingleton(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    #endregion

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < bgList.Length; i++)
        {
            if (scene.name == bgList[i].clip.name)
            {
                bgSound = bgList[i];
                BgSoundPlay(bgList[i].clip);
            }   
        }
    }
    
    /// <summary>
    /// 효과음이 달린 오브젝트에서, 해당 함수를 호출해야함.
    /// </summary>
    /// <param name="sfxName"></param>
    /// <param name="clip"></param>
    public void SFXPlay(string sfxName, AudioClip clip)
    {
        GameObject go = new GameObject(sfxName + "Sound");
        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
        audioSource.clip = clip;
        audioSource.Play();

        Destroy(go, clip.length);
    }

    /// <summary>
    /// 슬라이드 UI의 onValueChanged에 해당 함수를 추가하세요
    /// </summary>
    /// <param name="val"></param>
    public void BGSoundVolume(float val)
    {
        mixer.SetFloat("BGMVolume", Mathf.Log10(val)*20);
    }
    
    /// <summary>
    /// 슬라이드 UI의 onValueChanged에 해당 함수를 추가하세요
    /// </summary>
    /// <param name="val"></param>
    public void SFXSoundVolume(float val)
    {
        mixer.SetFloat("SFXVolume", Mathf.Log10(val)*20);
    }

    /// <summary>
    /// 브금이 달린 오브젝트(this)에서, 해당 함수를 호출해야함.
    /// </summary>
    /// <param name="clip"></param>
    public void BgSoundPlay(AudioClip clip)
    {
        foreach (var sound in bgList)
        {
            sound.Stop();
        }
        
        bgSound.outputAudioMixerGroup = mixer.FindMatchingGroups("BGM")[0];
        bgSound.clip = clip;
        bgSound.loop = true;
        bgSound.volume = 0.1f;
        bgSound.Play();

        
    }
}
