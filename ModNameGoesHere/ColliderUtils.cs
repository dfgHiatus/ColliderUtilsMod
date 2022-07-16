using System;
using System.Collections.Generic;
using System.Linq;
using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using FrooxEngine.Undo;

namespace MeshColliderManagementTools
{
    public class ColliderUtils
    {
        public Dictionary<string, string> ReplacementColliderComponent = new Dictionary<string, string>()
        {
            { "BoxCollider", "BoxCollider" },
            { "SphereCollider", "SphereCollider" },
            { "CapsuleCollider", "CapsuleCollider" },
            { "CylinderCollider", "CylinderCollider" },
            { "ConvexHullCollider", "ConvexHullCollider" }
        };

        public Dictionary<string, string> SetupBoundsType = new Dictionary<string, string>()
        {
            { "None", "None" },
            { "SetupFromLocalBounds", "SetupFromLocalBounds" },
            { "SetupFromPreciseBounds", "SetupFromPreciseBounds" }
        };

        public Dictionary<string, string> UseTagMode = new Dictionary<string, string>()
        {
            { "IgnoreTag", "IgnoreTag" },
            { "IncludeOnlyWithTag", "IncludeOnlyWithTag" },
            { "ExcludeAllWithTag", "ExcludeAllWithTag" }
        };

        public static ColliderUtils _Wizard;
        public static Slot WizardSlot;

        public static ValueField<bool> IgnoreInactive;
        public static ValueField<bool> IgnoreDisabled;
        public static ValueField<bool> IgnoreNonPersistent;
        public static ValueField<bool> IgnoreUserHierarchies;
        public static ValueField<bool> PreserveColliderSettings;
        public static ValueField<bool> SetIgnoreRaycasts;
        public static ValueField<bool> SetCharacterCollider;
        public static ValueField<float> Mass;
        public static ValueField<float> HighlightDuration;
        public static ValueField<color> HighlightColor;
        public static ValueField<ColliderType> setColliderType;
        public static ReferenceField<Slot> ProcessRoot;
        public static ReferenceField<TextField> tag;
        public static Text resultsText;

        // TODO Replace this with a dict.
        public static ValueField<string> setupBoundsType;
        public static ValueField<string> replacementColliderComponent;
        public static ValueField<string> useTagMode;

        private int _count;
        private color _buttonColor;
        private LocaleString _text;
        private Slot _scrollAreaRoot;
        private UIBuilder _listBuilder;

        public ColliderUtils()
        {
            _Wizard = this;
            WizardSlot = Engine.Current.WorldManager.FocusedWorld.RootSlot.AddSlot("Dynamic Bone Wizard");
            WizardSlot.GetComponentInChildrenOrParents<Canvas>()?.MarkDeveloper();
            WizardSlot.PersistentSelf = false;
            Slot Data = WizardSlot.AddSlot("Data");

            Awake(Data);
            Attach(Data);
            Start(Data);
        }

