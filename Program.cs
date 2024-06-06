using System.Numerics;
using Raylib_cs;

namespace HelloWorld;

class Program
{
    public static void Main()
    {
        Raylib.InitWindow(1280, 720, "Hello World");
        Raylib.SetTargetFPS(60);

        var windowManager = new WindowManagementWidget(new Vector2(0, 0));
        windowManager.Init();

        while (!Raylib.WindowShouldClose())
        {
            windowManager.Update(windowManager);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            foreach (WindowList name in Enum.GetValues(typeof(WindowList)))
            {
                WindowWidget window = windowManager.GetWindow(name);
                if (window.IsVisible())
                {
                    window.HandleInput(windowManager);
                }
            }

            windowManager.Draw(new Vector2(0, 0));

            Raylib.EndDrawing();
        }

        windowManager.Dispose();
        Raylib.CloseWindow();
    }
}

public class WindowTeste1 : WindowWidget
{
    public WindowTeste1(Vector2 position, int width, int height, Color color, WindowList windowType, string title = "", string closeButton = "X") : base(position, width, height, color, windowType, title, closeButton)
    {
        var button = new ButtonWidget(this, new Vector2(0, 200), 200, 40, "Teste Button", Color.Black, Color.White, Color.Gray, Color.Red, 20);

        AddWidget("button", button);
    }

}

public class WindowTeste2 : WindowWidget
{
    public WindowTeste2(Vector2 position, int width, int height, Color color, WindowList windowType, string title = "", string closeButton = "X") : base(position, width, height, color, windowType, title, closeButton)
    {
        var label = new LabelWidget(200, new Vector2(0, 0), "Teste Label", Color.Black, 20, LabelWidget.LabelAlignment.Center);
        var button = new ButtonWidget(this, new Vector2(0, 200), 200, 40, "Teste Button", Color.Black, Color.White, Color.Gray, Color.Red, 20);

        button.Clicked += (button) =>
        {
            Console.WriteLine("Button clicked");
        };

        AddWidget("label", label);
        AddWidget("button", button);
    }


}

public abstract class WidgetBase
{
    protected Vector2 position;

    public WidgetBase(Vector2 position)
    {
        this.position = position;
    }

    public abstract void Init();
    public abstract void Draw(Vector2 parentPosition);
    public abstract void Update(WindowManagementWidget windowManagementWidget);
    public abstract void Dispose();
}

public enum WindowList
{
    WindowTeste1,
    WindowTeste2,

}

public class WindowManagementWidget(Vector2 position) : WidgetBase(position)
{

    private readonly Dictionary<WindowList, WindowWidget> windows = new Dictionary<WindowList, WindowWidget>();
    private List<WindowWidget> windowOrder = new List<WindowWidget>();

    public override void Init()
    {
        // var windowTeste = new WindowTeste1(new Vector2(100, 100), 300, 400, Color.SkyBlue);
        var windowTeste = new WindowTeste1(new Vector2(100, 100), 300, 400, Color.SkyBlue, WindowList.WindowTeste1);
        AddWindow(WindowList.WindowTeste1, windowTeste);

        var windowTeste2 = new WindowTeste2(new Vector2(100, 100), 300, 400, Color.SkyBlue, WindowList.WindowTeste2);
        AddWindow(WindowList.WindowTeste2, windowTeste2);

        ShowWindow(WindowList.WindowTeste1);
        ShowWindow(WindowList.WindowTeste2);

        windowOrder.Add(windows[WindowList.WindowTeste1]);
        windowOrder.Add(windows[WindowList.WindowTeste2]);
    }

    public override void Draw(Vector2 parentPosition)
    {
        foreach (WindowWidget window in windowOrder)
        {
            window.Draw(parentPosition + position);
        }
    }

    public override void Update(WindowManagementWidget windowManagementWidget)
    {

        for (int i = windowOrder.Count - 1; i >= 0; i--)
        {
            WindowWidget window = windowOrder[i];

            if (window.IsUnderMouse())
            {
                if (window.IsDragging())
                {
                    Vector2 mousePosition = Raylib.GetMousePosition();
                    Vector2 newPosition = mousePosition - window.GetDraggingOffset();
                    window.SetPosition(newPosition);
                    windowManagementWidget.BringToFront(window);
                }
                else
                {
                    window.Update(windowManagementWidget);
                }

                break;
            }
        }
    }



