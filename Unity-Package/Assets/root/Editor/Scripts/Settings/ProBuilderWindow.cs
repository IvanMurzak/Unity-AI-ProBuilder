/*
Copyright (c) 2025 Ivan Murzak
Licensed under the MIT License.
See the LICENSE file in the project root for more information.
*/

#nullable enable
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace com.IvanMurzak.Unity.MCP.ProBuilder
{
    public class ProBuilderWindow : EditorWindow
    {
        const string WindowTitle = "ProBuilder";
        const string ProBuilderPackageName = "com.unity.probuilder";
        const string ProBuilderPackagePath = "Packages/com.unity.probuilder/package.json";
        const string ProBuilderMeshType = "UnityEngine.ProBuilder.ProBuilderMesh, Unity.ProBuilder";
        const string ProBuilderEditorType = "UnityEditor.ProBuilder.ProBuilderEditor, Unity.ProBuilder.Editor";
        const string UvEditingType = "UnityEngine.ProBuilder.MeshOperations.UVEditing, Unity.ProBuilder";
        const string ProBuilderWindowType = "UnityEditor.ProBuilder.ProBuilderWindow, Unity.ProBuilder.Editor";

        static AddRequest? proBuilderInstallRequest;
        static string? proBuilderInstallError;

        bool uiInitialized;
        Label? packageStatusLabel;
        Label? runtimeStatusLabel;
        Label? editorStatusLabel;
        Label? uvStatusLabel;
        Label? uvNoteLabel;
        Label? installErrorLabel;
        Button? installButton;
        Button? openProBuilderButton;
        Button? refreshButton;

        [MenuItem("Window/AI ProBuilder", priority = 1008)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProBuilderWindow>(utility: false, title: WindowTitle, focus: true);
            window.minSize = new Vector2(420f, 220f);
            window.SetWindowTitle();
        }

        void OnEnable()
        {
            SetWindowTitle();
            if (uiInitialized)
                RefreshUi();
        }

        void Update()
        {
            if (proBuilderInstallRequest == null || !proBuilderInstallRequest.IsCompleted)
                return;

            if (proBuilderInstallRequest.Status == StatusCode.Failure)
                proBuilderInstallError = proBuilderInstallRequest.Error?.message;
            else
                proBuilderInstallError = null;

            proBuilderInstallRequest = null;
            RefreshUi();
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            AddStyleSheets(root);

            var scroll = new ScrollView();
            scroll.contentContainer.AddToClassList("container");
            root.Add(scroll);

            scroll.Add(BuildHeader());
            scroll.Add(BuildDivider());
            scroll.Add(BuildSetupSection());
            scroll.Add(BuildDivider());
            scroll.Add(BuildActionsSection());

            uiInitialized = true;
            RefreshUi();
        }

        VisualElement BuildHeader()
        {
            var header = new VisualElement();
            var title = new Label(WindowTitle);
            title.AddToClassList("header");
            header.Add(title);

            var description = new Label("Verify ProBuilder setup for AI ProBuilder tools.");
            description.AddToClassList("section-desc");
            header.Add(description);

            return header;
        }

        VisualElement BuildSetupSection()
        {
            var container = new VisualElement();

            var title = new Label("Setup");
            title.AddToClassList("header");
            container.Add(title);

            var description = new Label("Checks package installation, editor integration, and UV tools.");
            description.AddToClassList("section-desc");
            container.Add(description);

            container.Add(BuildStatusRow("ProBuilder Package", out packageStatusLabel));
            container.Add(BuildStatusRow("Runtime API", out runtimeStatusLabel));
            container.Add(BuildStatusRow("Editor API", out editorStatusLabel));
            container.Add(BuildStatusRow("UV Mapping API", out uvStatusLabel));

            uvNoteLabel = new Label("API availability indicates ProBuilder support is present; run a UV tool to validate behavior.");
            uvNoteLabel.AddToClassList("section-desc");
            container.Add(uvNoteLabel);

            installErrorLabel = new Label();
            installErrorLabel.AddToClassList("section-desc");
            container.Add(installErrorLabel);

            installButton = new Button(() =>
            {
                StartProBuilderInstall();
                RefreshUi();
            });
            installButton.AddToClassList("btn-secondary");
            container.Add(installButton);

            return container;
        }

        VisualElement BuildActionsSection()
        {
            var container = new VisualElement();

            var title = new Label("Actions");
            title.AddToClassList("header");
            container.Add(title);

            var row = new VisualElement();
            row.AddToClassList("row-left-align");
            container.Add(row);

            refreshButton = new Button(() => RefreshUi())
            {
                text = "Refresh"
            };
            refreshButton.AddToClassList("btn-primary");
            row.Add(refreshButton);

            openProBuilderButton = new Button(() =>
            {
                if (!TryOpenProBuilderWindow())
                {
                    EditorUtility.DisplayDialog(
                        "ProBuilder Window",
                        "Unable to open the ProBuilder window. Please ensure ProBuilder is installed.",
                        "OK");
                }
            })
            { text = "Open ProBuilder Window" };
            openProBuilderButton.AddToClassList("btn-secondary");
            row.Add(openProBuilderButton);

            return container;
        }

        void AddStyleSheets(VisualElement root)
        {
            var styleSheet = EditorAssetLoader.LoadAssetAtPath<StyleSheet>(
                EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/MainWindow.uss"));
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
        }

        void RefreshUi()
        {
            if (!uiInitialized)
                return;

            if (titleContent.image == null)
                SetWindowTitle();

            var isInstalled = IsProBuilderInstalled();
            var hasRuntime = HasType(ProBuilderMeshType);
            var hasEditor = HasType(ProBuilderEditorType);
            var uvStatus = GetUvStatus();
            var canOpenProBuilder = GetProBuilderWindowType() != null;

            if (packageStatusLabel != null)
                packageStatusLabel.text = isInstalled ? "Installed" : "Not installed";
            if (runtimeStatusLabel != null)
                runtimeStatusLabel.text = hasRuntime ? "Available" : "Missing";
            if (editorStatusLabel != null)
                editorStatusLabel.text = hasEditor ? "Available" : "Missing";
            if (uvStatusLabel != null)
                uvStatusLabel.text = uvStatus;

            if (installErrorLabel != null)
            {
                var hasError = !string.IsNullOrWhiteSpace(proBuilderInstallError);
                installErrorLabel.style.display = hasError ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasError)
                    installErrorLabel.text = $"ProBuilder install failed: {proBuilderInstallError}";
            }

            var needsInstall = !isInstalled || !hasRuntime || !hasEditor || string.Equals(uvStatus, "Missing", StringComparison.Ordinal);
            if (installButton != null)
            {
                if (proBuilderInstallRequest != null)
                    installButton.text = "Installing ProBuilder...";
                else
                    installButton.text = isInstalled ? "Reinstall ProBuilder" : "Install ProBuilder";

                installButton.SetEnabled(proBuilderInstallRequest == null);
                installButton.style.display = needsInstall ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (refreshButton != null)
                refreshButton.SetEnabled(proBuilderInstallRequest == null);

            if (openProBuilderButton != null)
                openProBuilderButton.SetEnabled(canOpenProBuilder);
        }

        static VisualElement BuildDivider()
        {
            var divider = new VisualElement();
            divider.AddToClassList("divider");
            return divider;
        }

        static VisualElement BuildStatusRow(string labelText, out Label statusLabel)
        {
            var row = new VisualElement();
            row.AddToClassList("row");

            var label = new Label(labelText);
            label.AddToClassList("section-text");
            row.Add(label);

            statusLabel = new Label();
            statusLabel.AddToClassList("section-desc");
            row.Add(statusLabel);

            return row;
        }

        static bool HasType(string typeName)
            => Type.GetType(typeName) != null;

        static string GetUvStatus()
        {
            var uvType = Type.GetType(UvEditingType);
            if (uvType == null)
                return "Missing";

            var methodNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var method in uvType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                methodNames.Add(method.Name);

            var supported = new List<string>();
            if (methodNames.Contains("ProjectFacesAuto"))
                supported.Add("Auto");
            if (methodNames.Contains("ProjectFacesBox"))
                supported.Add("Box");
            if (methodNames.Contains("ProjectFacesPlanar"))
                supported.Add("Planar");

            if (supported.Count == 0 && methodNames.Contains("ProjectFaces"))
                supported.Add("Generic");

            return supported.Count == 0
                ? "Missing"
                : $"Available ({string.Join(", ", supported)})";
        }

        static bool IsProBuilderInstalled()
            => PackageInfo.FindForAssetPath(ProBuilderPackagePath) != null;

        static void StartProBuilderInstall()
        {
            proBuilderInstallError = null;
            proBuilderInstallRequest = Client.Add(ProBuilderPackageName);
        }

        static bool TryOpenProBuilderWindow()
        {
            var windowType = GetProBuilderWindowType();
            if (windowType == null)
                return false;

            EditorWindow.GetWindow(windowType, false, "ProBuilder");
            return true;
        }

        static Type? GetProBuilderWindowType()
        {
            var type = Type.GetType(ProBuilderEditorType);
            if (type != null && typeof(EditorWindow).IsAssignableFrom(type))
                return type;

            type = Type.GetType(ProBuilderWindowType);
            if (type != null && typeof(EditorWindow).IsAssignableFrom(type))
                return type;

            foreach (var candidate in TypeCache.GetTypesDerivedFrom<EditorWindow>())
            {
                if (candidate.FullName == null)
                    continue;

                if (!candidate.FullName.Contains("ProBuilder", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (candidate.Name.Contains("Window", StringComparison.OrdinalIgnoreCase)
                    || candidate.Name.Contains("Editor", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        void SetWindowTitle()
        {
            var icon = EditorAssetLoader.LoadAssetAtPath<UnityEngine.Texture>(EditorAssetLoader.PackageLogoIcon);
            titleContent = icon == null
                ? new GUIContent(WindowTitle)
                : new GUIContent(WindowTitle, icon);
        }
    }
}
#endif
