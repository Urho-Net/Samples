
namespace Urho.Gui
{
    public class TreeViewWindow : Window
    {
        ListView treeView;
        public TreeViewWindow()
        {
            Application.Current.UI.Root.AddChild(this);
            var Graphics = Application.Current.Graphics;
            SetStyleAuto();
            Size = new IntVector2(Graphics.Width, Graphics.Height);
            CreateTreeView(Graphics.Width,Graphics.Height);

            UpdateLayout();

        }


        void CreateTreeView(int width,int height)
        {

            treeView = CreateChild<ListView>();
            treeView.SetStyle("HierarchyListView");
            treeView.BaseIndent = 1;
            treeView.Size = new IntVector2(width, height);

            treeView.ItemDoubleClicked += OnItemDoubleClicked;
        
            uint index = 0;
            for (uint i = 0; i < 10; i++)
            {
                Text item = new Text();
                item.Name = "Parent Item #" + i;
                item.Value = item.Name;
                item.SetStyleAuto();
                treeView.InsertItem(index++,item,null);

                // create child items
                for (uint j = 0; j < 10; j++)
                {
                    Text childItem = new Text();
                    childItem.Name = "Child Item #" + i+"."+j ;
                    childItem.Value = childItem.Name;
                    childItem.SetStyleAuto();
                    treeView.InsertItem(index++, childItem, item);
                }
    
            }
        }

        private void OnItemDoubleClicked(ItemDoubleClickedEventArgs obj)
        {
             bool expand = treeView.IsExpanded((uint)obj.Selection);

             treeView.Expand( (uint)obj.Selection,!expand, true);       
        }

        protected override void Dispose(bool disposing)
        {
            treeView.ItemDoubleClicked -= OnItemDoubleClicked;
            base.Dispose(disposing);
        }

    }

}