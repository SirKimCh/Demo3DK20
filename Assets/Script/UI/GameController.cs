using UnityEngine;
using System.Collections.Generic;

namespace Script.UI
{
    public class GameController : MonoBehaviour
    {
        private static GameController _instance;
        public static GameController Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameController");
                    _instance = go.AddComponent<GameController>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private bool _isGamePaused;
        private List<GameObject> _activePanels = new List<GameObject>();
        private Dictionary<Rigidbody, bool> _rigidbodyStates = new Dictionary<Rigidbody, bool>();
        private Dictionary<Animator, bool> _animatorStates = new Dictionary<Animator, bool>();
        private Dictionary<CharacterController, bool> _characterControllerStates = new Dictionary<CharacterController, bool>();
        private Dictionary<MonoBehaviour, bool> _scriptStates = new Dictionary<MonoBehaviour, bool>();

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static void PauseGame(GameObject panel)
        {
            if (panel != null && !Instance._activePanels.Contains(panel))
            {
                Instance._activePanels.Add(panel);
                panel.SetActive(true);
            }

            if (Instance._isGamePaused) return;

            Instance._isGamePaused = true;

            StarterAssets.StarterAssetsInputs.SetGameActive(false);
            StarterAssets.StarterAssetsInputs.UnlockCursor();

            DisableAllAnimators();
            DisableAllRigidbodies();
            DisableAllCharacterControllers();
            DisableGameplayScripts();
        }

        public static void ResumeGame(GameObject panel = null)
        {
            if (!Instance._isGamePaused) return;

            if (panel != null && Instance._activePanels.Contains(panel))
            {
                Instance._activePanels.Remove(panel);
                panel.SetActive(false);
            }

            if (Instance._activePanels.Count == 0)
            {
                Instance._isGamePaused = false;

                StarterAssets.StarterAssetsInputs.SetGameActive(true);

                EnableAllAnimators();
                EnableAllRigidbodies();
                EnableAllCharacterControllers();
                EnableGameplayScripts();
            }
        }

        public static void ShowPanel(GameObject panel)
        {
            if (panel == null)
            {
                Debug.LogError("GameController: Panel is null!");
                return;
            }

            PauseGame(panel);
        }

        public static void HidePanel(GameObject panel)
        {
            if (panel == null) return;

            ResumeGame(panel);
        }

        public static bool IsGamePaused()
        {
            return Instance._isGamePaused;
        }

        private static void DisableAllAnimators()
        {
            Instance._animatorStates.Clear();
            Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator anim in animators)
            {
                if (anim.gameObject.activeInHierarchy && !IsUIComponent(anim.gameObject))
                {
                    Instance._animatorStates[anim] = anim.enabled;
                    anim.speed = 0f;
                }
            }
        }

        private static void EnableAllAnimators()
        {
            foreach (var kvp in Instance._animatorStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.speed = 1f;
                }
            }
            Instance._animatorStates.Clear();
        }

        private static void DisableAllRigidbodies()
        {
            Instance._rigidbodyStates.Clear();
            Rigidbody[] rigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (Rigidbody rb in rigidbodies)
            {
                if (!IsUIComponent(rb.gameObject))
                {
                    Instance._rigidbodyStates[rb] = rb.isKinematic;
                    rb.isKinematic = true;
                }
            }
        }

        private static void EnableAllRigidbodies()
        {
            foreach (var kvp in Instance._rigidbodyStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.isKinematic = kvp.Value;
                }
            }
            Instance._rigidbodyStates.Clear();
        }

        private static void DisableAllCharacterControllers()
        {
            Instance._characterControllerStates.Clear();
            CharacterController[] controllers = Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
            foreach (CharacterController cc in controllers)
            {
                if (!IsUIComponent(cc.gameObject))
                {
                    Instance._characterControllerStates[cc] = cc.enabled;
                    cc.enabled = false;
                }
            }
        }

        private static void EnableAllCharacterControllers()
        {
            foreach (var kvp in Instance._characterControllerStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.enabled = kvp.Value;
                }
            }
            Instance._characterControllerStates.Clear();
        }

        private static void DisableGameplayScripts()
        {
            Instance._scriptStates.Clear();
            MonoBehaviour[] scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour script in scripts)
            {
                if (script == null || script == Instance) continue;
                if (IsUIComponent(script.gameObject)) continue;
                if (IsSystemScript(script)) continue;

                Instance._scriptStates[script] = script.enabled;
                script.enabled = false;
            }
        }

        private static void EnableGameplayScripts()
        {
            foreach (var kvp in Instance._scriptStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.enabled = kvp.Value;
                }
            }
            Instance._scriptStates.Clear();
        }

        private static bool IsSystemScript(MonoBehaviour script)
        {
            if (script == null) return true;
            
            string typeName = script.GetType().FullName;
            if (string.IsNullOrEmpty(typeName)) return true;
            
            return typeName.StartsWith("UnityEngine.") || 
                   typeName.StartsWith("Unity.") ||
                   typeName == "Script.UI.GameController" ||
                   typeName == "Login" ||
                   typeName == "Script.Enemy.EnemyAttackHandler";
        }

        private static bool IsUIComponent(GameObject obj)
        {
            return obj.GetComponentInParent<Canvas>() != null;
        }
    }
}
