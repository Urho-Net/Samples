Make sure you are always using the latest Urho.Net framework package , otherwise it might won't work.\
https://github.com/Urho-Net/Urho.Net

- This Sample demonstrates the ability to hot-reload modified components , while the application is running.
- There are 2 components in this demo  "Oscillator.cs" and "Rotator.cs" that are modifiable and hot-reloaded during the runtime of the app.
- To test this Sample open the app in Visual-Studio-Code and run it.
- While the application is running ,modify the source code of either or both components .
- Save (Ctrl/Command + S ) while running the app and observe compilation and hot-reload of the components , the scene will be updated on the fly.
- The compnents can be modified in any possible way , they will be reloaded only if compilation succeeds.
- Recomended modifications in Rotator.cs are Vector3 RotationSpeed
- Recomended modifications in Oscillator.cs are  : Vector3 movementVector  ,float movementFactor  ,float period;