        private void Awake(Slot Data)
        {
            IgnoreInactive = Data.AddSlot("IgnoreInactive").AttachComponent<ValueField<bool>>();
            IgnoreInactive.Value.Value = true;

            IgnoreDisabled = Data.AddSlot("IgnoreDisabled").AttachComponent<ValueField<bool>>();
            IgnoreDisabled.Value.Value = true;

            IgnoreNonPersistent = Data.AddSlot("IgnoreNonPersistent").AttachComponent<ValueField<bool>>();
            IgnoreNonPersistent.Value.Value = true;

            IgnoreUserHierarchies = Data.AddSlot("IgnoreUserHierarchies").AttachComponent<ValueField<bool>>();
            IgnoreUserHierarchies.Value.Value = true;

            SetIgnoreRaycasts = Data.AddSlot("SetIgnoreRaycasts").AttachComponent<ValueField<bool>>();
            SetIgnoreRaycasts.Value.Value = true;

            SetCharacterCollider = Data.AddSlot("SetCharacterCollider").AttachComponent<ValueField<bool>>();
            SetCharacterCollider.Value.Value = true;

            setupBoundsType = Data.AddSlot("setupBoundsType").AttachComponent<ValueField<string>>();
            setupBoundsType.Value.Value = SetupBoundsType["SetupFromLocalBounds"];

            useTagMode = Data.AddSlot("useTagMode").AttachComponent<ValueField<string>>();
            useTagMode.Value.Value = UseTagMode["IgnoreTag"];

            setColliderType = Data.AddSlot("setColliderType").AttachComponent<ValueField<ColliderType>>();
            setColliderType.Value.Value = ColliderType.Static;

            replacementColliderComponent = Data.AddSlot("replacementColliderType").AttachComponent<ValueField<string>>();
            replacementColliderComponent.Value.Value = ReplacementColliderComponent["BoxCollider"];

            ProcessRoot = Data.AddSlot("ProcessRoot").AttachComponent<ReferenceField<Slot>>();
            ProcessRoot.Reference.Target = null;

            PreserveColliderSettings = Data.AddSlot("PreserveColliderSettings").AttachComponent<ValueField<bool>>();
            PreserveColliderSettings.Value.Value = true;

            HighlightDuration = Data.AddSlot("HighlightDuration").AttachComponent<ValueField<float>>();
            HighlightDuration.Value.Value = 1f;

            HighlightColor = Data.AddSlot("HighlightColor").AttachComponent<ValueField<color>>();
            HighlightColor.Value.Value = new color(1f, 1f, 1f);

            Mass = Data.AddSlot("Mass").AttachComponent<ValueField<float>>();
            Mass.Value.Value = 1f;
        }

