using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuackToHell.Sound.Editor
{
    /// <summary>
    /// 프로젝트 내 모든 AudioClip의 볼륨을 조절할 수 있는 에디터 윈도우
    /// </summary>
    public class SoundVolumeEditor : EditorWindow
    {
        // 설정 에셋
        private SoundVolumeSettings settings;
        private SerializedObject serializedSettings;
        
        // AudioClip 데이터
        private List<AudioClipInfo> allClips = new List<AudioClipInfo>();
        private Dictionary<string, List<AudioClipInfo>> folderGroups = new Dictionary<string, List<AudioClipInfo>>();
        private Dictionary<string, bool> folderFoldouts = new Dictionary<string, bool>();
        
        // UI 상태
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private bool showOnlyModified = false;
        
        // 미리듣기
        private AudioClip previewClip;
        private bool isPlaying = false;
        
        private class AudioClipInfo
        {
            public string guid;
            public string name;
            public string path;
            public string folderPath;
            public AudioClip clip;
            public float volume;
            public bool isModified;
            
            public AudioClipInfo(string guid, string path, AudioClip clip)
            {
                this.guid = guid;
                this.path = path;
                this.name = clip != null ? clip.name : System.IO.Path.GetFileNameWithoutExtension(path);
                this.clip = clip;
                this.volume = 0.5f;
                this.isModified = false;
                
                // 폴더 경로 추출 (Assets/ 제외)
                int lastSlash = path.LastIndexOf('/');
                this.folderPath = lastSlash > 0 ? path.Substring(0, lastSlash) : "Root";
                if (this.folderPath.StartsWith("Assets/"))
                {
                    this.folderPath = this.folderPath.Substring(7);
                }
            }
        }
        
        [MenuItem("Tools/Sound/Sound Volume Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SoundVolumeEditor>("Sound Volume Editor");
            window.minSize = new Vector2(450, 400);
        }
        
        private void OnEnable()
        {
            LoadOrCreateSettings();
            ScanAudioClips();
        }
        
        private void OnDisable()
        {
            StopPreview();
        }
        
        private void LoadOrCreateSettings()
        {
            // Resources 폴더에서 설정 찾기
            string[] guids = AssetDatabase.FindAssets("t:SoundVolumeSettings", new[] { "Assets/Resources" });
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<SoundVolumeSettings>(path);
            }
            
            if (settings == null)
            {
                // Resources 폴더 확인/생성
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                // 새 설정 에셋 생성
                settings = ScriptableObject.CreateInstance<SoundVolumeSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Resources/SoundVolumeSettings.asset");
                AssetDatabase.SaveAssets();
                Debug.Log("[SoundVolumeEditor] SoundVolumeSettings.asset 생성됨");
            }
            
            serializedSettings = new SerializedObject(settings);
        }
        
        private void ScanAudioClips()
        {
            allClips.Clear();
            folderGroups.Clear();
            
            // 프로젝트 내 모든 AudioClip 검색
            string[] guids = AssetDatabase.FindAssets("t:AudioClip");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                
                if (clip != null)
                {
                    var clipInfo = new AudioClipInfo(guid, path, clip);
                    
                    // 저장된 볼륨 로드
                    if (settings != null && settings.HasEntry(guid))
                    {
                        clipInfo.volume = settings.GetVolumeByGuid(guid);
                        clipInfo.isModified = true;
                    }
                    
                    allClips.Add(clipInfo);
                    
                    // 폴더별 그룹화
                    if (!folderGroups.ContainsKey(clipInfo.folderPath))
                    {
                        folderGroups[clipInfo.folderPath] = new List<AudioClipInfo>();
                        folderFoldouts[clipInfo.folderPath] = false;
                    }
                    folderGroups[clipInfo.folderPath].Add(clipInfo);
                }
            }
            
            // 폴더 이름으로 정렬
            var sortedFolders = folderGroups.Keys.OrderBy(k => k).ToList();
            var sortedGroups = new Dictionary<string, List<AudioClipInfo>>();
            foreach (var folder in sortedFolders)
            {
                sortedGroups[folder] = folderGroups[folder].OrderBy(c => c.name).ToList();
            }
            folderGroups = sortedGroups;
            
            Debug.Log("[SoundVolumeEditor] " + allClips.Count + "개의 AudioClip 스캔 완료");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            
            // 헤더
            EditorGUILayout.LabelField("Sound Volume Editor", EditorStyles.boldLabel);
            
            // 툴바
            DrawToolbar();
            
            EditorGUILayout.Space(5);
            
            // 검색 및 필터
            DrawSearchBar();
            
            EditorGUILayout.Space(5);
            
            // 구분선
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // 클립 목록
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawClipList();
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("스캔", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ScanAudioClips();
            }
            
            if (GUILayout.Button("모두 저장", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                SaveAllSettings();
            }
            
            if (GUILayout.Button("리셋", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("볼륨 리셋", "모든 볼륨을 50%로 초기화하시겠습니까?", "확인", "취소"))
                {
                    ResetAllVolumes();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            // 미리듣기 정지 버튼
            GUI.enabled = isPlaying;
            if (GUILayout.Button("정지", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                StopPreview();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            showOnlyModified = EditorGUILayout.Toggle("수정된 항목만", showOnlyModified, GUILayout.Width(120));
            
            GUILayout.FlexibleSpace();
            
            int displayCount = GetFilteredClipCount();
            EditorGUILayout.LabelField("클립 수: " + displayCount + " / " + allClips.Count, GUILayout.Width(120));
            
            EditorGUILayout.EndHorizontal();
        }
        
        private int GetFilteredClipCount()
        {
            return allClips.Count(c => PassesFilter(c));
        }
        
        private bool PassesFilter(AudioClipInfo clipInfo)
        {
            if (showOnlyModified && !clipInfo.isModified)
                return false;
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                string lowerFilter = searchFilter.ToLower();
                if (!clipInfo.name.ToLower().Contains(lowerFilter) &&
                    !clipInfo.path.ToLower().Contains(lowerFilter))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void DrawClipList()
        {
            if (allClips.Count == 0)
            {
                EditorGUILayout.HelpBox("AudioClip이 없습니다. '스캔' 버튼을 눌러 프로젝트를 스캔하세요.", MessageType.Info);
                return;
            }
            
            foreach (var folder in folderGroups)
            {
                // 폴더 내 필터 통과 클립 확인
                var filteredClips = folder.Value.Where(c => PassesFilter(c)).ToList();
                if (filteredClips.Count == 0)
                    continue;
                
                // 폴더 foldout
                EditorGUILayout.BeginHorizontal();
                
                if (!folderFoldouts.ContainsKey(folder.Key))
                    folderFoldouts[folder.Key] = false;
                
                folderFoldouts[folder.Key] = EditorGUILayout.Foldout(folderFoldouts[folder.Key], 
                    folder.Key + " (" + filteredClips.Count + ")", true);
                
                EditorGUILayout.EndHorizontal();
                
                if (folderFoldouts[folder.Key])
                {
                    EditorGUI.indentLevel++;
                    
                    foreach (var clipInfo in filteredClips)
                    {
                        DrawClipEntry(clipInfo);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        private void DrawClipEntry(AudioClipInfo clipInfo)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 수정 표시
            Color originalColor = GUI.color;
            if (clipInfo.isModified)
            {
                GUI.color = new Color(0.8f, 1f, 0.8f);
            }
            
            // 클립 이름
            EditorGUILayout.LabelField(clipInfo.name, GUILayout.Width(180));
            
            // 볼륨 슬라이더
            EditorGUI.BeginChangeCheck();
            float newVolume = EditorGUILayout.Slider(clipInfo.volume, 0f, 1f, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                clipInfo.volume = newVolume;
                clipInfo.isModified = true;
                
                // 실시간 저장
                if (settings != null)
                {
                    settings.SetVolume(clipInfo.guid, clipInfo.name, clipInfo.path, newVolume);
                    EditorUtility.SetDirty(settings);
                }
            }
            
            // 볼륨 퍼센트 표시
            EditorGUILayout.LabelField(Mathf.RoundToInt(clipInfo.volume * 100) + "%", GUILayout.Width(40));
            
            GUI.color = originalColor;
            
            // 미리듣기 버튼
            bool isThisPlaying = isPlaying && previewClip == clipInfo.clip;
            string playButtonText = isThisPlaying ? "||" : ">";
            
            if (GUILayout.Button(playButtonText, GUILayout.Width(25)))
            {
                if (isThisPlaying)
                {
                    StopPreview();
                }
                else
                {
                    PlayPreview(clipInfo);
                }
            }
            
            // 클립 선택 버튼
            if (GUILayout.Button("O", GUILayout.Width(25)))
            {
                Selection.activeObject = clipInfo.clip;
                EditorGUIUtility.PingObject(clipInfo.clip);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void PlayPreview(AudioClipInfo clipInfo)
        {
            StopPreview();
            
            if (clipInfo.clip == null) return;
            
            previewClip = clipInfo.clip;
            isPlaying = true;
            
            // Unity 에디터 내부 AudioUtil 사용
            System.Type audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass != null)
            {
                MethodInfo playClipMethod = audioUtilClass.GetMethod("PlayPreviewClip",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null);
                
                if (playClipMethod != null)
                {
                    playClipMethod.Invoke(null, new object[] { clipInfo.clip, 0, false });
                    
                    // 재생 종료 체크를 위한 EditorApplication.update 등록
                    EditorApplication.update += CheckPreviewEnded;
                }
            }
        }
        
        private void StopPreview()
        {
            if (!isPlaying) return;
            
            System.Type audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass != null)
            {
                MethodInfo stopMethod = audioUtilClass.GetMethod("StopAllPreviewClips",
                    BindingFlags.Static | BindingFlags.Public);
                
                if (stopMethod != null)
                {
                    stopMethod.Invoke(null, null);
                }
            }
            
            previewClip = null;
            isPlaying = false;
            EditorApplication.update -= CheckPreviewEnded;
            Repaint();
        }
        
        private void CheckPreviewEnded()
        {
            if (!isPlaying) return;
            
            System.Type audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass != null)
            {
                MethodInfo isPlayingMethod = audioUtilClass.GetMethod("IsPreviewClipPlaying",
                    BindingFlags.Static | BindingFlags.Public);
                
                if (isPlayingMethod != null)
                {
                    bool stillPlaying = (bool)isPlayingMethod.Invoke(null, null);
                    if (!stillPlaying)
                    {
                        previewClip = null;
                        isPlaying = false;
                        EditorApplication.update -= CheckPreviewEnded;
                        Repaint();
                    }
                }
            }
        }
        
        private void SaveAllSettings()
        {
            if (settings == null)
            {
                LoadOrCreateSettings();
            }
            
            foreach (var clipInfo in allClips)
            {
                settings.SetVolume(clipInfo.guid, clipInfo.name, clipInfo.path, clipInfo.volume);
                clipInfo.isModified = true;
            }
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[SoundVolumeEditor] " + allClips.Count + "개 클립의 볼륨 설정 저장됨");
            EditorUtility.DisplayDialog("저장 완료", allClips.Count + "개 클립의 볼륨 설정이 저장되었습니다.", "확인");
        }
        
        private void ResetAllVolumes()
        {
            foreach (var clipInfo in allClips)
            {
                clipInfo.volume = 0.5f;
            }
            
            if (settings != null)
            {
                settings.ResetAllVolumes();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log("[SoundVolumeEditor] 모든 볼륨이 50%로 초기화됨");
        }
    }
}