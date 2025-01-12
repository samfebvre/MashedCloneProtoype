using System;
using System.Collections.Generic;
using DG.DOTweenEditor;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Editor;
using Editor.PrettyWindow;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class PrettyWindow : EditorWindow
{

    #region Serialized

    [FormerlySerializedAs( "m_VisualTreeAsset" )] [SerializeField]
    private VisualTreeAsset m_visualTreeAsset;

    [SerializeField]
    private StyleSheet m_styleSheet;

    #endregion

    #region Public Methods

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Add the style sheet
        root.styleSheets.Add( m_styleSheet );

        VisualElement topLevelElement = new VisualElement();
        topLevelElement.AddToClassList( "topLevelElement-default" );
        root.Add( topLevelElement );
        
        // Instantiate UXML
        VisualElement uxmlElem = m_visualTreeAsset.Instantiate();
        topLevelElement.Add( uxmlElem );
        
        // Create a container for the title and text field
        VisualElement titleAndTextFieldContainer = new VisualElement();
        titleAndTextFieldContainer.AddToClassList( "titleContainer-default" );
        topLevelElement.Add( titleAndTextFieldContainer );

        // Create a title
        Label titleElem = new Label
        {
            text = "Hello there",
        };
        titleElem.AddToClassList( "title-default" );
        titleAndTextFieldContainer.Add( titleElem );

        // Create a text input field
        TextField textField = new TextField
        {
            tooltip = "a text field of some variety",
            //value = m_prettyWindowData.MyString,
            viewDataKey = "mainTextField_Key",
        };

        textField.AddToClassList( "textField-default" );

        // Add the text field to the root
        topLevelElement.Add( textField );

        // Create a container for some buttons
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.AddToClassList( "buttonContainer-default" );
        topLevelElement.Add( buttonContainer );

        // Create some buttons using a loop with simple messages
        for ( int i = 0; i < 5; i++ )
        {
            // Create a wrapper element for the button

            VisualElement wrapper = new VisualElement();
            wrapper.AddToClassList( "buttonWrapper-default" );
            buttonContainer.Add( wrapper );

            // add a box shadow element to the wrapper
            VisualElement boxShadow = new VisualElement();
            boxShadow.AddToClassList( "boxShadow-default" );
            wrapper.Add( boxShadow );

            Button button = CreateAndBindButton( root: wrapper, buttonText: $"button {i}", onClick: () => { } );
            button.AddToClassList( "button-default" );
            button.RegisterCallback<MouseDownEvent>( callback: evt => boxShadow.AddToClassList( "boxShadow-hover" ),
                                                     useTrickleDown: TrickleDown.TrickleDown );
            button.RegisterCallback<MouseUpEvent>( callback: evt => boxShadow.RemoveFromClassList( "boxShadow-hover" ),
                                                   useTrickleDown: TrickleDown.TrickleDown );

            // add a callback to the mouse down that calls a new function   
            wrapper.RegisterCallback<MouseDownEvent>( callback: evt => OnMouseEnter_Shake( topLevelElement ), useTrickleDown: TrickleDown.TrickleDown );
            //wrapper.RegisterCallback<MouseLeaveEvent>( callback: evt => OnMouseLeave_Shake( wrapper ), useTrickleDown: TrickleDown.TrickleDown );

            // if ( i == 4 )
            // {
            //     button.AddToClassList( "button-important" );
            // }
        }

        // create a gradient
        Gradient           gradient  = new Gradient();
        // GradientColorKey[] colorKeys = new GradientColorKey[ 5 ];
        // ColorUtility.TryParseHtmlString( "#ffeb3a", out colorKeys[ 0 ].color );
        // colorKeys[ 0 ].time  = 0.0f;
        // ColorUtility.TryParseHtmlString( "#d8ef47", out colorKeys[ 1 ].color );
        // colorKeys[ 1 ].time = 0.25f;
        // ColorUtility.TryParseHtmlString( "#aff15c", out colorKeys[ 2 ].color );
        // colorKeys[ 2 ].time = 0.5f;
        // ColorUtility.TryParseHtmlString( "#84f174", out colorKeys[ 3 ].color );
        // colorKeys[ 3 ].time = 0.75f;
        // ColorUtility.TryParseHtmlString( "#4def8e", out colorKeys[ 4 ].color );
        // colorKeys[ 4 ].time  = 1.0f;
        
        GradientColorKey[] colorKeys = new GradientColorKey[ 2 ];
        ColorUtility.TryParseHtmlString( "#ffeb3a", out colorKeys[ 0 ].color );
        colorKeys[ 0 ].time  = 0.0f;
        ColorUtility.TryParseHtmlString( "#4def8e", out colorKeys[ 1 ].color );
        colorKeys[ 1 ].time  = 1.0f;
        gradient.colorKeys = colorKeys;

        VisualElement linearGradientElem = new VisualElement();
        linearGradientElem.AddToClassList( "linearGradient-default" );
        AssignGradientAsBackgroundElement( elem: linearGradientElem, gradient: gradient );
        topLevelElement.Add( linearGradientElem );

        Shadow shadow = new Shadow
        {
            shadowCornerRadius    = 6,
            shadowScale           = 1,
            shadowOffsetX         = 3,
            shadowOffsetY         = 3
        };
        shadow.AddToClassList( "softShadow-default" );
        shadow.RegisterCallback<MouseEnterEvent>( callback: evt => OnMouseEnter_Shadow( shadow ), useTrickleDown: TrickleDown.TrickleDown );
        shadow.RegisterCallback<MouseLeaveEvent>( callback: evt => OnMouseLeave_Shadow( shadow ), useTrickleDown: TrickleDown.TrickleDown );
        
        VisualElement someContainer = new VisualElement();
        someContainer.AddToClassList( "someContainer-default" );
        shadow.Add( someContainer );

        topLevelElement.Add( shadow );

        VisualElement maskElem = new VisualElement();
        maskElem.AddToClassList( "maskSVG" );
        topLevelElement.Add( maskElem );

        VisualElement maskedElem = new VisualElement();
        maskedElem.AddToClassList( "maskedElem" );
        
        colorKeys = new GradientColorKey[ 2 ];
        ColorUtility.TryParseHtmlString( "#FF5EEF", out colorKeys[ 0 ].color );
        colorKeys[ 0 ].time = 0.0f;
        ColorUtility.TryParseHtmlString( "#456EFF", out colorKeys[ 1 ].color );
        colorKeys[ 1 ].time = 1.0f;
        gradient.colorKeys  = colorKeys;
        
        AssignGradientAsBackgroundElement( elem: maskedElem, gradient: gradient );
        maskElem.Add( maskedElem );
        
        VisualElement heartIconContainer = new VisualElement();
        heartIconContainer.AddToClassList( "heartIconContainer" );
        VisualElement heartIcon = new VisualElement();
        heartIcon.AddToClassList( "heartIcon" );
        heartIconContainer.Add( heartIcon );
        
        heartIconContainer.RegisterCallback<MouseEnterEvent>( callback: evt => OnMouseEnter_Spin( heartIcon ), useTrickleDown: TrickleDown.TrickleDown );
        
        topLevelElement.Add( heartIconContainer );

    }

    private void OnMouseEnter_Spin(
        VisualElement elem )
    {
        if ( m_visualElementTweens.ContainsKey( elem ) )
        {
            m_visualElementTweens[ elem ].Complete();
            m_visualElementTweens[ elem ].Kill( true );
            m_visualElementTweens.Remove( elem );
        }

        // create a tween that bounces the heart icon
        float rotateVal = 0;
        Tween tween = DOTween.To( getter: () => rotateVal, setter: x =>
        {
            rotateVal = x;
            elem.RotateFromFloat( x );
        }, endValue: 360.0f, duration: 0.4f ).SetEase( Ease.InOutCirc );

        DOTweenEditorPreview.PrepareTweenForPreview( tween );
        m_visualElementTweens.Add( key: elem, value: tween );
    }

    [MenuItem( "Window/UI Toolkit/PrettyWindow" )]
    public static void ShowExample()
    {
        PrettyWindow wnd = GetWindow<PrettyWindow>();
        wnd.titleContent = new GUIContent( "PrettyWindow" );
    }

    #endregion

    #region Unity Functions

    private void OnEnable()
    {
        m_prettyWindowData = PrettyWindowData.instance;
        DOTweenEditorPreview.Start();
    }

    private void OnDisable()
    {
        DOTweenEditorPreview.Stop();
    }

    #endregion

    #region Private Fields

    private PrettyWindowData m_prettyWindowData;

    private Dictionary<VisualElement, Tween> m_visualElementTweens =
        new Dictionary<VisualElement, Tween>();

    #endregion

    #region Private Methods

    private void AssignGradientAsBackgroundElement(
        VisualElement elem,
        Gradient      gradient )
    {
        elem.style.backgroundImage = GradientTextureGenerator.GradientToTexture_WithRotation( 1024, 1024, gradient, 130.55f / (2 * Mathf.PI) );
    }

    private Button CreateAndBindButton(
        VisualElement root,
        string        buttonText,
        Action        onClick )
    {
        Button button = new Button
        {
            text = buttonText,
        };

        button.clicked += onClick;

        root.Add( button );
        return button;
    }

    private void OnMouseEnter_Shake(
        VisualElement elem )
    {
        if ( m_visualElementTweens.ContainsKey( elem ) )
        {
            m_visualElementTweens[ elem ].Complete();
            m_visualElementTweens[ elem ].Kill( true );
            m_visualElementTweens.Remove( elem );
        }

        Vector3 translateVal = elem.TranslateAsV2();
        Tween tween = DOTween.Shake( getter: () => translateVal, setter: x =>
        {
            translateVal = x;
            elem.TranslateFromV2( x );
        }, duration: 0.5f, strength: 3, vibrato: 10, randomness: 45 );

        DOTweenEditorPreview.PrepareTweenForPreview( tween );
        m_visualElementTweens.Add( key: elem, value: tween );
    }
    
    private void OnMouseEnter_Shadow(
        VisualElement elem )
    {
        if ( m_visualElementTweens.ContainsKey( elem ) )
        {
            m_visualElementTweens[ elem ].Complete();
            m_visualElementTweens[ elem ].Kill( true );
            m_visualElementTweens.Remove( elem );
        }
        
        // Create a tween that changes the color of the shadow
        Color shadowColor = Color.clear;
        Tween tween = DOTween.To( getter: () => shadowColor, setter: x =>
        {
            shadowColor = x;
            elem.style.color = new StyleColor
            {
                value = shadowColor,
            };
        }, endValue: Color.black, duration: 0.5f );
        

        DOTweenEditorPreview.PrepareTweenForPreview( tween );
        m_visualElementTweens.Add( key: elem, value: tween );
    }
    
    private void OnMouseLeave_Shadow(
        VisualElement elem )
    {
        if ( m_visualElementTweens.ContainsKey( elem ) )
        {
            m_visualElementTweens[ elem ].Complete();
            m_visualElementTweens[ elem ].Kill( true );
            m_visualElementTweens.Remove( elem );
        }
        
        // Create a tween that changes the color of the shadow back
        Color shadowColor = Color.black;
        Tween tween = DOTween.To( getter: () => shadowColor, setter: x =>
        {
            shadowColor = x;
            elem.style.color = new StyleColor
            {
                value = x,
            };
        }, endValue: Color.clear, duration: 0.5f );

        DOTweenEditorPreview.PrepareTweenForPreview( tween );
        m_visualElementTweens.Add( key: elem, value: tween );
    }

    private void OnMouseLeave_Shake(
        VisualElement button )
    {
        // if ( m_visualElementTweens.ContainsKey( button ) )
        // {
        //     m_visualElementTweens[ button ].Complete();
        //     m_visualElementTweens[ button ].Kill( true );
        //     m_visualElementTweens.Remove( button );
        // }
    }

    #endregion

}