    public override void Dispose() { }

    public Dictionary<WindowList, WindowWidget> GetWindows()
    {
        return windows;
    }

    public WindowWidget? GetActiveWindow()
    {
        if (windowOrder.Count > 0)
        {
            var activeWindow = windowOrder[windowOrder.Count - 1];
            return windows[activeWindow.WindowType];
        }

        return null;
    }

    public void ShowWindow(WindowList name)
    {
        GetWindow(name).Show();
    }

    public void HideWindow(WindowList name)
    {
        GetWindow(name).Hide();
    }

    public void BringToFront(WindowWidget window)
    {
        windowOrder.Remove(window);
        windowOrder.Add(window);
    }

    public WindowWidget? GetTopWindowUnderMouse()
    {
        Vector2 mousePosition = Raylib.GetMousePosition();

        for (int i = windowOrder.Count - 1; i >= 0; i--)
        {
            WindowWidget window = windowOrder[i];
            if (Raylib.CheckCollisionPointRec(mousePosition, window.GetBounds()))
            {
                return window;
            }
        }

        return null;
    }

    public void AddWindow(WindowList name, WindowWidget window)
    {
        windows[name] = window;
        windowOrder.Add(window);
    }

    public void RemoveWindow(WindowList name)
    {
        WindowWidget window = windows[name];
        windows.Remove(name);
        windowOrder.Remove(window);
    }

    public WindowWidget GetWindow(WindowList name)
    {
        return windows[name];
    }

    public WindowWidget? TryGetWindow(WindowList name)
    {
        windows.TryGetValue(name, out WindowWidget? value);
        return value;
    }
}

