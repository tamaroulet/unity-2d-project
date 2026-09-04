// SPDX-AI-Disclosure: ai-generated
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// UXML が Unity にパースでき、C# 側が参照する name が実在することを検証する。
    ///
    /// UI Toolkit へ移行する狙いは「UI がテキストになりエージェントが編集できる」ことだが、
    /// UXML の破損や name の綴り違いは C# のコンパイルでは検出できない。
    /// シーンに繋ぐ前にここで落とす。
    /// </summary>
    public class UxmlBindingTests
    {
        private const string EventDialogUxml = "Assets/UI/UXML/EventDialog.uxml";

        private static VisualElement LoadAndInstantiate(string assetPath)
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            Assert.IsNotNull(tree, $"UXML を読み込めなかった: {assetPath}");
            VisualElement root = tree.Instantiate();
            Assert.IsNotNull(root, $"UXML をインスタンス化できなかった: {assetPath}");
            return root;
        }

        [Test]
        public void EventDialog_Uxml_ParsesAndExposesRequiredNames()
        {
            VisualElement root = LoadAndInstantiate(EventDialogUxml);

            Assert.IsNotNull(root.Q<Label>("TitleLabel"),
                "EventDialog.uxml に name=\"TitleLabel\" の Label が無い");
            Assert.IsNotNull(root.Q<Label>("BodyLabel"),
                "EventDialog.uxml に name=\"BodyLabel\" の Label が無い");
            Assert.IsNotNull(root.Q<Button>("OkButton"),
                "EventDialog.uxml に name=\"OkButton\" の Button が無い");
            Assert.IsNotNull(root.Q<VisualElement>("DialogOverlay"),
                "EventDialog.uxml に name=\"DialogOverlay\" の VisualElement が無い");
        }

        [Test]
        public void EventDialog_Uss_Exists()
        {
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/USS/EventDialog.uss");
            Assert.IsNotNull(sheet, "EventDialog.uss を StyleSheet として読み込めなかった（USS の構文エラーの可能性）");
        }
    }
}
