Make sure you are always using the latest Urho.Net framework package , otherwise it might won't work.\
https://github.com/Urho-Net/Urho.Net

- This UIBuilder Sample demonstrates the ability to create dynamic UI while the application is running  , hot-reload is fully supported on any file in the HotReload Folder.
- The HotReload folder containes 2 folders , Builder and UI folder.
- The UI folder contains different  UIElements and supporting source code , new files can be added while the application is running and will be compiled instantly and hot-reloaded.\
Any file/s can be modified , will be instantly compiled and hot-reloaded in less than 150 milliseconds.

- The Builder folder contains 1 file , UIDynamicBuilder.cs
  UIDynamicBuilder.Start() is the main entry point.\
  It creates/instantiates the UI Window and displays it .\
   Basically you can create a new file ( new UI Window) in the UI folder on the fly while the application is running  and instatntiate it in UIDynamicBuilder.Start() .\
    Hot-reload will do the magic and the Window will be displayed instantly.
  There are several examples in  UIDynamicBuilder.Start() 

- To test this Sample open the app in Visual-Studio-Code and run it (F5).
- While the application is running ,modify the source code of any file/s in the  HotReload folder.
- Save (Ctrl/Command + S  or Save All if several files were modified) while running the app and observe instant compilation and hot-reload  , the  UI scene will be updated on the fly instantly and will be displayed.

- If several files are modified at once , one must make sure to save them all (Save All) inorder to pass compilation successfully.
  
- If the compilation fails , the display will become black .   Compilation errors will be presented in the Debug Console.

- One can preview the UI scene on mobile devices (Android and iOS) by deploying to the mobile device using the regular way of Urho.Net mobile deployment
- One can preview the UI scene on Web browsers using the regular way of Urho.Net Web deployment.





