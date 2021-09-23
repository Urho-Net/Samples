using Avalonia;
using Avalonia.Input;
using Avalonia.Platform;
using Urho.Resources;
using Urho.Gui;
using Urho.IO;

namespace Urho.Avalonia
{
    class CursorFactory : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub(cursorType);
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new CursorStub(cursor, hotSpot);

        static Gui.Cursor _urhoCursor = null;

        public static StandardCursorType cursorType {get;private set;}

        public CursorFactory()
        {
            if (_urhoCursor == null)
            {
                XmlFile style = Application.Current.ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
                _urhoCursor = new Gui.Cursor();
                _urhoCursor.SetStyleAuto(style);
                _urhoCursor.UseSystemShapes = true;
                int cursorSize = Application.Current.Graphics.Width / 60;
                _urhoCursor.Size = new IntVector2(cursorSize / 2, cursorSize);
                Application.Current.UI.Cursor = _urhoCursor;
                Application.Current.UI.Cursor.Visible = true;
            }

        }

        public static void ResetCursorFactory()
        {
            if(_urhoCursor != null)
            {
                _urhoCursor.Dispose();
            }
            XmlFile style = Application.Current.ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
            _urhoCursor = new Gui.Cursor();
            _urhoCursor.SetStyleAuto(style);
            _urhoCursor.UseSystemShapes = true;
            int cursorSize = Application.Current.Graphics.Width / 60;
            _urhoCursor.Size = new IntVector2(cursorSize / 2, cursorSize);
            Application.Current.UI.Cursor = _urhoCursor;
            Application.Current.UI.Cursor.Visible = true;
        }

        public static void SetCursor(StandardCursorType type)
        {
            if(type == cursorType)return;

            // Configure default values
            CursorShape urhoShape = CursorShape.Normal;
            _urhoCursor.UseSystemShapes = true;
            cursorType  =  type;

            AvaloniaUrhoContext.EnsureInvokeOnMainThread(() =>
            {
                Application.Current.Input.SetMouseVisible(true);
                Application.Current.Input.SetMouseMode(MouseMode.Free);
                Application.Current.UI.Cursor.Visible = true;
            });

            switch (cursorType)
            {
                case StandardCursorType.Arrow:
                    urhoShape = CursorShape.Normal;
                    break;

                case StandardCursorType.Ibeam:
                    urhoShape = CursorShape.Ibeam;
                    break;

                case StandardCursorType.Wait:
                    urhoShape = CursorShape.Busy;
                     _urhoCursor.UseSystemShapes = false;
                    break;

                case StandardCursorType.Cross:
                    urhoShape = CursorShape.Cross;
                    break;

                case StandardCursorType.UpArrow:
                    urhoShape = CursorShape.Normal;
                    break;

                case StandardCursorType.SizeWestEast:
                    urhoShape = CursorShape.Resizehorizontal;
                    break;

                case StandardCursorType.SizeNorthSouth:
                    urhoShape = CursorShape.Resizevertical;
                    break;

                case StandardCursorType.SizeAll:
                    urhoShape = CursorShape.Cross;
                    break;

                case StandardCursorType.No:
                    urhoShape = CursorShape.Rejectdrop;
                    break;

                case StandardCursorType.Hand:
                    urhoShape = CursorShape.Acceptdrop;
                    break;

                case StandardCursorType.AppStarting:
                    urhoShape = CursorShape.Busy;
                     _urhoCursor.UseSystemShapes = false;
                    break;

                case StandardCursorType.Help:
                    urhoShape = CursorShape.Normal;
                    break;

                case StandardCursorType.TopSide:
                    urhoShape = CursorShape.Resizevertical;
                    break;

                case StandardCursorType.BottomSide:
                    urhoShape = CursorShape.Resizevertical;
                    break;

                case StandardCursorType.LeftSide:
                    urhoShape = CursorShape.Resizehorizontal;
                    break;

                case StandardCursorType.RightSide:
                    urhoShape = CursorShape.Resizehorizontal;
                    break;


                case StandardCursorType.TopLeftCorner:
                    urhoShape = CursorShape.Cross;
                    break;

                case StandardCursorType.TopRightCorner:
                    urhoShape = CursorShape.Cross;
                    break;

                case StandardCursorType.BottomLeftCorner:
                    urhoShape = CursorShape.Cross;
                    break;

                case StandardCursorType.BottomRightCorner:
                    urhoShape = CursorShape.Cross;
                    break;

                case StandardCursorType.DragCopy:
                    urhoShape = CursorShape.Acceptdrop;
                    break;

                case StandardCursorType.DragLink:
                    urhoShape = CursorShape.Acceptdrop;
                    break;

                case StandardCursorType.DragMove:
                    urhoShape = CursorShape.Acceptdrop;
                    break;

                case StandardCursorType.None:
                    AvaloniaUrhoContext.EnsureInvokeOnMainThread(() =>
                    {
                        Application.Current.UI.Cursor.Visible = false;
                        _urhoCursor.UseSystemShapes = false;
                        Application.Current.Input.SetMouseVisible(false);
                    });
                    break;


            }

            AvaloniaUrhoContext.CursorShape = urhoShape;
        }

        public class CursorStub : ICursorImpl
        {
            public StandardCursorType _cursorType;
            private bool isCustomCursor = false;

            public CursorStub()
            {
                _cursorType = StandardCursorType.None;
                isCustomCursor = false;
            }

            public CursorStub(IBitmapImpl cursor, PixelPoint hotSpot)
            {
                _cursorType = StandardCursorType.None;
                isCustomCursor = true;
            }

            public CursorStub(StandardCursorType cursorType)
            {
                _cursorType = cursorType;
                isCustomCursor = false;
            }

            public void Dispose() { }
        }
    }
}