public class WindowWidget : WidgetBase
{
    private bool isVisible;
    private bool isDragging;
    private Vector2 draggingOffset;
    private Dictionary<string, WidgetBase> widgets = new Dictionary<string, WidgetBase>();
    private Rectangle bounds;
    private Rectangle titleBar;
    public WindowList WindowType { get; }
    public bool IsFocused { get; private set; }
    public Vector2 Position { get; private set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Color Color { get; set; }

    public WindowWidget(Vector2 position, int width, int height, Color color, WindowList windowType, string title = "", string closeButton = "") : base(position)
    {
        bounds = new Rectangle(position.X, position.Y, width, height);
        titleBar = new Rectangle(position.X, position.Y, width, 40);

        WindowType = windowType;

        if (width <= 0)
        {
            throw new ArgumentException("A largura deve ser maior que zero.", nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentException("A altura deve ser maior que zero.", nameof(height));
        }

        Width = width;
        Height = height;
        Color = color;

        if (!string.IsNullOrEmpty(closeButton))
        {
            ButtonWidget closeButtonWidget = new ButtonWidget(this, new Vector2(width - 40, 0), 40, 40, closeButton, Color.Black, Color.White, Color.Gray, Color.Red, 20);

            closeButtonWidget.Clicked += (btn) =>
            {
                Hide();
            };

            widgets["closeButton"] = closeButtonWidget;
        }

        IsFocused = false;
    }

    public override void Init()
    {
        foreach (var widget in widgets.Values)
        {
            widget.Init();
        }
    }

    public override void Draw(Vector2 parentPosition)
    {
        if (!isVisible)
        {
            return;
        }

        Raylib.DrawRectangleRec(bounds, Color);
        // Raylib.DrawRectangleRec(titleBar, Color.DarkGray);

        foreach (var widget in widgets.Values)
        {
            widget.Draw(position + parentPosition);
        }
    }

    public void SetFocus(bool focused)
    {
        IsFocused = focused;
    }

    public Vector2 GetPosition()
    {
        return position;
    }

    public void SetPosition(Vector2 newPosition)
    {
        position = newPosition;
        bounds.X = newPosition.X;
        bounds.Y = newPosition.Y;
        titleBar.X = bounds.X;
        titleBar.Y = bounds.Y;
    }

    public void HandleInput(WindowManagementWidget windowManagementWidget)
    {
        if (!isVisible)
        {
            return;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mousePosition = Raylib.GetMousePosition();
            WindowWidget? topWindow = windowManagementWidget.GetTopWindowUnderMouse();

            if (topWindow == this && Raylib.CheckCollisionPointRec(mousePosition, bounds))
            {
                windowManagementWidget.BringToFront(this);

                if (Raylib.CheckCollisionPointRec(mousePosition, titleBar))
                {
                    isDragging = true;
                    draggingOffset = mousePosition - new Vector2(bounds.X, bounds.Y);
                }
            }
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector2 mousePosition = Raylib.GetMousePosition();
            Vector2 newPosition = mousePosition - draggingOffset;
            SetPosition(newPosition);
        }
    }

    public override void Update(WindowManagementWidget windowManagementWidget)
    {
        foreach (var widget in widgets.Values)
        {
            widget.Update(windowManagementWidget);
        }
    }

    public override void Dispose()
    {
        foreach (var widget in widgets.Values)
        {
            widget.Dispose();
        }

        widgets.Clear();
    }

    public bool IsUnderMouse()
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        return Raylib.CheckCollisionPointRec(mousePosition, GetBounds());
    }

    public Rectangle GetBounds()
    {
        return bounds;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public Vector2 GetDraggingOffset()
    {
        return draggingOffset;
    }

    public void Show()
    {
        isVisible = true;
    }

    public void Hide()
    {
        isVisible = false;
    }

    public bool IsVisible()
    {
        return isVisible;
    }

    public void AddWidget(string name, WidgetBase widget)
    {
        if (widgets.ContainsKey(name))
        {
            throw new ArgumentException($"A widget with the name '{name}' already exists.", nameof(name));
        }

        widgets[name] = widget;
    }

    public void RemoveWidget(string name)
    {
        if (!widgets.ContainsKey(name))
        {
            throw new ArgumentException($"No widget with the name '{name}' exists.", nameof(name));
        }

        widgets.Remove(name);
    }

    public WidgetBase GetWidget(string name)
    {
        if (!widgets.ContainsKey(name))
        {
            throw new ArgumentException($"No widget with the name '{name}' exists.", nameof(name));
        }

        return widgets[name];
    }

    public List<WidgetBase> GetWidgets()
    {
        return new List<WidgetBase>(widgets.Values);
    }
}

public class LabelWidget : WidgetBase
{
    public int Width { get; private set; }
    public string Text { get; private set; }
    public Color TextColor { get; private set; }
    public int FontSize { get; private set; }
    public LabelAlignment Alignment { get; private set; }

    public enum LabelAlignment
    {
        Left,
        Center,
        Right
    }

    public LabelWidget(int width, Vector2 position, string text, Color textColor, int fontSize, LabelAlignment alignment) : base(position)
    {
        if (width < 0)
        {
            throw new ArgumentException("Width must not be negative", nameof(width));
        }

        if (fontSize <= 0)
        {
            throw new ArgumentException("Font size must be positive", nameof(fontSize));
        }

        Width = width;
        Text = text;
        TextColor = textColor;
        FontSize = fontSize;
        Alignment = alignment;
    }

    public override void Init() { }

    public override void Draw(Vector2 parentPosition)
    {
        Vector2 absolutePosition = parentPosition + position;

        int textWidth = Raylib.MeasureText(Text, FontSize);
        int xPosition;

        switch (Alignment)
        {
            case LabelAlignment.Left:
                xPosition = (int)absolutePosition.X;
                break;
            case LabelAlignment.Center:
                xPosition = (int)absolutePosition.X + (Width - textWidth) / 2;
                break;
            case LabelAlignment.Right:
                xPosition = (int)absolutePosition.X + Width - textWidth;
                break;
            default:
                throw new InvalidOperationException("Unknown label alignment: " + Alignment);
        }

        Raylib.DrawText(Text, xPosition, (int)absolutePosition.Y, FontSize, TextColor);
    }

    public override void Update(WindowManagementWidget windowManagementWidget) { }

    public override void Dispose() { }
}


public class ButtonWidget : WidgetBase
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Text { get; private set; }
    public Color TextColor { get; private set; }
    public int FontSize { get; private set; }
    public ButtonState CurrentState { get; private set; }
    public Color NormalColor { get; private set; }
    public Color HoverColor { get; private set; }
    public Color ClickedColor { get; private set; }
    public event Action<ButtonWidget>? Clicked;
    private WindowWidget parent;

    public enum ButtonState
    {
        Normal,
        Hover,
        Clicked
    }

    public ButtonWidget(WindowWidget parent, Vector2 position, int width, int height, string text, Color textColor, Color normalColor, Color hoverColor, Color clickedColor, int fontSize) : base(position)
    {
        if (width <= 0)
        {
            throw new ArgumentException("Width must be positive", nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentException("Height must be positive", nameof(height));
        }

        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Text must not be null or empty", nameof(text));
        }

        if (fontSize <= 0)
        {
            throw new ArgumentException("Font size must be positive", nameof(fontSize));
        }

        this.parent = parent;
        Width = width;
        Height = height;
        Text = text;
        TextColor = textColor;
        NormalColor = normalColor;
        HoverColor = hoverColor;
        ClickedColor = clickedColor;
        FontSize = fontSize;
        CurrentState = ButtonState.Normal;
    }

    public override void Init() { }

    public override void Draw(Vector2 parentPosition)
    {
        Vector2 absolutePosition = parentPosition + position;

        Color currentColor;

        switch (CurrentState)
        {
            case ButtonState.Normal:
                currentColor = NormalColor;
                break;
            case ButtonState.Hover:
                currentColor = HoverColor;
                break;
            case ButtonState.Clicked:
                currentColor = ClickedColor;
                break;
            default:
                throw new InvalidOperationException("Unknown button state: " + CurrentState);
        }

        Raylib.DrawRectangle((int)absolutePosition.X, (int)absolutePosition.Y, Width, Height, currentColor);

        int textWidth = Raylib.MeasureText(Text, FontSize);
        int textHeight = FontSize;
        int textX = (int)absolutePosition.X + (Width - textWidth) / 2;
        int textY = (int)absolutePosition.Y + (Height - textHeight) / 2;
        Raylib.DrawText(Text, textX, textY, FontSize, TextColor);
    }

    public override void Update(WindowManagementWidget windowManagementWidget)
    {
        Vector2 parentSize = new Vector2(parent.Width, parent.Height);
        Vector2 absolutePosition = parent.GetPosition() + position;

        if (position.X == 0 && position.Y == 0)
        {
            absolutePosition = parent.GetPosition() + parentSize - new Vector2(Width, Height);
        }

        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(absolutePosition.X, absolutePosition.Y, Width, Height)))
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                CurrentState = ButtonState.Clicked;
                OnClick();
            }
            else
            {
                CurrentState = ButtonState.Hover;
            }
        }
        else
        {
            CurrentState = ButtonState.Normal;
        }
    }

    public override void Dispose() { }

    protected virtual void OnClick()
    {
        Clicked?.Invoke(this);
    }

    public void SetText(string newText)
    {
        if (string.IsNullOrEmpty(newText))
        {
            throw new ArgumentException("Text must not be null or empty", nameof(newText));
        }

        Text = newText;
    }

    public void SetTextColor(Color newColor)
    {
        if (newColor.R == 0 && newColor.G == 0 && newColor.B == 0 && newColor.A == 0)
        {
            throw new ArgumentException("Color must not be default", nameof(newColor));
        }

        TextColor = newColor;
    }

    public void SetState(ButtonState newState)
    {
        if (!Enum.IsDefined(typeof(ButtonState), newState))
        {
            throw new ArgumentException("Invalid button state", nameof(newState));
        }

        CurrentState = newState;
    }

    public void SetNormalColor(Color newColor)
    {
        if (newColor.R == 0 && newColor.G == 0 && newColor.B == 0 && newColor.A == 0)
        {
            throw new ArgumentException("Color must not be default", nameof(newColor));
        }

        NormalColor = newColor;
    }

    public void SetHoverColor(Color newColor)
    {
        if (newColor.R == 0 && newColor.G == 0 && newColor.B == 0 && newColor.A == 0)
        {
            throw new ArgumentException("Color must not be default", nameof(newColor));
        }

        HoverColor = newColor;
    }

    public void SetClickedColor(Color newColor)
    {
        if (newColor.R == 0 && newColor.G == 0 && newColor.B == 0 && newColor.A == 0)
        {
            throw new ArgumentException("Color must not be default", nameof(newColor));
        }

        ClickedColor = newColor;
    }

    public void SetFontSize(int newSize)
    {
        if (newSize <= 0)
        {
            throw new ArgumentException("Font size must be positive", nameof(newSize));
        }

        FontSize = newSize;
    }
}

