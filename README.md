# Samples
This branch is matching the latest development branch of Urho.NET dotnet-9.x ,  .NET 9.

The dotnet-9.x (.NET9) branch is still under development but it looks allready now very promising .\
All the samples are running on Windows,Linux,macOS,iOS,Android and web browsers.\
On Android and iOS it is compiled only into NativeAOT .\
On Web Browsers it is compiled into .NET8 due to some issues that still need to be resolved inorder to be able to run on .NET9.\
On Desktop , it can be run either in JIT mode or compiled into a Native AOT binary.

This directory contains the various samples that showcase \
The features of the C# multiplatform game development framework
[Urho.Net .NET9 ](https://github.com/Urho-Net/Urho.Net/tree/dotnet-9.x).\
Please follow the installation instructions of [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-9.x)

 Check the Wiki for quick start
[Wiki](https://github.com/Urho-Net/Samples/wiki)






Urho.NET NativeAOT is supported on all platforms , desktop  mobiles and web browsers.\
It is much more performant , more secure , and most of the time the binaries are smaller.\
it doesnt require any .NET runtime to run because the game is compiled into native machine code.\
More information on 
[Microsoft site](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=windows%2Cnet9plus)




# Running the Feature Samples on Desktop.
Make sure that you followed the instructions and installed all the required dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-9.x).\
This applies to Windows,Linux and MacOS.\

git clone https://github.com/Urho-Net/Samples.git -b dotnet-9.x
* cd Samples
* code FeatureSamples
* Visual Studio Code will open the project.
* press F5 to run
* you can also compile into Native AOT binary by choosing publish-linux , publish-window , publish-osx  , the binaries will be generated in the output folder.



# Running the samples on a real Android device
Make sure that you followed the instructions and installed all the required dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-9.x)\
Only one Android device should be connected to your desktop/laptop.\
Support of multiple connected devices will be added in the future.
* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Tasks:Run Task , pick android-deploy-debug or android-deploy-release
* The sample will compile , will be installed and run on the Android device.

# Running the samples on a real iOS device 
Make sure that you followed the instructions and installed all the required dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-9.x)
* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Tasks:Run Task , pick ios-deploy-release
* Enter your developer ID
* The sample will compile , will be installed and run on the iOS device.

# Running the samples on a Web browser
  Make sure that you followed the instructions and installed all the rquired dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-9.x)

* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Choose Tasks: Run Task
* Choose web-build-debug or web-build-release
* The build will generate Web folder in the output project directory
* The sub folder wwwroot inside the Web folder is self contained and contains everything that is needed for web deployment.
* You can test it on your local browser with the [Live Server extention](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer)
* once you are happy with your game you can compile it in release configuration (web-build-release) , and upload the generated wwwroot self-contained folder to a remote web server.






