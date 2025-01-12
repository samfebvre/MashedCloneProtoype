// =====================================================================
// Copyright 2013-2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using FluffyUnderware.DevToolsEditor.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        private GUI.WindowFunction moduleDrawingCallbackLod0Delegate;
        private GUI.WindowFunction moduleDrawingCallbackLod1Delegate;
        private GUI.WindowFunction moduleDrawingCallbackCulledDelegate;

        private bool DrawWindow(
            bool isRepaint,
            out bool reorderModules)
        {
            DrawToolbar(out reorderModules);
            Viewport.DrawBackground();
            DrawDebugInformation(Viewport.ClientRectangle);
            if (CurvyProject.Instance.CGShowHelp)
                DrawHelpMessage(Viewport.ClientRectangle);
            bool needsRepaint = DrawStatusBar(StatusBarHeight);

            Viewport.BeginScalableArea(ToolbarHeight);
            {
                needsRepaint |= DrawModules(isRepaint);
                if (IsLinkDrag)
                    DrawLinkDrag();
                selectionRectangle.Draw(Viewport.CanvasMousePosition);
            }
            Viewport.EndScalableArea();

            return needsRepaint;
        }

        #region Non scalable elements

        /// <summary>
        /// The bar at the bottom of the generator window
        /// </summary>
        /// <param name="height"></param>
        /// <returns> True if a repaint is needed</returns>
        private bool DrawStatusBar(
            float height)
        {
            Rect r = new Rect(
                -1,
                position.height - height,
                201,
                height - 1
            );
            // Performance
            EditorGUI.HelpBox(
                r,
                string.Format(
                    "Exec. Time (Avg): {0:0.###} ms",
                    Generator.DEBUG_ExecutionTime.AverageMS
                ),
                MessageType.None
            );
            // Message
            return statusBar.Render(
                new Rect(
                    200,
                    position.height - height,
                    position.width,
                    height - 1
                )
            );
        }


        /// <summary>
        /// The message that appears when the user clicks the help button, at the top right of the window
        /// </summary>
        /// <param name="canvasArea"> The area in the editor window dedicated to show the canvas</param>
        private static void DrawHelpMessage(
            Rect canvasArea)
        {
            Rect drawingArea = GetHelpMessageArea(canvasArea);

            GUILayout.BeginArea(
                drawingArea,
                GUI.skin.box
            );
            {
                GUI.Label(
                    new Rect(
                        10,
                        5,
                        drawingArea.width,
                        drawingArea.height
                    ),
                    @"<b>General:</b>
      <b>Right Mouse Button:</b>
          Opens the contextual menu.
      <b>Right Mouse Button on Link:</b>
          Deletes the link.
      <b>Module Drag + Alt (Hold):</b>
          Snaps module(s) to grid.
      <b>Scroll + Alt (Hold):</b>
          Increases scroll speed.
      <b>Middle Button Mouse Drag
      or Mouse Drag + Space or Alt (Hold):</b>
          Pans canvas.
      <b>F key:</b>
          Focuses view on selected module(s) 
          or entire graph if none selected.

<b>To Add Modules:</b>
      <b>Contextual Menu:</b>
          Shows a list of all modules.
      <b>Link Drag & Drop:</b>
          Shows a list of modules compatible
          with the link.
      <b>Object Drag & Drop:</b>
          Creates input modules by dropping
          objects (mesh, spline, ...).",
                    DTStyles.HtmlLabelAlignTop
                );
            }
            GUILayout.EndArea();
        }

        private static Rect GetHelpMessageArea(
            Rect canvasArea)
        {
            Rect drawingArea = new Rect();
            drawingArea.x = canvasArea.xMax - 275;
            drawingArea.y = canvasArea.yMin + 5;
            drawingArea.width = 270;
            drawingArea.height = 380;

            // Ensure area does not spill out of canvas
            drawingArea.width = Mathf.Min(
                drawingArea.width,
                canvasArea.xMax - drawingArea.x
            );
            drawingArea.height = Mathf.Min(
                drawingArea.height,
                canvasArea.yMax - drawingArea.y
            );
            return drawingArea;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canvasArea"> The area in the editor window dedicated to show the canvas</param>
        [System.Diagnostics.Conditional(CompilationSymbols.CurvyDebug)]
        private void DrawDebugInformation(
            Rect canvasArea)
        {
            GUILayout.BeginArea(canvasArea);
            {
                GUILayout.Label("Viewport ClientRectangle: " + Viewport.ClientRectangle);
                GUILayout.Label(
                    "Canvas Scroll: "
                    + $" {Viewport.ScrollValue}/{Viewport.ScrollTarget} Speed {Viewport.ScrollSpeed}"
                );
                GUILayout.Label("Canvas VisibleCanvasArea: " + Viewport.VisibleCanvasArea);

                GUILayout.Label("Mouse event position: " + EV.mousePosition);
                GUILayout.Label("Mouse window position: " + MousePosition);
                GUILayout.Label("Mouse viewport position: " + Viewport.MousePosition);
                GUILayout.Label("Mouse canvas position: " + Viewport.CanvasMousePosition);

                //GUILayout.Label("IsWindowDrag: " + Canvas.IsWindowDrag);
                GUILayout.Label("IsSelectionDrag: " + selectionRectangle.IsDragging);
                GUILayout.Label("IsLinkDrag: " + IsLinkDrag);
                GUILayout.Label("IsCanvasDrag: " + canvasState.IsDragging);
                GUILayout.Label("IsModuleDrag: " + (GetDraggedModule() != null));
                GUILayout.Label("HoveredModule: " + GetHoveredModule());
                GUILayout.Label("MouseOverCanvas: " + Viewport.IsMouseHover);
                GUILayout.Label("SelectedLink: " + CanvasSelection.SelectedLink);
                GUILayout.Label("Selected Module: " + CanvasSelection.SelectedModule);
            }
            GUILayout.EndArea();
            Repaint();
        }

        #endregion

        #region Module

        private bool DrawModules(
            bool isRepaint)
        {
            bool needsRepaint = false;
            BeginWindows();

            for (int i = 0; i < Modules.Count; i++)
            {
                CGModule module = Modules[i];
                if (module == null)
                    continue;

                needsRepaint |= DrawModule(
                    module,
                    CanvasSelection.SelectedModule,
                    i
                );
            }

            if (isRepaint)
                //in repaint event, the module dimensions are valid.
                //we draw links before call to EndWindows, so that modules are drawn on top of links
                for (int i = 0; i < Modules.Count; i++)
                {
                    CGModule module = Modules[i];
                    if (module == null)
                        continue;

                    DrawModuleOutputLinks(module);
                }

            EndWindows();

            return needsRepaint;
        }

        private bool DrawModule(
            [NotNull] CGModule module,
            [CanBeNull] CGModule selectedModule,
            int moduleIndex)
        {
            CGModuleProperties moduleProperties = module.Properties;

            bool shouldDrawModule = ShouldDrawModule(
                module,
                selectedModule,
                ModuleRefreshHighlightSize
            );

            GUI.WindowFunction callback;
            Rect area;
            string windowTitle;
            GUIStyle style;
            if (shouldDrawModule)
            {
                bool isSimplifiedModule = Viewport.IsInOverviewMode;

                if (isSimplifiedModule)
                    callback = moduleDrawingCallbackLod1Delegate;
                else
                    callback = moduleDrawingCallbackLod0Delegate;

                area = isSimplifiedModule
                    ? moduleProperties.Dimensions
                    //we set height to 0 and rely on the draw callback to update the height by drawing
                    : new Rect(
                        moduleProperties.Dimensions.position,
                        new Vector2(
                            moduleProperties.MinWidth,
                            0
                        )
                    );
                windowTitle =
                    isSimplifiedModule
                        ? String.Empty
                        : module.GetTitle();
                style = CurvyStyles.ModuleWindow;
            }
            else
            {
                callback = moduleDrawingCallbackCulledDelegate;
                area = moduleProperties.Dimensions;
                windowTitle = String.Empty;
                style = GUI.skin.window;
            }

            Rect drawnWindowRect = GUILayout.Window(
                moduleIndex,
                area,
                callback,
                windowTitle,
                style
            );


            //drawnWindowRect.position is ignored, because we want to ignore module dragging, which is handled by ourselves elsewhere

            //todo extract dimensions update?
            if (EV.type == EventType.Repaint)
                //we update only in repaint, in Layout (and others?) dimensions are not set properly at the end of the GUILayout.Window call
                moduleProperties.Dimensions.size = drawnWindowRect.size;
            if (module.Properties.Dimensions.size != Vector2.zero)
                module.UpdateSlotDimensions();

            bool needsRepaint = false;

            if (shouldDrawModule)
                needsRepaint = DrawModuleRefreshHighlight(module);

            return needsRepaint;
        }

        private bool ShouldDrawModule(
            [NotNull] CGModule module,
            [CanBeNull] CGModule selectedModule,
            int refreshHighlightSize)
        {
            //When modules are culled, they are not rendered (duh) and thus their height is not updated. This is ok as long as the height is constant. When there is some module expanding/collapsing, the height should change. In those cases, we disable the culling (for all modules for implementation simplicity sake, could be optimized) so the height is updated, so that the modules reordering code can work based on the actual height, and not the pre-culling one, which was leading to bad reordering results.
            bool animationIsHappening = mShowDebug.isAnimating || Modules.Exists(m => m.Properties.Expanded.isAnimating);
            if (animationIsHappening)
                return true;

            //module was never drawn, so we need to draw it to get its dimensions, needed for clipping logic
            if (module.Properties.Dimensions.size == Vector2.zero)
                return true;

            //This is based on the condition at which mod.Properties.Expanded.target is modified in ModuleDrawingCallback_LOD0
            if (module.Properties.Expanded.target
                != module.GetExpectedExpansionState(
                    selectedModule
                ))
                return true;

            return !Viewport.IsModuleClipped(
                module,
                refreshHighlightSize
            );
        }

        #region Modules window drawing callbacks

        private void ModuleDrawingCallback_LOD0(
            int id)
        {
            // something happened in the meantime?
            if (id >= Modules.Count || mModuleCount != Modules.Count)
                return;

            CGModule module = Modules[id];

            DrawModuleTopArea(
                id,
                module
            );
            DrawSlotsArea_LOD0(module);

            CGModuleEditorBase moduleEditor = GetModuleEditor(module);
            if (moduleEditor && moduleEditor.target != null)
                DrawModuleFoldableArea(
                    moduleEditor,
                    module
                );

            //draw selection highlight
            if (CanvasSelection.SelectedModules.Contains(module))
            {
                Rect lastDrawnArea = GUILayoutUtility.GetLastRect();
                Rect highlightRectangle = new Rect(
                    Vector2.zero,
                    lastDrawnArea.size
                    + lastDrawnArea.position
                    + new Vector2(
                        4,
                        4
                    )
                );

                DrawSelectionHighlight(
                    highlightRectangle
                );
            }

            FinalizeModuleDrawingCallback(
                module,
                moduleEditor
            );
        }

        private void ModuleDrawingCallback_LOD1(
            int id)
        {
            // something happened in the meantime?
            if (id >= Modules.Count || mModuleCount != Modules.Count)
                return;

            CGModule module = Modules[id];
            Vector2 moduleSize = module.Properties.Dimensions.size;
            bool isSmallArea = moduleSize.x * moduleSize.y < 30000;

            DrawSlotsArea_LOD1(module);

            EditorGUILayout.BeginVertical();

            Rect moduleLocalRectangle = new Rect(
                Vector2.zero,
                moduleSize
            );

            GUI.Label(
                moduleLocalRectangle,
                module.GetTitle(
                    isSmallArea
                        ? 15
                        : 24
                ),
                CurvyStyles.GetModuleLOD1LabelStyle(isSmallArea)
            );

            EditorGUILayout.EndVertical();

            //draw selection highlight
            if (CanvasSelection.SelectedModules.Contains(module))
                DrawSelectionHighlight(
                    moduleLocalRectangle
                );

            FinalizeModuleDrawingCallback(
                module,
                GetModuleEditor(module)
            );
        }

        private void ModuleDrawingCallback_Culled(
            int id)
        {
            // something happened in the meantime?
            if (id >= Modules.Count || mModuleCount != Modules.Count)
                return;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.EndVertical();
        }

        private void FinalizeModuleDrawingCallback(
            [NotNull] CGModule module,
            [NotNull] CGModuleEditorBase moduleEditor)
        {
            // Check errors
            if (Generator.HasCircularReference(module))
                EditorGUILayout.HelpBox(
                    "Circular Reference",
                    MessageType.Error
                );

            UpdateDragState(moduleEditor);
        }

        private void DrawSelectionHighlight(
            Rect moduleRectangle)
        {
            DTGUI.PushBackgroundColor(SelectedModuleColor); //.SkinAwareColor());

            const int lineThickness = 3;
            int zoomAdjustedThickness = Mathf.RoundToInt(lineThickness / Viewport.Zoom);
            float x = moduleRectangle.x;
            float y = moduleRectangle.y;
            float width = moduleRectangle.width;
            float height = moduleRectangle.height;

            //This is the distance of intersection between lines. We take it into consideration to avoid lines overlapping. Ideally, this value should be equal to lineThickness, but because of probably not ideal zoom values, occasionally something is not rounded the right way somewhere in the rendering process, leading to an empty one pixel area. To avoid this, I reduced the size of this value by one. Meaning that there will be, depending on the roundings by Unity's rendering code (mine too?), either an overlap of 0 or 1 pixel.
            int overlapAvoidance = Mathf.RoundToInt((lineThickness - 1) / Viewport.Zoom);

            // top line
            GUI.Box(
                new Rect(
                    x + overlapAvoidance,
                    y,
                    width - (2 * overlapAvoidance),
                    zoomAdjustedThickness
                ),
                GUIContent.none,
                CurvyStyles.ModuleHighlight
            );

            // left
            GUI.Box(
                new Rect(
                    x,
                    y,
                    zoomAdjustedThickness,
                    height
                ),
                GUIContent.none,
                CurvyStyles.ModuleHighlight
            );

            // right
            GUI.Box(
                new Rect(
                    (x + width) - zoomAdjustedThickness,
                    y,
                    zoomAdjustedThickness,
                    height
                ),
                GUIContent.none,
                CurvyStyles.ModuleHighlight
            );

            // bottom
            GUI.Box(
                new Rect(
                    x + overlapAvoidance,
                    (y + height) - zoomAdjustedThickness,
                    width - (2 * overlapAvoidance),
                    zoomAdjustedThickness
                ),
                GUIContent.none,
                CurvyStyles.ModuleHighlight
            );

            DTGUI.PopBackgroundColor();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <returns>Needs a window repaint?</returns>
        private bool DrawModuleRefreshHighlight(
            [NotNull] CGModule module)
        {
            double timeSinceLastUpdate = (DateTime.Now - module.DEBUG_LastUpdateTime).TotalMilliseconds;
            if (timeSinceLastUpdate < ModuleRefreshHighlightDuration)
            {
                module.DrawRefreshHighlight(
                    ModuleRefreshHighlightDuration,
                    ModuleRefreshHighlightSize,
                    timeSinceLastUpdate
                );
                //TODO optim: repaint only every other frame or so
                return true;
            }

            return false;
        }

        private void UpdateDragState(
            [NotNull] CGModuleEditorBase moduleEditor)
        {
            bool wasDragEvent = EV.type == EventType.MouseDrag;
            bool wasDragEndingEvent =
                EV.type == EventType.MouseMove
                || EV.type == EventType.MouseDown
                || EV.type == EventType.MouseUp
                //Happens when mouse up outside of the module window boundaries
                || EV.type == EventType.Ignore;

            GUI.DragWindow();

            if (moduleEditor.NeedsDrag)
            {
                //stop dragging
                if (wasDragEndingEvent)
                    moduleEditor.NeedsDrag = false;
            }
            else
            {
                //start dragging
                if (wasDragEvent
                    && EV.type == EventType.Used)
                    moduleEditor.NeedsDrag = true;
            }
        }

        private void DrawModuleTopArea(
            int id,
            CGModule module)
        {
            Rect moduleDimensions = module.Properties.Dimensions;

            // Enabled
            EditorGUI.BeginChangeCheck();
            module.Active = GUI.Toggle(
                new Rect(
                    2,
                    2,
                    16,
                    16
                ),
                module.Active,
                ""
            );
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(Generator);

            //Edit Title & Color
            if (titleEditingModule == module)
            {
                GUI.SetNextControlName("editTitle" + id);
                module.ModuleName = GUI.TextField(
                    new Rect(
                        30,
                        5,
                        moduleDimensions.width - 120,
                        16
                    ),
                    module.ModuleName
                );
                module.Properties.BackgroundColor = EditorGUI.ColorField(
                    new Rect(
                        moduleDimensions.width - 70,
                        5,
                        32,
                        16
                    ),
                    module.Properties.BackgroundColor
                );
            }

            if (GUI.Button(
                    new Rect(
                        moduleDimensions.width - 32,
                        6,
                        16,
                        16
                    ),
                    new GUIContent(
                        CurvyStyles.EditTexture,
                        "Rename"
                    ),
                    CurvyStyles.BorderlessButton
                ))
            {
                StartTitleEditing(module);
                EditorGUI.FocusTextInControl("editTitle" + id);
            }

            // Help
            if (GUI.Button(
                    new Rect(
                        moduleDimensions.width - 16,
                        6,
                        16,
                        16
                    ),
                    new GUIContent(
                        CurvyStyles.HelpTexture,
                        "Help"
                    ),
                    CurvyStyles.BorderlessButton
                ))
            {
                string url = DTUtility.GetHelpUrl(module);
                if (!string.IsNullOrEmpty(url))
                    Application.OpenURL(url);
            }
        }

        private void DrawModuleFoldableArea(
            [NotNull] CGModuleEditorBase moduleEditor,
            [NotNull] CGModule module)
        {
            bool isDebugVisible = EditorGUILayout.BeginFadeGroup(mShowDebug.faded);
            {
                if (isDebugVisible)
                    moduleEditor.OnInspectorDebugGUIINTERNAL(Repaint);
            }
            EditorGUILayout.EndFadeGroup();

            // Draw Module Options

            //I don't see the need for this, but I am not familiar enough with CG editor's code to feel confident to remove it
            module.Properties.Expanded.valueChanged.RemoveListener(Repaint);
            module.Properties.Expanded.valueChanged.AddListener(Repaint);

            if (!CurvyProject.Instance.CGAutoModuleDetails)
                module.Properties.Expanded.target = GUILayout.Toggle(
                    module.Properties.Expanded.target,
                    new GUIContent(
                        module.Properties.Expanded.target
                            ? CurvyStyles.CollapseTexture
                            : CurvyStyles.ExpandTexture,
                        "Show Details"
                    ),
                    CurvyStyles.ShowDetailsButton
                );

            // === Module Details ===
            // Handle Auto-Folding
            module.Properties.Expanded.target = module.GetExpectedExpansionState(
                CanvasSelection.SelectedModule
            );

            bool isExpansionVisible = EditorGUILayout.BeginFadeGroup(module.Properties.Expanded.faded);
            {
                if (isExpansionVisible)
                {
                    EditorGUIUtility.labelWidth = module.Properties.LabelWidth;
                    // Draw Inspectors using Modules Background color
                    DTGUI.PushColor(moduleEditor.Target.Properties.BackgroundColor.SkinAwareColor(true));

                    EditorGUILayout.BeginVertical(CurvyStyles.ModuleWindowBackground);
                    {
                        DTGUI.PopColor();
                        moduleEditor.RenderGUI(true);
                        GUILayout.Space(2);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        #region Slots

        public const int ConnectorWidth = 25;

        private void DrawSlotsArea_LOD0(
            [NotNull] CGModule module)
        {
            DTGUI.PushColor(module.Properties.BackgroundColor.SkinAwareColor(true));
            GUILayout.Space(1);
            EditorGUILayout.BeginVertical(CurvyStyles.ModuleWindowSlotBackground);
            {
                DTGUI.PopColor();

                int slotIndex = 0;
                while (module.Input.Count > slotIndex || module.Output.Count > slotIndex)
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (module.Input.Count > slotIndex)
                        {
                            CGModuleInputSlot inputSlot = module.Input[slotIndex];
                            bool isDisabled = ShouldInputSlotBeDisabled(inputSlot);

                            DrawConnector(
                                GetConnectorColor(
                                    inputSlot,
                                    isDisabled
                                ),
                                false
                            );
                            DrawSlotLabel(
                                inputSlot,
                                isDisabled
                            );
                        }

                        if (module.Output.Count > slotIndex)
                        {
                            CGModuleOutputSlot outputSlot = module.Output[slotIndex];
                            bool isDisabled = ShouldOutputLinkBeDisabled(outputSlot);

                            DrawSlotLabel(
                                outputSlot,
                                isDisabled
                            );

                            DrawConnector(
                                GetConnectorColor(
                                    outputSlot,
                                    isDisabled
                                ),
                                false
                            );
                        }
                    }
                    GUILayout.EndHorizontal();
                    slotIndex++;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool ShouldOutputLinkBeDisabled(
            CGModuleOutputSlot outputSlot) =>
            IsOutputLinkDrag
            || (IsInputLinkDrag && !outputSlot.CanLinkTo(InputLinkDragFrom));

        private bool ShouldInputSlotBeDisabled(
            CGModuleInputSlot inputSlot) =>
            IsInputLinkDrag
            || (IsOutputLinkDrag && !inputSlot.CanLinkTo(OutputLinkDragFrom));

        private void DrawSlotsArea_LOD1(
            [NotNull] CGModule module)
        {
            EditorGUILayout.BeginVertical();
            {
                int slotIndex = 0;
                while (module.Input.Count > slotIndex || module.Output.Count > slotIndex)
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (module.Input.Count > slotIndex)
                        {
                            CGModuleInputSlot inputSlot = module.Input[slotIndex];
                            bool isDisabled = ShouldInputSlotBeDisabled(inputSlot);

                            DrawConnector(
                                GetConnectorColor(
                                    inputSlot,
                                    isDisabled
                                ),
                                true
                            );
                        }

                        if (module.Output.Count > slotIndex)
                        {
                            //fill space usually occupied by the label
                            GUILayout.FlexibleSpace();

                            CGModuleOutputSlot outputSlot = module.Output[slotIndex];
                            bool isDisabled = ShouldOutputLinkBeDisabled(outputSlot);

                            DrawConnector(
                                GetConnectorColor(
                                    outputSlot,
                                    isDisabled
                                ),
                                true
                            );
                        }
                    }
                    GUILayout.EndHorizontal();
                    slotIndex++;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private Color GetConnectorColor(
            [NotNull] CGModuleSlot slot,
            bool isDisabled)
        {
            Color slotColor;
            {
                slotColor = isDisabled
                    ? new Color(
                        0.5f,
                        0.5f,
                        0.5f
                    )
                    : DataTypeColors.Instance.GetTypeColor(slot.Info.DataType);
            }
            return slotColor;
        }

        private static void DrawSlotLabel(
            [NotNull] CGModuleSlot slot,
            bool isIncompatible)
        {
            GUILayout.Label(
                new GUIContent(
                    slot.GetLabelText(),
                    slot.Info.Tooltip
                ),
                CurvyStyles.GetSlotLabelStyle(
                    slot,
                    isIncompatible
                )
            );
        }

        private static void DrawConnector(
            Color color,
            bool isLod1)
        {
            DTGUI.PushColor(color);
            GUILayout.Box(
                isLod1
                    ? ""
                    : "<",
                CurvyStyles.Slot
            );
            DTGUI.PopColor();
        }

        #endregion

        #endregion
    }
}