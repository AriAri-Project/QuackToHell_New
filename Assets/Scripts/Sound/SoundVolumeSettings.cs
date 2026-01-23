using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 AudioClip의 개별 볼륨 설정을 저장하는 ScriptableObject
/// Resources 폴더에 저장하여 런타임에서 접근 가능
/// </summary>
[CreateAssetMenu(fileName = "SoundVolumeSettings", menuName = "Pansori/Sound Volume Settings")]
public class SoundVolumeSettings : ScriptableObject
{
    private const string RESOURCE_PATH = "SoundVolumeSettings";
    private const float DEFAULT_VOLUME = 0.5f;
    
    [Serializable]
    public class VolumeEntry
    {
        public string clipGuid;      // AudioClip의 GUID (에디터에서 사용)
        public string clipName;      // AudioClip 이름 (런타임 폴백용)
        public string clipPath;      // AudioClip 경로 (표시용)
        [Range(0f, 1f)]
        public float volume = DEFAULT_VOLUME;
        
        public VolumeEntry(string guid, string name, string path, float vol = 0.5f)
        {
            clipGuid = guid;
            clipName = name;
            clipPath = path;
            volume = vol;
        }
    }
    
    [SerializeField]
    private List<VolumeEntry> volumeEntries = new List<VolumeEntry>();
    
    // 빠른 조회를 위한 캐시 딕셔너리
    private Dictionary<string, float> guidToVolumeCache;
    private Dictionary<string, float> nameToVolumeCache;
    
    // 싱글톤 인스턴스 (런타임용)
    private static SoundVolumeSettings instance;
    public static SoundVolumeSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SoundVolumeSettings>(RESOURCE_PATH);
                if (instance == null)
                {
                    Debug.LogWarning("[SoundVolumeSettings] Resources 폴더에서 SoundVolumeSettings를 찾을 수 없습니다. 기본 볼륨을 사용합니다.");
                }
                else
                {
                    instance.BuildCache();
                }
            }
            return instance;
        }
    }
    
    /// <summary>
    /// 볼륨 엔트리 목록 (에디터용)
    /// </summary>
    public List<VolumeEntry> VolumeEntries => volumeEntries;
    
    /// <summary>
    /// 캐시 딕셔너리 빌드
    /// </summary>
    public void BuildCache()
    {
        guidToVolumeCache = new Dictionary<string, float>();
        nameToVolumeCache = new Dictionary<string, float>();
        
        foreach (var entry in volumeEntries)
        {
            if (!string.IsNullOrEmpty(entry.clipGuid))
            {
                guidToVolumeCache[entry.clipGuid] = entry.volume;
            }
            if (!string.IsNullOrEmpty(entry.clipName))
            {
                nameToVolumeCache[entry.clipName] = entry.volume;
            }
        }
    }
    
    /// <summary>
    /// AudioClip의 설정된 볼륨을 가져옵니다
    /// </summary>
    /// <param name="clip">AudioClip</param>
    /// <returns>설정된 볼륨 (0~1), 설정이 없으면 기본값 반환</returns>
    public float GetVolume(AudioClip clip)
    {
        if (clip == null) return DEFAULT_VOLUME;
        
        // 캐시가 없으면 빌드
        if (guidToVolumeCache == null || nameToVolumeCache == null)
        {
            BuildCache();
        }
        
        // 이름으로 검색 (런타임에서는 GUID 접근 불가)
        if (nameToVolumeCache != null && nameToVolumeCache.TryGetValue(clip.name, out float volume))
        {
            return volume;
        }
        
        return DEFAULT_VOLUME;
    }
    
    /// <summary>
    /// 클립 이름으로 볼륨을 가져옵니다
    /// </summary>
    /// <param name="clipName">클립 이름</param>
    /// <returns>설정된 볼륨</returns>
    public float GetVolumeByName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return DEFAULT_VOLUME;
        
        if (nameToVolumeCache == null)
        {
            BuildCache();
        }
        
        if (nameToVolumeCache != null && nameToVolumeCache.TryGetValue(clipName, out float volume))
        {
            return volume;
        }
        
        return DEFAULT_VOLUME;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// GUID로 볼륨을 가져옵니다 (에디터 전용)
    /// </summary>
    public float GetVolumeByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return DEFAULT_VOLUME;
        
        if (guidToVolumeCache == null)
        {
            BuildCache();
        }
        
        if (guidToVolumeCache != null && guidToVolumeCache.TryGetValue(guid, out float volume))
        {
            return volume;
        }
        
        return DEFAULT_VOLUME;
    }
    
    /// <summary>
    /// 볼륨 엔트리 설정 (에디터 전용)
    /// </summary>
    public void SetVolume(string guid, string name, string path, float volume)
    {
        var entry = volumeEntries.Find(e => e.clipGuid == guid);
        if (entry != null)
        {
            entry.volume = volume;
            entry.clipName = name;
            entry.clipPath = path;
        }
        else
        {
            volumeEntries.Add(new VolumeEntry(guid, name, path, volume));
        }
        
        // 캐시 업데이트
        if (guidToVolumeCache == null) guidToVolumeCache = new Dictionary<string, float>();
        if (nameToVolumeCache == null) nameToVolumeCache = new Dictionary<string, float>();
        
        guidToVolumeCache[guid] = volume;
        if (!string.IsNullOrEmpty(name))
        {
            nameToVolumeCache[name] = volume;
        }
    }
    
    /// <summary>
    /// 엔트리 존재 여부 확인 (에디터 전용)
    /// </summary>
    public bool HasEntry(string guid)
    {
        return volumeEntries.Exists(e => e.clipGuid == guid);
    }
    
    /// <summary>
    /// 사용하지 않는 엔트리 정리 (에디터 전용)
    /// </summary>
    public int CleanupUnusedEntries(HashSet<string> validGuids)
    {
        int removed = volumeEntries.RemoveAll(e => !validGuids.Contains(e.clipGuid));
        if (removed > 0)
        {
            BuildCache();
        }
        return removed;
    }
    
    /// <summary>
    /// 모든 엔트리 초기화 (에디터 전용)
    /// </summary>
    public void ResetAllVolumes()
    {
        foreach (var entry in volumeEntries)
        {
            entry.volume = DEFAULT_VOLUME;
        }
        BuildCache();
    }
    #endif
}