        private void Attach(Slot Data)
        {
            UniLog.Log("1");
            // Create the UI for the wizard.
            Data.Name = "MeshCollider Management Wizard";
            Data.Tag = "Developer";
            NeosCanvasPanel neosCanvasPanel = Data.AttachComponent<NeosCanvasPanel>();
            neosCanvasPanel.Panel.AddCloseButton();
            neosCanvasPanel.Panel.AddParentButton();
            neosCanvasPanel.Panel.Title = "MeshCollider Management Wizard";
            neosCanvasPanel.CanvasSize = new float2(800f, 900f);
            UIBuilder uIBuilder = new UIBuilder(neosCanvasPanel.Canvas);
            List<RectTransform> rectList = uIBuilder.SplitHorizontally(0.5f, 0.5f);

            UniLog.Log("2");
            // Build left hand side UI - options and buttons.
            UIBuilder uIBuilder2 = new UIBuilder(rectList[0].Slot);
            Slot _layoutRoot = uIBuilder2.VerticalLayout(4f, 0f, new Alignment()).Slot;
            uIBuilder2.FitContent(SizeFit.Disabled, SizeFit.MinSize);
            uIBuilder2.Style.Height = 24f;
            UIBuilder uIBuilder3 = uIBuilder2;

            UniLog.Log("3");
            // Slot reference to which changes will be applied.
            _text = "Process root slot:";
            uIBuilder3.Text(in _text);
            uIBuilder3.Next("Root");
            uIBuilder3.Current.AttachComponent<RefEditor>().Setup(ProcessRoot.Reference);
            uIBuilder3.Spacer(24f);

            UniLog.Log("4");
            // Basic filtering settings for which MeshColliders are accepted for changes or listing.
            _text = "Ignore inactive:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(IgnoreInactive.Value));
            _text = "Ignore disabled:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(IgnoreDisabled.Value));
            _text = "Ignore non-persistent:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(IgnoreNonPersistent.Value));
            _text = "Ignore user hierarchies:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(IgnoreUserHierarchies.Value));
            _text = "Tag:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.2f, () => uIBuilder3.TextField());
            _text = "Tag handling mode:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.5f, () => uIBuilder3.PrimitiveMemberEditor(useTagMode.Value));
            uIBuilder3.Spacer(24f);

            UniLog.Log("5");
            // Settings for highlighing individual colliders.
            _text = "Highlight duration:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.8f, () => uIBuilder3.PrimitiveMemberEditor(HighlightDuration.Value));
            _text = "Highlight color:";
            uIBuilder3.Text(in _text);
            uIBuilder3.ColorMemberEditor(HighlightColor.Value);
            uIBuilder3.Spacer(24f);

            // Controls for specific replacement collider settings.
            _text = "Replacement collider component:";
            uIBuilder3.Text(in _text);
            uIBuilder3.PrimitiveMemberEditor(replacementColliderComponent.Value);
            _text = "Replacement setup action:";
            uIBuilder3.Text(in _text);
            uIBuilder3.PrimitiveMemberEditor(setupBoundsType.Value);
            uIBuilder3.Spacer(24f);
            _text = "Preserve existing collider settings:";
            uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(PreserveColliderSettings.Value));
            _text = "Set collision Type:";
            uIBuilder3.Text(in _text);
            Slot _hideTextSlot = _layoutRoot.GetAllChildren().Last();
            uIBuilder3.EnumMemberEditor(setColliderType.Value);
            Slot _hideEnumSlot = _layoutRoot.GetAllChildren().Last().Parent.Parent;
            _text = "Collider Mass:";
            Slot _hideFloatSlot = uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.PrimitiveMemberEditor(Mass.Value)).Slot.Parent;
            _text = "Set CharacterCollider:";
            Slot _hideBoolSlot1 = uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(SetCharacterCollider.Value)).Slot.Parent;
            _text = "Set IgnoreRaycasts:";
            Slot _hideBoolSlot2 = uIBuilder3.HorizontalElementWithLabel(in _text, 0.9f, () => uIBuilder3.BooleanMemberEditor(SetIgnoreRaycasts.Value)).Slot.Parent;
            uIBuilder3.Spacer(24f);

            // Hide some options if preserving existing settings.
            var _valCopy = _layoutRoot.AttachComponent<ValueCopy<bool>>();
            var _boolValDriver = _layoutRoot.AttachComponent<BooleanValueDriver<bool>>();
            var _valMultiDriver = _layoutRoot.AttachComponent<ValueMultiDriver<bool>>();
            _valCopy.Source.Target = PreserveColliderSettings.Value;
            _valCopy.Target.Target = _boolValDriver.State;
            _boolValDriver.TrueValue.Value = false;
            _boolValDriver.FalseValue.Value = true;
            _boolValDriver.TargetField.Target = _valMultiDriver.Value;
            for (int i = 0; i < 5; i++)
            {
                _valMultiDriver.Drives.Add();
            }
            _valMultiDriver.Drives[0].Target = _hideTextSlot.ActiveSelf_Field;
            _valMultiDriver.Drives[1].Target = _hideEnumSlot.ActiveSelf_Field;
            _valMultiDriver.Drives[2].Target = _hideBoolSlot1.ActiveSelf_Field;
            _valMultiDriver.Drives[3].Target = _hideBoolSlot2.ActiveSelf_Field;
            _valMultiDriver.Drives[4].Target = _hideFloatSlot.ActiveSelf_Field;

            // Buttons for batch actions.
            _text = "List matching MeshColliders";
            uIBuilder3.Button(in _text).LocalPressed += PopulateList;
            _text = "Replace all matching MeshColliders";
            uIBuilder3.Button(in _text).LocalPressed += ReplaceAll;
            _text = "Remove all matching MeshColliders";
            uIBuilder3.Button(in _text).LocalPressed += RemoveAll;
            uIBuilder3.Spacer(24f);
            _text = "------";
            resultsText = uIBuilder3.Text(in _text);

            // Build right hand side UI - list of found MeshColliders.
            UIBuilder uIBuilder4 = new UIBuilder(rectList[1].Slot);
            uIBuilder4.ScrollArea();
            uIBuilder4.VerticalLayout(10f, 4f);
            _scrollAreaRoot = uIBuilder4.FitContent(SizeFit.Disabled, SizeFit.MinSize).Slot;

            // Prepare UIBuilder for addding elements to MeshCollider list.
            _listBuilder = uIBuilder4;
            _listBuilder.Style.MinHeight = 40f;
        }

        private void Start(Slot Data)
        {
            Data.GetComponentInChildrenOrParents<Canvas>()?.MarkDeveloper();
        }

        private void CreateScrollListElement(MeshCollider mc)
        {
            Slot _elementRoot = _listBuilder.Next("Element");
            var _refField = _elementRoot.AttachComponent<ReferenceField<MeshCollider>>();
            _refField.Reference.Target = mc;
            UIBuilder _listBuilder2 = new UIBuilder(_elementRoot);
            _listBuilder2.NestInto(_elementRoot);
            _listBuilder2.VerticalLayout(4f, 4f);
            _listBuilder2.HorizontalLayout(10f);
            _buttonColor = new color(1f, 1f, 1f);
            _text = "Jump To";
            _listBuilder2.ButtonRef<Slot>(in _text, in _buttonColor, JumpTo, mc.Slot);
            _text = "Highlight";
            _listBuilder2.ButtonRef<Slot>(in _text, in _buttonColor, Highlight, mc.Slot);
            _text = "Replace";
            _listBuilder2.ButtonRef<MeshCollider>(in _text, in _buttonColor, Replace, mc);
            _text = "Remove";
            _listBuilder2.ButtonRef<MeshCollider>(in _text, in _buttonColor, Remove, mc);
            _listBuilder2.NestOut();
            _listBuilder2.NestOut();
            _listBuilder2.Current.AttachComponent<RefEditor>().Setup(_refField.Reference);
        }

        private void ForeachMeshCollider(Action<MeshCollider> process)
        {
            if (ProcessRoot.Reference.Target != null)
            {
                foreach (MeshCollider componentsInChild in ProcessRoot.Reference.Target.GetComponentsInChildren<MeshCollider>(delegate (MeshCollider mc)
                {
                    // Check whether collider should be filtered out.
                    return ((!IgnoreInactive.Value.Value || mc.Slot.IsActive)
                    && (!IgnoreDisabled.Value.Value || mc.Enabled)
                    && (!IgnoreNonPersistent.Value.Value || mc.IsPersistent)
                    && (!IgnoreUserHierarchies.Value.Value || mc.Slot.ActiveUser == null)
                    && ((useTagMode.Value.Value == UseTagMode["IgnoreTag"])
                    || (useTagMode.Value.Value == UseTagMode["IncludeOnlyWithTag"] && mc.Slot.Tag == tag.Reference.Target.TargetString)
                    || (useTagMode.Value.Value == UseTagMode["ExcludeAllWithTag"] && mc.Slot.Tag != tag.Reference.Target.TargetString)));
                }))
                {
                    process(componentsInChild);
                }
            }
            else ShowResults("No target root slot set.");
        }

        private bool CheckReplacementBoundsSetting()
        {
            return ((replacementColliderComponent.Value.Value == ReplacementColliderComponent["BoxCollider"])
                || (replacementColliderComponent.Value.Value == ReplacementColliderComponent["SphereCollider"])
                || (replacementColliderComponent.Value.Value == ReplacementColliderComponent["ConvexHullCollider"])
                || (((replacementColliderComponent.Value.Value == ReplacementColliderComponent["CapsuleCollider"])
                || (replacementColliderComponent.Value.Value == ReplacementColliderComponent["CylinderCollider"]))
                && (setupBoundsType.Value.Value != SetupBoundsType["SetupFromLocalBounds"])));
        }

        private void Highlight(IButton button, ButtonEventData eventData, Slot s)
        {
            HighlightHelper.FlashHighlight(s, null, HighlightColor.Value.Value, HighlightDuration.Value.Value);
        }

        private void JumpTo(IButton button, ButtonEventData eventData, Slot s)
        {
            //LocalUserRoot.JumpToPoint(s.GlobalPosition);
        }

        private void PopulateList()
        {
            _scrollAreaRoot.DestroyChildren();
            ForeachMeshCollider(delegate (MeshCollider mc)
            {
                CreateScrollListElement(mc);
            });
        }

        private void PopulateList(IButton button, ButtonEventData eventData)
        {
            _count = 0;
            _scrollAreaRoot.DestroyChildren();
            ForeachMeshCollider(delegate (MeshCollider mc)
            {
                CreateScrollListElement(mc);
                _count++;
            });
            ShowResults($"{_count} matching MeshColliders listed.");
        }

        private void Remove(IButton button, ButtonEventData eventData, MeshCollider mc)
        {
            mc.UndoableDestroy();
            PopulateList();
            ShowResults($"MeshCollider removed.");
        }

        private void Replace(IButton button, ButtonEventData eventData, MeshCollider mc)
        {
            if (CheckReplacementBoundsSetting())
            {
                // World.BeginUndoBatch("Replace MeshCollider");
                if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["BoxCollider"])
                {
                    var bc = mc.Slot.AttachComponent<BoxCollider>();
                    bc.CreateSpawnUndoPoint();
                    SetupNewCollider(bc, mc);
                }
                else if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["SphereCollider"])
                {
                    var sc = mc.Slot.AttachComponent<SphereCollider>();
                    sc.CreateSpawnUndoPoint();
                    SetupNewCollider(sc, mc);
                }
                else if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["SphereCollider"])
                {
                    var sc = mc.Slot.AttachComponent<SphereCollider>();
                    sc.CreateSpawnUndoPoint();
                    SetupNewCollider(sc, mc);
                }
                else if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["ConvexHullCollider"])
                {
                    mc.Slot.AttachComponent<ConvexHullCollider>().CreateSpawnUndoPoint();
                }

                mc.UndoableDestroy();
                // World.EndUndoBatch();
                PopulateList();
                ShowResults($"MeshCollider replaced.");
            }
            else
            {
                ShowResults($"{replacementColliderComponent.Value.Value} cannot be used with {setupBoundsType.Value.Value}");
            }
        }

        private void RemoveAll(IButton button, ButtonEventData eventData)
        {
            // World.BeginUndoBatch("Batch remove MeshColliders");
            _count = 0;
            ForeachMeshCollider(delegate (MeshCollider mc)
            {
                mc.UndoableDestroy();
                _count++;
            });
            // World.EndUndoBatch();
            PopulateList();
            ShowResults($"{_count} matching MeshColliders removed.");
        }
        private void ReplaceAll(IButton button, ButtonEventData eventData)
        {
            if (CheckReplacementBoundsSetting())
            {
                // World.BeginUndoBatch("Batch replace MeshColliders");
                _count = 0;
                ForeachMeshCollider(delegate (MeshCollider mc)
                {
                    if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["BoxCollider"])
                    {
                        var bc = mc.Slot.AttachComponent<BoxCollider>();
                        bc.CreateSpawnUndoPoint();
                        SetupNewCollider(bc, mc);
                    }
                    else if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["SphereCollider"])
                    {
                        var sc = mc.Slot.AttachComponent<SphereCollider>();
                        sc.CreateSpawnUndoPoint();
                        SetupNewCollider(sc, mc);
                    }
                    else if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["ConvexHullCollider"])
                    {
                        var chc = mc.Slot.AttachComponent<ConvexHullCollider>();
                        chc.CreateSpawnUndoPoint();
                        SetupNewCollider(chc, mc);
                    }
                    else if (replacementColliderComponent.Value.Value == ReplacementColliderComponent["CapsuleCollider"])
                    {
                        var capc = mc.Slot.AttachComponent<CapsuleCollider>();
                        capc.CreateSpawnUndoPoint();
                        SetupNewCollider(capc, mc);
                    }
                    else if(replacementColliderComponent.Value.Value == ReplacementColliderComponent["CylinderCollider"])
                    {
                        var cylc = mc.Slot.AttachComponent<CylinderCollider>();
                        cylc.CreateSpawnUndoPoint();
                        SetupNewCollider(cylc, mc);
                    }
                    mc.UndoableDestroy();
                    _count++;
                });
                // World.EndUndoBatch();
                PopulateList();
                ShowResults($"{_count} matching MeshColliders replaced with {replacementColliderComponent.ToString()}s.");
            }
            else
            {
                ShowResults($"{replacementColliderComponent.Value.Value} cannot be used with {setupBoundsType.Value.Value}");
            }

        }

        private void SetupNewCollider(BoxCollider bc, MeshCollider mc)
        {
            if (setupBoundsType.Value.Value == SetupBoundsType["SetupFromLocalBounds"])
            {
                bc.SetFromLocalBounds();
            }
            else if (setupBoundsType.Value.Value == SetupBoundsType["SetupFromPreciseBound"])
            {
                bc.SetFromLocalBoundsPrecise();
            }

            if (PreserveColliderSettings.Value)
            {
                bc.Type.Value = mc.Type.Value;
                bc.CharacterCollider.Value = mc.CharacterCollider.Value;
                bc.IgnoreRaycasts.Value = mc.IgnoreRaycasts.Value;
                bc.Mass.Value = mc.Mass.Value;
            }
            else
            {
                bc.Type.Value = setColliderType.Value;
                bc.CharacterCollider.Value = SetCharacterCollider.Value;
                bc.IgnoreRaycasts.Value = SetIgnoreRaycasts.Value;
                bc.Mass.Value = Mass.Value;
            }
        }

        private void SetupNewCollider(SphereCollider sc, MeshCollider mc)
        {
            if (setupBoundsType.Value.Value == SetupBoundsType["SetupFromLocalBounds"])
            {
                sc.SetFromLocalBounds();
            }
            else if (setupBoundsType.Value.Value == SetupBoundsType["SetupFromPreciseBound"])
            {
                sc.SetFromPreciseBounds();
            }

            if (PreserveColliderSettings.Value.Value)
            {
                sc.Type.Value = mc.Type.Value;
                sc.CharacterCollider.Value = mc.CharacterCollider.Value;
                sc.IgnoreRaycasts.Value = mc.IgnoreRaycasts.Value;
                sc.Mass.Value = mc.Mass.Value;
            }
            else
            {
                sc.Type.Value = setColliderType.Value;
                sc.CharacterCollider.Value = SetCharacterCollider.Value;
                sc.IgnoreRaycasts.Value = SetIgnoreRaycasts.Value;
                sc.Mass.Value = Mass.Value;
            }
        }

        private void SetupNewCollider(CapsuleCollider capc, MeshCollider mc)
        {
            if (setupBoundsType.Value.Value == SetupBoundsType["SetupFromPreciseBound"])
            {
                capc.SetFromExactCylinder();
            }

            if (PreserveColliderSettings.Value.Value)
            {
                capc.Type.Value = mc.Type.Value;
                capc.CharacterCollider.Value = mc.CharacterCollider.Value;
                capc.IgnoreRaycasts.Value = mc.IgnoreRaycasts.Value;
                capc.Mass.Value = mc.Mass.Value;
            }
            else
            {
                capc.Type.Value = setColliderType.Value;
                capc.CharacterCollider.Value = SetCharacterCollider.Value;
                capc.IgnoreRaycasts.Value = SetIgnoreRaycasts.Value;
                capc.Mass.Value = Mass.Value;
            }
        }

        private void SetupNewCollider(CylinderCollider cylc, MeshCollider mc)
        {
            if (setupBoundsType.Value.Value == SetupBoundsType["SetupFromPreciseBound"])
            {
                cylc.SetFromPreciseBounds();
            }


            if (PreserveColliderSettings.Value.Value)
            {
                cylc.Type.Value = mc.Type.Value;
                cylc.CharacterCollider.Value = mc.CharacterCollider.Value;
                cylc.IgnoreRaycasts.Value = mc.IgnoreRaycasts.Value;
                cylc.Mass.Value = mc.Mass.Value;
            }
            else
            {
                cylc.Type.Value = setColliderType.Value;
                cylc.CharacterCollider.Value = SetCharacterCollider.Value;
                cylc.IgnoreRaycasts.Value = SetIgnoreRaycasts.Value;
                cylc.Mass.Value = Mass.Value;
            }
        }

        private void SetupNewCollider(ConvexHullCollider chc, MeshCollider mc)
        {
            if (PreserveColliderSettings.Value.Value)
            {
                chc.Type.Value = mc.Type.Value;
                chc.CharacterCollider.Value = mc.CharacterCollider.Value;
                chc.IgnoreRaycasts.Value = mc.IgnoreRaycasts.Value;
                chc.Mass.Value = mc.Mass.Value;
            }
            else
            {
                chc.Type.Value = setColliderType.Value;
                chc.CharacterCollider.Value = SetCharacterCollider.Value;
                chc.IgnoreRaycasts.Value = SetIgnoreRaycasts.Value;
                chc.Mass.Value = Mass.Value;
            }
        }

        private void ShowResults(string results)
        {
            resultsText.Content.Value = results;
        }
    }
}
