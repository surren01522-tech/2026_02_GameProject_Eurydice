#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeamlessWormhole.Editor
{
    /// <summary>
    /// Unity Editor에서 Play할 때 키보드와 마우스 입력을 Game 뷰로 보냅니다.
    /// Play가 끝나면 원래 Editor 입력 설정으로 되돌립니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class WormholeEditorInputRouter
    {
        private static InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
        private static InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private static bool hasOverriddenInputSettings;

        static WormholeEditorInputRouter()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeState;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreInputSettings;
        }

        private static void HandlePlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
                previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;

                InputSystem.settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                hasOverriddenInputSettings = true;

                // Inspector를 클릭한 뒤에도 입력 처리는 유지되며, Game 탭도 함께 선택합니다.
                EditorApplication.delayCall += FocusGameView;
                return;
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
                RestoreInputSettings();
        }

        private static void FocusGameView()
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;

            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                if (candidate is EditorWindow gameView)
                {
                    gameView.Focus();
                    return;
                }
            }
        }

        private static void RestoreInputSettings()
        {
            if (!hasOverriddenInputSettings) return;

            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            hasOverriddenInputSettings = false;
        }
    }
}
#endif
