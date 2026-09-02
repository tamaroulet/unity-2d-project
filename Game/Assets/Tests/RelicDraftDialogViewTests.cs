// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Relic;
using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class RelicDraftDialogViewTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _createdGameObjects)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _createdGameObjects.Clear();

            foreach (UnityEngine.Object obj in _createdObjects)
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void Show_PopulatesCardViewsAndActivatesPanel()
        {
            RelicDraftDialogView dialog = CreateDialog(out RelicCardView card1, out RelicCardView card2, out RelicCardView card3);
            RelicSO r1 = CreateRelic(1, "Relic1", "Desc1");
            RelicSO r2 = CreateRelic(2, "Relic2", "Desc2");
            RelicSO r3 = CreateRelic(3, "Relic3", "Desc3");

            dialog.Show(new[] { r1, r2, r3 });

            Assert.IsTrue(dialog.IsVisible);
            Assert.AreEqual(r1, card1.BoundRelic);
            Assert.AreEqual(r2, card2.BoundRelic);
            Assert.AreEqual(r3, card3.BoundRelic);
        }

        [Test]
        public void OnCardSelected_RaisesRelicAcquiredChannelAndDismisses()
        {
            RelicDraftDialogView dialog = CreateDialog(out RelicCardView card1, out RelicCardView card2, out RelicCardView card3);
            int acquiredId = -1;
            RelicAcquiredChannelSO channel = ScriptableObject.CreateInstance<RelicAcquiredChannelSO>();
            channel.OnEventRaised += id => acquiredId = id;
            _createdObjects.Add(channel);
            SetField(dialog, "_relicAcquiredChannel", channel);

            RelicSO r1 = CreateRelic(10, "Relic10", "Desc10");
            dialog.Show(new[] { r1 });

            dialog.OnCardSelected(10);

            Assert.AreEqual(10, acquiredId);
            Assert.AreEqual(10, dialog.LastSelectedRelicId);
            Assert.IsFalse(dialog.IsVisible);
        }

        private RelicDraftDialogView CreateDialog(out RelicCardView card1, out RelicCardView card2, out RelicCardView card3)
        {
            GameObject dialogGo = new GameObject("RelicDraftDialog");
            _createdGameObjects.Add(dialogGo);
            RelicDraftDialogView dialog = dialogGo.AddComponent<RelicDraftDialogView>();

            GameObject panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(dialogGo.transform);
            panelRoot.SetActive(false);

            card1 = CreateCardView("Card1", panelRoot);
            card2 = CreateCardView("Card2", panelRoot);
            card3 = CreateCardView("Card3", panelRoot);

            SetField(dialog, "_panelRoot", panelRoot);
            SetField(dialog, "_cardViews", new List<RelicCardView> { card1, card2, card3 });

            return dialog;
        }

        private RelicCardView CreateCardView(string name, GameObject parent)
        {
            GameObject cardGo = new GameObject(name);
            cardGo.transform.SetParent(parent.transform);
            return cardGo.AddComponent<RelicCardView>();
        }

        private RelicSO CreateRelic(int id, string displayName, string desc)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            SetField(relic, "_relicId", id);
            SetField(relic, "_displayName", displayName);
            SetField(relic, "_description", desc);
            _createdObjects.Add(relic);
            return relic;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new System.InvalidOperationException(
                    $"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            }
            field.SetValue(target, value);
        }
    }
}
