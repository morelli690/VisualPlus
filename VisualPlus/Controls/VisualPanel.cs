﻿namespace VisualPlus.Controls
{
    #region Namespace

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    using VisualPlus.Framework;
    using VisualPlus.Framework.GDI;
    using VisualPlus.Framework.Handlers;
    using VisualPlus.Framework.Structure;
    using VisualPlus.Localization;
    using VisualPlus.Styles;

    #endregion

    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(Panel))]
    [DefaultEvent("Paint")]
    [DefaultProperty("Enabled")]
    [Description("The Visual Panel")]
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public sealed class VisualPanel : Panel
    {
        #region Variables

        private readonly MouseState mouseState;
        private Color backgroundColor;
        private Border border = new Border();
        private GraphicsPath controlGraphicsPath;
        private Expander expander;
        private StyleManager styleManager = new StyleManager();

        #endregion

        #region Constructors

        public VisualPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint,
                true);

            mouseState = new MouseState(this);

            Size = new Size(187, 117);
            Padding = new Padding(5, 5, 5, 5);
            DoubleBuffered = true;

            UpdateStyles();

            expander = new Expander(this, 22);

            ConfigureStyleManager();
        }

        [Description("Occours when the expander toggle has changed.")]
        public delegate void ToggleChangedEventHandler();

        public event ToggleChangedEventHandler ToggleExpanderChanged;

        #endregion

        #region Properties

        [Category(Localize.Category.Appearance)]
        [Description(Localize.Description.Common.Color)]
        public Color Background
        {
            get
            {
                return backgroundColor;
            }

            set
            {
                backgroundColor = value;
                Invalidate();
            }
        }

        [TypeConverter(typeof(BorderConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Category(Localize.Category.Appearance)]
        public Border Border
        {
            get
            {
                return border;
            }

            set
            {
                border = value;
                Invalidate();
            }
        }

        [TypeConverter(typeof(ExpanderConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Category(Localize.Category.Appearance)]
        public Expander Expander
        {
            get
            {
                return expander;
            }

            set
            {
                expander = value;
                Invalidate();
            }
        }

        [Category(Localize.Category.Appearance)]
        public MouseStates MouseState
        {
            get
            {
                return mouseState.State;
            }

            set
            {
                mouseState.State = value;
                Invalidate();
            }
        }

        [TypeConverter(typeof(StyleManagerConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Category(Localize.Category.Appearance)]
        public StyleManager StyleManager
        {
            get
            {
                return styleManager;
            }

            set
            {
                styleManager = value;
                Invalidate();
            }
        }

        #endregion

        #region Events

        protected override void OnControlAdded(ControlEventArgs e)
        {
            ExceptionManager.SetControlBackColor(e.Control, backgroundColor, false);
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            ExceptionManager.SetControlBackColor(e.Control, backgroundColor, true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (expander.MouseOnButton)
            {
                if (expander.Expanded)
                {
                    expander.Expanded = false;
                }
                else
                {
                    expander.Expanded = true;
                }

                ToggleExpanderChanged?.Invoke();
            }
            else
            {
                Focus();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            mouseState.State = MouseStates.Hover;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseState.State = MouseStates.Normal;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (expander.Visible)
            {
                expander.GetMouseOnButton(e.Location);

                if (expander.MouseOnButton)
                {
                    Cursor = expander.Cursor;
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.Clear(Parent.BackColor);
            graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            if (styleManager.LockedStyle)
            {
                ConfigureStyleManager();
            }

            controlGraphicsPath = GDI.GetBorderShape(ClientRectangle, border.Type, border.Rounding);
            graphics.FillPath(new SolidBrush(backgroundColor), controlGraphicsPath);

            if (border.Visible)
            {
                GDI.DrawBorderType(graphics, mouseState.State, controlGraphicsPath, border.Thickness, border.Color, border.HoverColor, border.HoverVisible);
            }

            if (expander.Visible)
            {
                Point buttonPoint = expander.GetAlignmentPoint(Size);
                expander.Draw(graphics, buttonPoint);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            expander?.UpdateOriginal(Size);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            expander?.UpdateOriginal(Size);
        }

        private void ConfigureStyleManager()
        {
            if (styleManager.VisualStylesManager != null)
            {
                // Load style manager settings 
                IBorder borderStyle = styleManager.VisualStylesManager.BorderStyle;
                IControl controlStyle = styleManager.VisualStylesManager.ControlStyle;
                IFont fontStyle = styleManager.VisualStylesManager.FontStyle;

                border.Color = borderStyle.Color;
                border.HoverColor = borderStyle.HoverColor;
                border.HoverVisible = styleManager.VisualStylesManager.BorderHoverVisible;
                border.Rounding = styleManager.VisualStylesManager.BorderRounding;
                border.Type = styleManager.VisualStylesManager.BorderType;
                border.Thickness = styleManager.VisualStylesManager.BorderThickness;
                border.Visible = styleManager.VisualStylesManager.BorderVisible;
                Font = new Font(fontStyle.FontFamily, fontStyle.FontSize, fontStyle.FontStyle);
                backgroundColor = controlStyle.Background(0);
            }
            else
            {
                // Load default settings
                border = new Border();
                Font = Settings.DefaultValue.DefaultFont;
                backgroundColor = Settings.DefaultValue.Control.Background(0);
            }
        }

        #endregion
    }
}